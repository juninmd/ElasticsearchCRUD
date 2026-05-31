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
			_traceProvider.Trace(TraceEventType.Verbose, "ElasticsearchContextExists: IndexExistsAsync for Type:{0}, Index: {1}, IndexType: {2}, Entity {3}",
				typeof(T),
				elasticSearchMapping.GetIndexForType(typeof(T)),
				elasticSearchMapping.GetDocumentType(typeof(T)),
				entityId
			);

			var uri = new Uri($"{_connectionString}/{elasticSearchMapping.GetIndexForType(typeof(T))}/{elasticSearchMapping.GetDocumentType(typeof(T))}/{entityId}{RoutingDefinition.GetRoutingUrl(routingDefinition)}");
			return await ExistsHeadRequest.ExistsAsync(uri);
		}

		public async Task<ResultDetails<bool>> IndexExistsAsync<T>()
		{
			var elasticSearchMapping = _elasticsearchSerializerConfiguration.ElasticsearchMappingResolver.GetElasticSearchMapping(typeof(T));
			_traceProvider.Trace(TraceEventType.Verbose, "ElasticsearchContextExists: IndexExistsAsync for Type:{0}, Index: {1}",
				typeof(T),
				elasticSearchMapping.GetIndexForType(typeof(T))
			);

			var uri = new Uri($"{_connectionString}/{elasticSearchMapping.GetIndexForType(typeof(T))}");
			return await ExistsHeadRequest.ExistsAsync(uri);
		}

		public async Task<ResultDetails<bool>> IndexTypeExistsAsync<T>()
		{
			var elasticSearchMapping = _elasticsearchSerializerConfiguration.ElasticsearchMappingResolver.GetElasticSearchMapping(typeof(T));
			_traceProvider.Trace(TraceEventType.Verbose, "ElasticsearchContextExists: IndexExistsAsync for Type:{0}, Index: {1}, IndexType: {2}", 
				typeof(T), 
				elasticSearchMapping.GetIndexForType(typeof(T)), 
				elasticSearchMapping.GetDocumentType(typeof(T))
			);

			var uri = new Uri($"{_connectionString}/{elasticSearchMapping.GetIndexForType(typeof(T))}/{elasticSearchMapping.GetDocumentType(typeof(T))}");
			return await ExistsHeadRequest.ExistsAsync(uri);
		}

		public async Task<ResultDetails<bool>> AliasExistsForIndexAsync<T>(string alias)
		{
			var elasticSearchMapping = _elasticsearchSerializerConfiguration.ElasticsearchMappingResolver.GetElasticSearchMapping(typeof(T));
			_traceProvider.Trace(TraceEventType.Verbose, "ElasticsearchContextExists: AliasExistsAsync for Type:{0}, Index: {1}",
				typeof(T),
				elasticSearchMapping.GetIndexForType(typeof(T))
			);

			var uri = new Uri($"{_connectionString}/{elasticSearchMapping.GetIndexForType(typeof(T))}/_alias/{alias}");
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
