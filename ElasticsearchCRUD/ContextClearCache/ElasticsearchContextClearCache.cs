using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ElasticsearchCRUD.Model;
using ElasticsearchCRUD.Tracing;
using ElasticsearchCRUD.Utils;

namespace ElasticsearchCRUD.ContextClearCache
{
	public class ElasticsearchContextClearCache
	{
		private readonly ITraceProvider _traceProvider;
		private readonly CancellationTokenSource _cancellationTokenSource;
		private readonly ElasticsearchSerializerConfiguration _elasticsearchSerializerConfiguration;
		private readonly HttpClient _client;
		private readonly string _connectionString;

		public ElasticsearchContextClearCache(ITraceProvider traceProvider, CancellationTokenSource cancellationTokenSource, ElasticsearchSerializerConfiguration elasticsearchSerializerConfiguration, HttpClient client, string connectionString)
		{
			_traceProvider = traceProvider;
			_cancellationTokenSource = cancellationTokenSource;
			_elasticsearchSerializerConfiguration = elasticsearchSerializerConfiguration;
			_client = client;
			_connectionString = connectionString;
		}

		public bool ClearCacheForIndex(string index)
		{
			var syncExecutor = new SyncExecute(_traceProvider);
			return syncExecutor.Execute(() => ClearCacheForIndexAsync(index));	
		}

		public bool ClearCacheForIndex<T>()
		{
			var syncExecutor = new SyncExecute(_traceProvider);
			return syncExecutor.Execute(() => ClearCacheForIndexAsync<T>());	
		}

		public async Task<ResultDetails<bool>> ClearCacheForIndexAsync<T>()
		{
			var elasticSearchMapping = _elasticsearchSerializerConfiguration.ElasticsearchMappingResolver.GetElasticSearchMapping(typeof(T));
			var index = elasticSearchMapping.GetIndexForType(typeof(T));
			_traceProvider.Trace(TraceEventType.Verbose, $"ElasticsearchContextClearCache: Clearing Cache for index {index}");

			var resultDetails = new ResultDetails<bool> {Status = HttpStatusCode.InternalServerError};
			var uri = new Uri($"{_connectionString}/{index}/_cache/clear");
			_traceProvider.Trace(TraceEventType.Verbose, "{1}: Request HTTP POST uri: {0}", uri.AbsoluteUri, "ElasticsearchContextClearCache");

			var request = new HttpRequestMessage(HttpMethod.Post, uri);
			var response = await _client.SendAsync(request, _cancellationTokenSource.Token).ConfigureAwait(false);

			if (response.StatusCode == HttpStatusCode.OK)
			{
				resultDetails.PayloadResult = true;
				return resultDetails;
			}

			_traceProvider.Trace(TraceEventType.Error, $"ElasticsearchContextClearCache: Could nor clear cache for index {elasticSearchMapping.GetIndexForType(typeof(T))}");
			throw new ElasticsearchCrudException($"ElasticsearchContextClearCache: Could nor clear cache for index {elasticSearchMapping.GetIndexForType(typeof(T))}");
		}

		public async Task<ResultDetails<bool>> ClearCacheForIndexAsync(string index)
		{
			_traceProvider.Trace(TraceEventType.Verbose, $"ElasticsearchContextClearCache: Clearing Cache for index {index}");

			var resultDetails = new ResultDetails<bool> { Status = HttpStatusCode.InternalServerError };
			var uri = new Uri($"{_connectionString}/{index}/_cache/clear");
			_traceProvider.Trace(TraceEventType.Verbose, "{1}: Request HTTP POST uri: {0}", uri.AbsoluteUri, "ElasticsearchContextClearCache");

			var request = new HttpRequestMessage(HttpMethod.Post, uri);
			var response = await _client.SendAsync(request, _cancellationTokenSource.Token).ConfigureAwait(false);

			if (response.StatusCode == HttpStatusCode.OK)
			{
				resultDetails.PayloadResult = true;
				return resultDetails;
			}

			_traceProvider.Trace(TraceEventType.Error, $"ElasticsearchContextClearCache: Could nor clear cache for index {index}");
			throw new ElasticsearchCrudException($"ElasticsearchContextClearCache: Could nor clear cache for index {index}");
		}
	}
}
