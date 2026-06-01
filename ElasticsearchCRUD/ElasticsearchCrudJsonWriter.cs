using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace ElasticsearchCRUD
{
	public class ElasticsearchCrudJsonWriter : IDisposable
	{
		public ElasticsearchCrudJsonWriter()
		{
			Stringbuilder = new StringBuilder();
			JsonWriter = new JsonTextWriter(new StringWriter(Stringbuilder, CultureInfo.InvariantCulture)) { CloseOutput = true };
		}

		public ElasticsearchCrudJsonWriter(StringBuilder stringbuilder)
		{
			Stringbuilder = stringbuilder;
			JsonWriter = new JsonTextWriter(new StringWriter(Stringbuilder, CultureInfo.InvariantCulture)) { CloseOutput = true };
		}

		public ElasticsearchCrudJsonWriter? ElasticsearchCrudJsonWriterChildItem { get; set; }

		public StringBuilder Stringbuilder { get; }

		public JsonWriter JsonWriter { get; private set; }

		private bool _isDisposed;
		public void Dispose()
		{
			if (_isDisposed) return;
			_isDisposed = true;
			JsonWriter.Close();
			JsonWriter = null;
		}

		public string GetJsonString()
		{
			var sb = new StringBuilder();
			var jsonString = new List<string> { Stringbuilder.ToString() };

			AppendDataToTrace(ElasticsearchCrudJsonWriterChildItem, jsonString);

			for (var i = jsonString.Count - 1; i >= 0; i--)
			{
				sb.Append(jsonString[i]);
			}

			return sb.ToString();
		}

		public void AppendDataToTrace(ElasticsearchCrudJsonWriter elasticsearchCrudJsonWriterChildItem, List<string> jsonString)
		{
			while (elasticsearchCrudJsonWriterChildItem != null)
			{
				jsonString.Add(elasticsearchCrudJsonWriterChildItem.Stringbuilder.ToString());
				elasticsearchCrudJsonWriterChildItem = elasticsearchCrudJsonWriterChildItem.ElasticsearchCrudJsonWriterChildItem;
			}
		}
	}
}
