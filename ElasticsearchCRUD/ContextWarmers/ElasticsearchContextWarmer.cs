using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ElasticsearchCRUD.Model;
using ElasticsearchCRUD.Tracing;
using ElasticsearchCRUD.Utils;

namespace ElasticsearchCRUD.ContextWarmers
{
	class ElasticsearchContextWarmer
	{
		private readonly ITraceProvider _traceProvider;
		private readonly CancellationTokenSource _cancellationTokenSource;
		private readonly ElasticsearchSerializerConfiguration _elasticsearchSerializerConfiguration;
		private readonly HttpClient _client;
		private readonly string _connectionString;

		public ElasticsearchContextWarmer(ITraceProvider traceProvider, CancellationTokenSource cancellationTokenSource, ElasticsearchSerializerConfiguration elasticsearchSerializerConfiguration, HttpClient client, string connectionString)
		{
			_traceProvider = traceProvider;
			_cancellationTokenSource = cancellationTokenSource;
			_elasticsearchSerializerConfiguration = elasticsearchSerializerConfiguration;
			_client = client;
			_connectionString = connectionString;
		}

		public bool SendWarmerCommand(Warmer warmer, string index, string type)
		{
			var syncExecutor = new SyncExecute(_traceProvider);
			return syncExecutor.Execute(() => SendWarmerCommandAsync(warmer, index, type));	
		}

		public async Task<ResultDetails<bool>> SendWarmerCommandAsync(Warmer warmer, string index, string type)
		{
			_traceProvider.Trace(TraceEventType.Verbose, $"ElasticsearchContextWarmer: Creating Warmer {warmer.Name}");

			var resultDetails = new ResultDetails<bool> { Status = HttpStatusCode.InternalServerError };
			var elasticsearchUrl = CreateWarmerUriParameter(index, type, warmer.Name);
			var uri = new Uri(elasticsearchUrl);
			_traceProvider.Trace(TraceEventType.Verbose, "{1}: Request HTTP PUT uri: {0}", uri.AbsoluteUri, "ElasticsearchContextWarmer");

			var content = new StringContent(warmer.ToString());
			var response = await _client.PutAsync(uri, content, _cancellationTokenSource.Token).ConfigureAwait(false);

			if (response.StatusCode == HttpStatusCode.OK)
			{
				resultDetails.PayloadResult = true;
				return resultDetails;
			}

			_traceProvider.Trace(TraceEventType.Error, $"ElasticsearchContextWarmer: Cound Not Execute Warmer Create {warmer.Name}");
			throw new ElasticsearchCrudException($"ElasticsearchContextWarmer: Could Not Execute Warmer Create {warmer.Name}");
		}

		private string CreateWarmerUriParameter(string index, string type, string warmerName)
		{
			if (string.IsNullOrEmpty(index))
			{
				return $"{_connectionString}/_warmer/{warmerName}";
			}

			if (string.IsNullOrEmpty(type))
			{
				return $"{_connectionString}/{index}/_warmer/{warmerName}";
			}

			return $"{_connectionString}/{index}/{type}/_warmer/{warmerName}";
		}

		public bool SendWarmerDeleteCommand(string warmerName, string index)
		{
			var syncExecutor = new SyncExecute(_traceProvider);
			return syncExecutor.Execute(() => SendWarmerDeleteCommandAsync(warmerName, index));
		}

		public async Task<ResultDetails<bool>> SendWarmerDeleteCommandAsync(string warmerName, string index)
		{
			_traceProvider.Trace(TraceEventType.Verbose, $"ElasticsearchContextWarmer: Deleting Warmer {warmerName}");

			var resultDetails = new ResultDetails<bool> { Status = HttpStatusCode.InternalServerError };
			var elasticsearchUrl = $"{_connectionString}/{index}/_warmer/{warmerName}";
			var uri = new Uri(elasticsearchUrl);
			_traceProvider.Trace(TraceEventType.Verbose, "{1}: Request HTTP DELETE uri: {0}", uri.AbsoluteUri, "ElasticsearchContextWarmer");

			var response = await _client.DeleteAsync(uri, _cancellationTokenSource.Token).ConfigureAwait(false);

			if (response.StatusCode == HttpStatusCode.OK)
			{
				resultDetails.PayloadResult = true;
				return resultDetails;
			}

			_traceProvider.Trace(TraceEventType.Error, $"ElasticsearchContextWarmer: Could Not Execute Warmer Delete {warmerName}");
			throw new ElasticsearchCrudException($"ElasticsearchContextWarmer: Could Not Execute Warmer Delete {warmerName}");
		}
	}
}
