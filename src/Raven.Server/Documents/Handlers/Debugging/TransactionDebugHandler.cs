﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Raven.Server.Routing;
using Sparrow.Json;
using Sparrow.Json.Parsing;
using Voron;
using Voron.Impl;

namespace Raven.Server.Documents.Handlers.Debugging
{
    class TransactionDebugHandler : DatabaseRequestHandler
    {
        public class TransactionInfo
        {
            public string Path;
            public List<LowLevelTransaction> Information;
        }

        [RavenAction("/databases/*/debug/txinfo", "GET")]
        public Task TxInfo()
        {
            
            var results = new List<TransactionInfo>();

            foreach (var env in Database.GetAllStoragesEnvironment())
            {
                var txInfo = new TransactionInfo
                {
                    Path = env.Environment.Options.BasePath,
                    Information = env.Environment.ActiveTransactions.AllTransactionsInstances
                };
                results.Add(txInfo);
            }

            JsonOperationContext context;
            using (ContextPool.AllocateOperationContext(out context))
            using (var writer = new BlittableJsonTextWriter(context, ResponseBodyStream()))
            {
                context.Write(writer, ToJson(results));
            }
            return Task.CompletedTask;
        }

        private DynamicJsonArray ToJson(List<TransactionInfo> txInfos)
        {
            if (txInfos.Count < 1)
                return  new DynamicJsonArray();

            return new DynamicJsonArray(txInfos.Select(ToJson));
        }

        private DynamicJsonValue ToJson(TransactionInfo txinfo)
        {
            return new DynamicJsonValue
            {
                [nameof(StorageEnvironmentOptions.BasePath)] = txinfo.Path,
                [nameof(TransactionInfo.Information)] = new DynamicJsonArray(txinfo.Information.Select(ToJson))
            };
        }

        private DynamicJsonValue ToJson(LowLevelTransaction lowLevelTransaction)
        {
            return new DynamicJsonValue
            {
                [nameof(TxInfoResult.TransactionId)] = lowLevelTransaction.Id,
                [nameof(TxInfoResult.ThreadId)] = lowLevelTransaction.ThreadId,
                [nameof(TxInfoResult.StartTime)] = lowLevelTransaction.TxStartTime,
                [nameof(TxInfoResult.TotalTime)] = $"{(DateTime.UtcNow - lowLevelTransaction.TxStartTime).Milliseconds} mSecs",
                [nameof(TxInfoResult.FlushInProgressLockTaken)] = lowLevelTransaction.FlushInProgressLockTaken,
                [nameof(TxInfoResult.Flags)] = lowLevelTransaction.Flags,
                [nameof(TxInfoResult.IsLazyTransaction)] = lowLevelTransaction.IsLazyTransaction,
                [nameof(TxInfoResult.NumberOfModifiedPages)] = lowLevelTransaction.NumberOfModifiedPages,
                [nameof(TxInfoResult.Committed)] = lowLevelTransaction.Committed
            };
        }
    }

    internal class TxInfoResult
    {
        public int TransactionId;
        public int ThreadId;
        public int StartTime;
        public int TotalTime;
        public bool FlushInProgressLockTaken;
        public TransactionFlags Flags;
        public bool IsLazyTransaction;
        public long NumberOfModifiedPages;
        public bool Committed;
    }
}