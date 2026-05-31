using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using ElasticsearchCRUD.ContextSearch.SearchModel;
using ElasticsearchCRUD.Model;
using ElasticsearchCRUD.Model.GeoModel;
using ElasticsearchCRUD.Tracing;
using ElasticsearchCRUD.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ElasticsearchCRUD.ContextSearch
{
	public class SearchRequest
	{
		private readonly ITraceProvider _traceProvider;
		private readonly CancellationTokenSource _cancellationTokenSource;
		private readonly ElasticsearchSerializerConfiguration _elasticsearchSerializerConfiguration;
		private readonly HttpClient _client;
		private readonly string _connectionString;

		public SearchRequest(ITraceProvider traceProvider, CancellationTokenSource cancellationTokenSource, ElasticsearchSerializerConfiguration elasticsearchSerializerConfiguration, HttpClient client, string connectionString)
		{
			_traceProvider = traceProvider;
			_cancellationTokenSource = cancellationTokenSource;
			_elasticsearchSerializerConfiguration = elasticsearchSerializerConfiguration;
			_client = client;
			_connectionString = connectionString;
		}

		public async Task<ResultDetails<SearchResult<T>>> PostSearchAsync<T>(string jsonContent, string scrollId, ScanAndScrollConfiguration scanAndScrollConfiguration, SearchUrlParameters searchUrlParameters)
		{
			_traceProvider.Trace(TraceEventType.Verbose, "{2}: Request for search: {0}, content: {1}", typeof(T), jsonContent, "Search");

			var urlParams = "";
			if (searchUrlParameters != null)
			{
				urlParams = searchUrlParameters.GetUrlParameters();
			}
			var elasticSearchMapping = _elasticsearchSerializerConfiguration.ElasticsearchMappingResolver.GetElasticSearchMapping(typeof(T));
			var elasticsearchUrlForEntityGet = string.Format("{0}/{1}/{2}/_search{3}", _connectionString, elasticSearchMapping.GetIndexForType(typeof(T)), elasticSearchMapping.GetDocumentType(typeof(T)), urlParams);

			if (!string.IsNullOrEmpty(scrollId))
			{
				elasticsearchUrlForEntityGet = string.Format("{0}/{1}{2}", _connectionString ,scanAndScrollConfiguration.GetScrollScanUrlForRunning(),scrollId);
			}

			var uri = new Uri(elasticsearchUrlForEntityGet);

			var result = await PostInteranlSearchAsync<T>(jsonContent, uri);
			return result;
		}

		public ResultDetails<SearchResult<T>> PostSearch<T>(string jsonContent, string scrollId, ScanAndScrollConfiguration scanAndScrollConfiguration, SearchUrlParameters searchUrlParameters)
		{
			var syncExecutor = new SyncExecute(_traceProvider);
			return syncExecutor.ExecuteResultDetails(() => PostSearchAsync<T>(jsonContent, scrollId, scanAndScrollConfiguration, searchUrlParameters));
		}

		public async Task<ResultDetails<SearchResult<T>>> PostSearchCreateScanAndScrollAsync<T>(string jsonContent, ScanAndScrollConfiguration scanAndScrollConfiguration)
		{
			_traceProvider.Trace(TraceEventType.Verbose, "{2}: Request for search create scan ans scroll: {0}, content: {1}", typeof (T), jsonContent, "Search");

			var elasticSearchMapping = _elasticsearchSerializerConfiguration.ElasticsearchMappingResolver.GetElasticSearchMapping(typeof (T));
			var elasticsearchUrlForEntityGet = string.Format("{0}/{1}/{2}/_search", _connectionString,
				elasticSearchMapping.GetIndexForType(typeof (T)), elasticSearchMapping.GetDocumentType(typeof (T)));

			elasticsearchUrlForEntityGet = elasticsearchUrlForEntityGet + "?" + scanAndScrollConfiguration.GetScrollScanUrlForSetup();

			var uri = new Uri(elasticsearchUrlForEntityGet);
			var result = await PostInteranlSearchAsync<T>(jsonContent, uri);
			return result;
		}

		public ResultDetails<SearchResult<T>> PostSearchCreateScanAndScroll<T>(string jsonContent, ScanAndScrollConfiguration scanAndScrollConfiguration)
		{
			var syncExecutor = new SyncExecute(_traceProvider);
			return syncExecutor.ExecuteResultDetails(() => PostSearchCreateScanAndScrollAsync<T>(jsonContent, scanAndScrollConfiguration));
		}

		public async Task<ResultDetails<bool>> PostSearchExistsAsync<T>(string jsonContent, SearchUrlParameters searchUrlParameters)
		{
			_traceProvider.Trace(TraceEventType.Verbose, "{2}: Request for search exists: {0}, content: {1}", typeof(T), jsonContent, "Search");

			var urlParams = searchUrlParameters?.GetUrlParameters() ?? "";
			var elasticSearchMapping = _elasticsearchSerializerConfiguration.ElasticsearchMappingResolver.GetElasticSearchMapping(typeof(T));
			var uri = new Uri(string.Format("{0}/{1}/{2}/_search/exists{3}", _connectionString, elasticSearchMapping.GetIndexForType(typeof(T)), elasticSearchMapping.GetDocumentType(typeof(T)), urlParams));

			return await PostSearchInternalAsync<bool>(jsonContent, uri, ReadExistsResponse);
		}

		private static bool ReadExistsResponse(JObject responseObject)
		{
			return (bool)responseObject["exists"];
		}

		public bool PostSearchExists<T>(string jsonContent, SearchUrlParameters searchUrlParameters)
		{
			var syncExecutor = new SyncExecute(_traceProvider);
			return syncExecutor.ExecuteResultDetails(() => PostSearchExistsAsync<T>(jsonContent, searchUrlParameters)).PayloadResult;
		}

		private async Task<ResultDetails<SearchResult<T>>> PostInteranlSearchAsync<T>(string jsonContent, Uri uri)
		{
			return await PostSearchInternalAsync<SearchResult<T>>(jsonContent, uri, ReadSearchResponse<T>);
		}

		private static SearchResult<T> ReadSearchResponse<T>(JObject responseObject)
		{
			var ser = new JsonSerializer();
			ser.Converters.Add(new GeoShapeGeometryCollectionGeometriesConverter());
			return responseObject.ToObject<SearchResult<T>>(ser);
		}

		private async Task<ResultDetails<TResult>> PostSearchInternalAsync<TResult>(string jsonContent, Uri uri, Func<JObject, TResult> responseReader)
		{
			_traceProvider.Trace(TraceEventType.Verbose, "{2}: Request for search: {0}, content: {1}", typeof(TResult), jsonContent, "Search");
			var resultDetails = new ResultDetails<TResult>
			{
				Status = HttpStatusCode.InternalServerError,
				RequestBody = jsonContent
			};

			try
			{
				_traceProvider.Trace(TraceEventType.Verbose, "{1}: Request HTTP POST uri: {0}", uri.AbsoluteUri, "Search");
				var content = new StringContent(jsonContent);

				content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
				resultDetails.RequestUrl = uri.ToString();
				var response = await _client.PostAsync(uri, content, _cancellationTokenSource.Token).ConfigureAwait(true);

				resultDetails.Status = response.StatusCode;
				if (response.StatusCode != HttpStatusCode.OK)
				{
					_traceProvider.Trace(TraceEventType.Warning, "{2}: PostSearchAsync response status code: {0}, {1}", response.StatusCode, response.ReasonPhrase, "Search");
					var errorInfo = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
					resultDetails.Description = errorInfo;

					if (response.StatusCode == HttpStatusCode.BadRequest && errorInfo.Contains("RoutingMissingException"))
					{
						throw new ElasticsearchCrudException("HttpStatusCode.BadRequest: RoutingMissingException, adding the parent Id if this is a child item...");
					}

					return resultDetails;
				}

				var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(true);
				_traceProvider.Trace(TraceEventType.Verbose, "{1}: Post Request response: {0}", responseString, "Search");
				var responseObject = JObject.Parse(responseString);
				resultDetails.PayloadResult = responseReader(responseObject);
				return resultDetails;
			}
			catch (OperationCanceledException oex)
			{
				_traceProvider.Trace(TraceEventType.Verbose, oex, "{1}: Post Request OperationCanceledException: {0}", oex.Message, "Search");
				return resultDetails;
			}
		}
	}
}
