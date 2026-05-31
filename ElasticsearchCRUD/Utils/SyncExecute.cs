using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ElasticsearchCRUD.Model;
using ElasticsearchCRUD.Tracing;

namespace ElasticsearchCRUD.Utils
{
	public class SyncExecute
	{
		private readonly ITraceProvider _traceProvider;

		public SyncExecute(ITraceProvider traceProvider)
		{
			_traceProvider = traceProvider;
		}

		public T Execute<T>(Func<Task<ResultDetails<T>>> method)
		{
			try
			{
				var result = ExecuteAsync(method).GetAwaiter().GetResult();
				return result.PayloadResult;
			}
			catch (ElasticsearchCrudException)
			{
				throw;
			}
			catch (HttpRequestException)
			{
				throw;
			}
			catch (Exception ex)
			{
				_traceProvider.Trace(TraceEventType.Error, $"SyncExecute: Execute error for Type {typeof(T)}: {ex.Message}");
				throw new ElasticsearchCrudException(ex.Message);
			}
		}

		public ResultDetails<T> ExecuteResultDetails<T>(Func<Task<ResultDetails<T>>> method)
		{
			try
			{
				var result = ExecuteAsync(method).GetAwaiter().GetResult();
				return result;
			}
			catch (ElasticsearchCrudException)
			{
				throw;
			}
			catch (HttpRequestException)
			{
				throw;
			}
			catch (Exception ex)
			{
				_traceProvider.Trace(TraceEventType.Error, $"SyncExecute: ExecuteResultDetails error for Type {typeof(T)}: {ex.Message}");
				throw new ElasticsearchCrudException(ex.Message);
			}
		}

		private static async Task<ResultDetails<T>> ExecuteAsync<T>(Func<Task<ResultDetails<T>>> method)
		{
			var result = await method();
			return result;
		}
	}
}
