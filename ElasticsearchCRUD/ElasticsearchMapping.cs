using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using ElasticsearchCRUD.Model;
using ElasticsearchCRUD.Tracing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ElasticsearchCRUD
{
	public partial class ElasticsearchMapping
	{
		protected HashSet<string> SerializedTypes = new HashSet<string>();
		public ITraceProvider TraceProvider = new NullTraceProvider();
		public bool SaveChildObjectsAsWellAsParent { get; set; }
		public bool ProcessChildDocumentsAsSeparateChildIndex { get; set; }

		public List<EntityContextInfo> ChildIndexEntities = new List<EntityContextInfo>();

		protected void MapValue(string key, object valueObj, JsonWriter writer)
		{
			writer.WritePropertyName(key);
			writer.WriteValue(valueObj);
		}

		protected bool IsPropertyACollection(PropertyInfo property)
		{
			if (property.PropertyType.FullName == "System.String" || property.PropertyType.FullName == "System.Decimal")
			{
				return false;
			}
			return property.PropertyType.GetInterface(typeof(IEnumerable<>).FullName) != null;
		}

		public virtual object ParseEntity(JToken source, Type type)
		{
			return JsonConvert.DeserializeObject(source.ToString(), type);
		}

		public virtual string GetDocumentType(Type type)
		{
			if (type.BaseType != null && type.Namespace == "System.Data.Entity.DynamicProxies")
			{
				type = type.BaseType;
			}
			return type.Name.ToLower();
		}

		public virtual Type GetEntityDocumentType(Type type)
		{
			if (type.BaseType != null && type.Namespace == "System.Data.Entity.DynamicProxies")
			{
				type = type.BaseType;
			}
			return type;
		}

		public virtual string GetIndexForType(Type type)
		{
			if (type.BaseType != null && type.Namespace == "System.Data.Entity.DynamicProxies")
			{
				type = type.BaseType;
			}
			return type.Name.ToLower() + "s";
		}

		public string GetElasticsearchType(Type propertyType)
		{
			switch (propertyType.FullName)
			{
				case "System.Boolean":
					return "boolean";
				case "System.Byte":
					return "byte";
				case "System.SByte":
					return "byte";
				case "System.Double":
					return "double";
				case "System.Single":
					return "float";
				case "System.Int32":
					return "integer";
				case "System.UInt32":
					return "integer";
				case "System.Int64":
					return "long";
				case "System.UInt64":
					return "long";
				case "System.Int16":
					return "short";
				case "System.UInt16":
					return "short";
				default:
					return "string";
			}
		}
	}
}
