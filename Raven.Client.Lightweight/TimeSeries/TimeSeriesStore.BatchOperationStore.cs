﻿using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Raven.Abstractions.TimeSeries;
using Raven.Abstractions.Extensions;
using Raven.Client.TimeSeries.Operations;

namespace Raven.Client.TimeSeries
{
	public partial class TimeSeriesStore
	{
		public class BatchOperationsStore 
		{
			private readonly TimeSeriesStore parent;
			private readonly Lazy<TimeSeriesBatchOperation> defaultBatchOperation;
			private readonly ConcurrentDictionary<string, TimeSeriesBatchOperation> batchOperations;

			internal BatchOperationsStore(TimeSeriesStore parent)
			{
				batchOperations = new ConcurrentDictionary<string, TimeSeriesBatchOperation>();
				this.parent = parent;
				if (string.IsNullOrWhiteSpace(parent.Name) == false)
					defaultBatchOperation = new Lazy<TimeSeriesBatchOperation>(() => new TimeSeriesBatchOperation(parent, parent.Name));

				OperationId = Guid.NewGuid();
			}

			public TimeSeriesBatchOperation this[string storageName]
			{
				get { return GetOrCreateBatchOperation(storageName); }
			}

			private TimeSeriesBatchOperation GetOrCreateBatchOperation(string storageName)
			{
				return batchOperations.GetOrAdd(storageName, arg => new TimeSeriesBatchOperation(parent, storageName));
			}

			public void Dispose()
			{
				batchOperations.Values
					.ForEach(operation => operation.Dispose());
				if (defaultBatchOperation != null && defaultBatchOperation.IsValueCreated)
					defaultBatchOperation.Value.Dispose();
			}

			public void ScheduleChange(string groupName, string timeSeriesName, long delta)
			{
				if (string.IsNullOrWhiteSpace(parent.Name))
					throw new InvalidOperationException("Default timeSeries storage name cannot be empty!");

				defaultBatchOperation.Value.ScheduleChange(groupName, timeSeriesName, delta);
			}

			public void ScheduleIncrement(string groupName, string timeSeriesName)
			{
				if (string.IsNullOrWhiteSpace(parent.Name))
					throw new InvalidOperationException("Default timeSeries storage name cannot be empty!");

				defaultBatchOperation.Value.ScheduleIncrement(groupName, timeSeriesName);
			}

			public void ScheduleDecrement(string groupName, string timeSeriesName)
			{
				if (string.IsNullOrWhiteSpace(parent.Name))
					throw new InvalidOperationException("Default timeSeries storage name cannot be empty!");

				defaultBatchOperation.Value.ScheduleDecrement(groupName, timeSeriesName);
			}

			public async Task FlushAsync()
			{
				if (string.IsNullOrWhiteSpace(parent.Name))
					throw new InvalidOperationException("Default time series name cannot be empty!");

				parent.AssertInitialized();

				await parent.ReplicationInformer.UpdateReplicationInformationIfNeededAsync();
				await defaultBatchOperation.Value.FlushAsync();
			}

			public Guid OperationId { get; private set; }

			public TimeSeriesBatchOptions DefaultOptions
			{
				get
				{
					if (string.IsNullOrWhiteSpace(parent.Name))
						throw new InvalidOperationException("Default time series name cannot be empty!");
					return defaultBatchOperation.Value.DefaultOptions;
				}
			}
		}
	}
}
