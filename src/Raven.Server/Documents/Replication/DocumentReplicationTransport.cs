﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Raven.Abstractions.Connection;
using Raven.Abstractions.Data;
using Raven.Abstractions.Util;
using Raven.Client.Connection;
using Raven.Client.Platform.Unix;
using Raven.Server.Documents;
using Raven.Server.Json;
using Raven.Server.ServerWide.Context;
using Sparrow.Json;

namespace Raven.Server.ReplicationUtil
{
    public class DocumentReplicationTransport : IDisposable
    {
        private readonly string _url;
        private readonly Guid _srcDbId;
        private readonly string _srcDbName;
        private readonly CancellationToken _cancellationToken;
        private WebSocket _webSocket;
        private bool _disposed;
        private WebsocketStream _websocketStream;

        //does not need alot of cache
        private static readonly HttpJsonRequestFactory _jsonRequestFactory = new HttpJsonRequestFactory(128);
        private readonly string _targetDbName;	    

        public DocumentReplicationTransport(string url, 
            Guid srcDbId, 
            string srcDbName,
            string targetDbName,
            CancellationToken cancellationToken)
        {
            _url = url;
            _srcDbId = srcDbId;
            _srcDbName = srcDbName;
            _targetDbName = targetDbName;
            _cancellationToken = cancellationToken;
            _disposed = false;
        }

        public async Task EnsureConnectionAsync()
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                _webSocket = await GetAndConnectWebSocketAsync();
                _websocketStream = new WebsocketStream(_webSocket, _cancellationToken);
            }
        }

        public long GetLatestEtag()
        {
            //auth here? not sure it is needed
            var @params = new CreateHttpJsonRequestParams(null,
                $"{_url}/databases/{_targetDbName}/lastSentEtag?srcDbId={_srcDbId}",HttpMethod.Get, 
                new OperationCredentials(string.Empty, new NetworkCredential()), null);
            using (var request = _jsonRequestFactory.CreateHttpJsonRequest(@params))
            {
                var response = AsyncHelpers.RunSync(() => request.ExecuteRawResponseAsync());
                response.EnsureSuccessStatusCode();
                
                IEnumerable<string> values;
                if (!response.Headers.TryGetValues(Constants.LastEtagFieldName, out values))
                    return 0;

                var val = values.FirstOrDefault();
                long etag;
                if(string.IsNullOrWhiteSpace(val) || !long.TryParse(val, out etag))
                    throw new NotImplementedException($@"
                            Expected an int64 number when fetching last etag, but got {val}. 
                                This should not happen and it is likely a bug.");

                return etag;
            }
        }

        private async Task<WebSocket> GetAndConnectWebSocketAsync()
        {
            var uri = new Uri(
                $@"{_url?.Replace("http://", "ws://")
                    ?.Replace(".fiddler", "")}/databases/{_targetDbName?.Replace("/", string.Empty)}/documentReplication?srcDbId={_srcDbId}&srcDbName={EscapingHelper
                        .EscapeLongDataString(_srcDbName)}");
            try
            {
                if (Sparrow.Platform.Platform.RunningOnPosix)
                {
                    var webSocketUnix = new RavenUnixClientWebSocket();
                    await webSocketUnix.ConnectAsync(uri, _cancellationToken);

                    return webSocketUnix;
                }

                var webSocket = new ClientWebSocket();			    
                await webSocket.ConnectAsync(uri, _cancellationToken);
                return webSocket;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Failed to connect websocket for remote replication node.", e);
            }
        }

        public async Task SendDocumentBatchAsync(Document[] docs, DocumentsOperationContext context)
        {
            await EnsureConnectionAsync();
            using (var writer = new BlittableJsonTextWriter(context,_websocketStream))
            {
                for (int i = 0; i < docs.Length; i++)
                {
                    docs[i].EnsureMetadata();												
                    context.Write(writer, docs[i].Data);
                    writer.Flush();
                }
            }
            await _websocketStream.WriteEndOfMessageAsync();
            await _websocketStream.FlushAsync(_cancellationToken);
        }		

        public async Task SendHeartbeatAsync(DocumentsOperationContext context)
        {
            await EnsureConnectionAsync();
            //TODO : finish heartbeat
        }

        public void Dispose()
        {
            _disposed = true;
            _webSocket.Dispose();
        }				   
    }
}
