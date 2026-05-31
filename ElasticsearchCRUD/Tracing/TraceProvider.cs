using System;
using System.Diagnostics;
using System.Text;

namespace ElasticsearchCRUD.Tracing
{
	public class TraceProvider : ITraceProvider
	{
		private readonly TraceSource _ts;
		public TraceProvider(string source)
		{
			_ts = new TraceSource(source);
		}
		public void Trace(TraceEventType level, string message, params object[] args)
		{
			var sb = new StringBuilder();
			sb.AppendLine();
			sb.Append($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: ");
			sb.Append(args.Length > 0 ? string.Format(message, args) : message);
			WriteTrace(level, sb.ToString());
		}

		public void Trace(TraceEventType level, Exception ex, string message, params object[] args)
		{
			var sb = new StringBuilder();
			sb.AppendLine();
			sb.Append($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}: ");
			sb.Append(args.Length > 0 ? string.Format(message, args) : message);
			sb.AppendFormat("{2}: {0} , {1}", ex.Message, ex.StackTrace, ex.GetType());
			WriteTrace(level, sb.ToString());
		}

		private void WriteTrace(TraceEventType level, string message)
		{
			_ts.TraceEvent(level,0,message);
		}
	}
}