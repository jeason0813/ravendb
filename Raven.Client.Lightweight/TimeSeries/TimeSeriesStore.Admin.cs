﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Raven.Abstractions.Connection;
using Raven.Abstractions.TimeSeries;
using Raven.Abstractions.Extensions;
using Raven.Abstractions.Util;
using Raven.Json.Linq;

namespace Raven.Client.TimeSeries
{
    public partial class TimeSeriesStore
    {
		public class TimeSeriesStoreAdminOperations
		{
			private readonly TimeSeriesStore parent;
			
			internal TimeSeriesStoreAdminOperations(TimeSeriesStore parent)
			{
				this.parent = parent;
			}

			/// <summary>
			/// Create new time series on the server.
			/// </summary>
			/// <param name="timeSeriesDocument">Settings for the time series. If null, default settings will be used, and the name specified in the client ctor will be used</param>
			/// <param name="timeSeriesName">Override timeSeries storage name specified in client ctor. If null, the name already specified will be used</param>
			public async Task<TimeSeriesStore> CreateTimeSeriesAsync(TimeSeriesDocument timeSeriesDocument, string timeSeriesName, bool shouldUpateIfExists = false, OperationCredentials credentials = null, CancellationToken token = default(CancellationToken))
			{
				if (timeSeriesDocument == null)
					throw new ArgumentNullException("timeSeriesDocument");
				if (timeSeriesName == null) throw new ArgumentNullException("timeSeriesName");

				parent.AssertInitialized();

				var urlTemplate = "{0}/admin/cs/{1}";
				if (shouldUpateIfExists)
					urlTemplate += "?update=true";

				var requestUriString = String.Format(urlTemplate, parent.Url, timeSeriesName);

				using (var request = parent.CreateHttpJsonRequest(requestUriString, HttpMethods.Put))
				{
					try
					{
						await request.WriteAsync(RavenJObject.FromObject(timeSeriesDocument)).WithCancellation(token).ConfigureAwait(false);
					}
					catch (ErrorResponseException e)
					{
						if (e.StatusCode == HttpStatusCode.Conflict)
							throw new InvalidOperationException("Cannot create time series with the name '" + timeSeriesName + "' because it already exists. Use CreateOrUpdateTimeSeriesAsync in case you want to update an existing timeSeries storage", e);

						throw;
					}
				}

				return new TimeSeriesStore
				{
					Name = timeSeriesName,
					Url = parent.Url,
					Credentials = credentials ?? parent.Credentials
				};
			}

			public async Task DeleteTimeSeriesAsync(string timeSeriesName, bool hardDelete = false, CancellationToken token = default(CancellationToken))
			{
				parent.AssertInitialized();

				var requestUriString = String.Format("{0}/admin/cs/{1}?hard-delete={2}", parent.Url, timeSeriesName, hardDelete);

				using (var request = parent.CreateHttpJsonRequest(requestUriString, HttpMethods.Delete))
				{
					try
					{
						await request.ExecuteRequestAsync().WithCancellation(token).ConfigureAwait(false);
					}
					catch (ErrorResponseException e)
					{
						if (e.StatusCode == HttpStatusCode.NotFound)
							throw new InvalidOperationException(string.Format("TimeSeries storage with specified name ({0}) doesn't exist", timeSeriesName));
						throw;
					}
				}
			}

			public async Task<string[]> GetTimeSeriesNamesAsync(CancellationToken token = default(CancellationToken))
			{
				parent.AssertInitialized();

				var requestUriString = String.Format("{0}/cs/timeSeriesNames", parent.Url);

				using (var request = parent.CreateHttpJsonRequest(requestUriString, HttpMethods.Get))
				{
					var response = await request.ReadResponseJsonAsync().WithCancellation(token).ConfigureAwait(false);
					return response.ToObject<string[]>(parent.JsonSerializer);
				}
			}
			 
		}
    }
}
