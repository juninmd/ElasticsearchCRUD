using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ElasticsearchCRUD.ContextAddDeleteUpdate.IndexModel;
using ElasticsearchCRUD.Model;
using ElasticsearchCRUD.Tracing;
using ElasticsearchCRUD.Utils;

namespace ElasticsearchCRUD.ContentExists
{
	public class ElasticsearchContextExists
	{
		private readonly ITraceProvider _traceProvider;
		private readonly ElasticsearchSerializerConfiguration _elasticsearchSerializerConfiguration;
		private readonly string _connectionString;
		public readonly Exists ExistsHeadRequest;

		public ElasticsearchContextExists(ITraceProvider traceProvider, CancellationTokenSource cancellationTokenSource,
			ElasticsearchSerializerConfiguration elasticsearchSerializerConfiguration, HttpClient client, string connectionString)
		{
			_traceProvider = traceProvider;
			_elasticsearchSerializerConfiguration = elasticsearchSerializerConfiguration;
			_connectionString = connectionString;
			ExistsHeadRequest = new Exists(_traceProvider, cancellationTokenSource, client);
		}

		public async Task<ResultDetails<bool>> DocumentExistsAsync<T>(object entityId, RoutingDefinition routingDefinition)
		{
			var elasticSearchMapping = _elasticsearchSerializerConfiguration.ElasticsearchMappingResolver.GetElasticSearchMapping(typeof(T));
			var index = elasticSearchMapping.GetIndexForType(typeof(T));
			var type = elasticSearchMapping.GetDocumentType(typeof(T));
			_traceProvider.Trace(TraceEventType.Verbose, $"ElasticsearchContextExists: IndexExistsAsync for Type:{typeof(T)}, Index: {index}, IndexType: {type}, Entity {entityId}");

			var uri = new Uri($"{_connectionString}/{index}/{type}/{entityId}{RoutingDefinition.GetRoutingUrl(routingDefinition)}");
			return await ExistsHeadRequest.ExistsAsync(uri);
		}

		public async Task<ResultDetails<bool>> IndexExistsAsync<T>()
		{
			var elasticSearchMapping = _elasticsearchSerializerConfiguration.ElasticsearchMappingResolver.GetElasticSearchMapping(typeof(T));
			var index = elasticSearchMapping.GetIndexForType(typeof(T));
			_traceProvider.Trace(TraceEventType.Verbose, $"ElasticsearchContextExists: IndexExistsAsync for Type:{typeof(T)}, Index: {index}");

			var uri = new Uri($"{_connectionString}/{index}");
			return await ExistsHeadRequest.ExistsAsync(uri);
		}

		public async Task<ResultDetails<bool>> IndexTypeExistsAsync<T>()
		{
			var elasticSearchMapping = _elasticsearchSerializerConfiguration.ElasticsearchMappingResolver.GetElasticSearchMapping(typeof(T));
			var index = elasticSearchMapping.GetIndexForType(typeof(T));
			var type = elasticSearchMapping.GetDocumentType(typeof(T));
			_traceProvider.Trace(TraceEventType.Verbose, $"ElasticsearchContextExists: IndexExistsAsync for Type:{typeof(T)}, Index: {index}, IndexType: {type}");

			var uri = new Uri($"{_connectionString}/{index}/{type}");
			return await ExistsHeadRequest.ExistsAsync(uri);
		}

		public async Task<ResultDetails<bool>> AliasExistsForIndexAsync<T>(string alias)
		{
			var elasticSearchMapping = _elasticsearchSerializerConfiguration.ElasticsearchMappingResolver.GetElasticSearchMapping(typeof(T));
			var index = elasticSearchMapping.GetIndexForType(typeof(T));
			_traceProvider.Trace(TraceEventType.Verbose, $"ElasticsearchContextExists: AliasExistsAsync for Type:{typeof(T)}, Index: {index}");

			var uri = new Uri($"{_connectionString}/{index}/_alias/{alias}");
			return await ExistsHeadRequest.ExistsAsync(uri);
		}

		public async Task<ResultDetails<bool>> AliasExistsAsync(string alias)
		{
			_traceProvider.Trace(TraceEventType.Verbose, $"ElasticsearchContextExists: AliasExistsAsync for alias:{alias}");

			var uri = new Uri($"{_connectionString}/_alias/{alias}");
			return await ExistsHeadRequest.ExistsAsync(uri);
		}

		public bool Exists(Task<ResultDetails<bool>> method)
		{
			var syncExecutor = new SyncExecute(_traceProvider);
			return syncExecutor.Execute(() => method);
		}

		public async Task<ResultDetails<bool>> ExistsAsync(Uri uri)
		{
			return await ExistsHeadRequest.ExistsAsync(uri);
		}
	}
}
