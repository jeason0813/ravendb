﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Raven.Abstractions.Data;
using Raven.Abstractions.Util;
using Raven.Server.Extensions;
using Raven.Server.Routing;
using Raven.Server.ServerWide;
using Raven.Server.ServerWide.Context;
using Sparrow.Json;
using Sparrow.Json.Parsing;

namespace Raven.Server.Documents.Handlers
{
    public class DocumentReplicationRequestHandler : DatabaseRequestHandler
    {
        [RavenAction("/databases/*/lastSentEtag", "GET", "@/databases/{databaseName: string}/lastSentEtag?srcDbId={databaseUniqueId:string}")]
        public Task GetLastSentEtag()
        {
            var srcDbId = Guid.Parse(GetQueryStringValueAndAssertIfSingleAndNotEmpty("srcDbId"));

            DocumentsOperationContext context;
            using (ContextPool.AllocateOperationContext(out context))
            using (context.OpenReadTransaction())
            {
                var serverChangeVector = Database.DocumentsStorage.GetChangeVector(context);
                var vectorEntry = serverChangeVector.FirstOrDefault(x => x.DbId == srcDbId);

                //no need to write a document for transferring a single value
                HttpContext.Response.Headers[Constants.LastEtagFieldName] = vectorEntry.Etag.ToString();				
            }
            return Task.CompletedTask;
        }

        //an endpoint to establish replication websocket
        [RavenAction("/databases/*/documentReplication", "GET",
            @"@/databases/{databaseName:string}/documentReplication?
                srcDbId={databaseUniqueId:string}
                &srcDbName={databaseName:string}")]
        public async Task DocumentReplicationConnection()
        {
            var srcDbId = Guid.Parse(GetQueryStringValueAndAssertIfSingleAndNotEmpty("srcDbId"));
            var srcDbName = EscapingHelper.UnescapeLongDataString(GetQueryStringValueAndAssertIfSingleAndNotEmpty("srcDbName"));

            var hasOtherSideClosedConnection = false;
            bool shouldConnectBack;
            var srcUrl = HttpContext.Request.GetHostnameUrl();
            var executer = Database.DocumentReplicationLoader.RegisterConnection(
                srcDbId,
                srcUrl,
                srcDbName,
                out shouldConnectBack);

            if(executer.HasOutgoingReplication)
                executer.Start();

            string ReplicationReceiveDebugTag = $"document-replication/receive <{executer.ReplicationUniqueName}>";			

            using (var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync())
            {
                DocumentsOperationContext context;
                using (ContextPool.AllocateOperationContext(out context))
                {
                    var docs = new List<BlittableJsonReaderObject>();
                    var buffer = new ArraySegment<byte>(context.GetManagedBuffer());
                    var jsonParserState = new JsonParserState();
                    using (var parser = new UnmanagedJsonParser(context, jsonParserState, ReplicationReceiveDebugTag))
                    {
                        while (!Database.DatabaseShutdown.IsCancellationRequested)
                        {
                            //this loop handles one replication batch
                            RavenTransaction tx = null;
                            try
                            {
                                while (true)
                                {
                                    var result = await webSocket.ReceiveAsync(buffer, Database.DatabaseShutdown);
                                    //open write transaction at beginning of the batch
                                    if (tx == null)
                                        tx = context.OpenWriteTransaction();

                                    var writer = new BlittableJsonDocumentBuilder(context,
                                        BlittableJsonDocumentBuilder.UsageMode.None, ReplicationReceiveDebugTag,
                                        parser, jsonParserState);
                                    writer.ReadObject();
                                    if (result.MessageType == WebSocketMessageType.Close)
                                    {
                                        hasOtherSideClosedConnection = true;
                                        break;
                                    }

                                    if (result.EndOfMessage)
                                        break;

                                    parser.SetBuffer(buffer.Array, result.Count);
                                    while (writer.Read() == false)
                                    {
                                        result = await webSocket.ReceiveAsync(buffer, Database.DatabaseShutdown);
                                        parser.SetBuffer(buffer.Array, result.Count);
                                    }
                                    writer.FinalizeDocument();
                                    var receivedDoc = writer.CreateReader();
                                    docs.Add(receivedDoc);
                                }
                                executer.ReceiveReplicatedDocuments(context, docs);

                                //precaution
                                if (tx == null)
                                    throw new InvalidOperationException(@"
                                        Transaction is not initialized while receiving replicated documents; this
                                         is something that is not supposed to happen and is likely a bug.");

                                tx.Commit();
                            }
                            catch (Exception e)
                            {
                                throw new InvalidOperationException("Failed to receive replication document batch.", e);
                            }
                            finally
                            {
                                tx?.Dispose();
                            }

                            if (hasOtherSideClosedConnection)
                                break;
                        }
                    }

                    //if execution path gets here, it means the node was disconnected
                    Database.DocumentReplicationLoader.HandleConnectionDisconnection(executer);
                }
            }
        }		
    }
}
