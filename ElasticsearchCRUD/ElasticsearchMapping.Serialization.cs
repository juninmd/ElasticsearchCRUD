using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using ElasticsearchCRUD.ContextAddDeleteUpdate.CoreTypeAttributes;
using ElasticsearchCRUD.ContextAddDeleteUpdate.IndexModel;
using ElasticsearchCRUD.Model;
using ElasticsearchCRUD.Model.GeoModel;
using ElasticsearchCRUD.Tracing;
using Newtonsoft.Json;

namespace ElasticsearchCRUD
{
	public partial class ElasticsearchMapping
	{
		public virtual void MapEntityValues(EntityContextInfo entityInfo, ElasticsearchCrudJsonWriter elasticsearchCrudJsonWriter, bool beginMappingTree = false, bool createPropertyMappings = false)
		{
			try
			{
				BeginNewEntityToDocumentMapping(entityInfo, beginMappingTree);

				TraceProvider.Trace(TraceEventType.Verbose, "ElasticsearchMapping: SerializedTypes new Type added: {0}", GetDocumentType(entityInfo.Document.GetType()));
				var propertyInfo = entityInfo.Document.GetType().GetProperties();
				foreach (var prop in propertyInfo)
				{
					if (!Attribute.IsDefined(prop, typeof(JsonIgnoreAttribute)))
					{
						MapProperty(entityInfo, elasticsearchCrudJsonWriter, prop, createPropertyMappings, beginMappingTree);
					}
				}
			}
			catch (Exception ex)
			{
				TraceProvider.Trace(TraceEventType.Critical, ex, "ElasticsearchMapping: Property is a simple Type: {0}", elasticsearchCrudJsonWriter.GetJsonString());
				throw;
			}
		}

		private void MapProperty(EntityContextInfo entityInfo, ElasticsearchCrudJsonWriter elasticsearchCrudJsonWriter, PropertyInfo prop, bool createPropertyMappings, bool beginMappingTree)
		{
			if (Attribute.IsDefined(prop, typeof(ElasticsearchGeoTypeAttribute)))
			{
				MapGeoProperty(entityInfo, elasticsearchCrudJsonWriter, prop, createPropertyMappings);
			}
			else if (IsPropertyACollection(prop))
			{
				ProcessArrayOrCollection(entityInfo, elasticsearchCrudJsonWriter, prop, createPropertyMappings);
			}
			else if (prop.PropertyType.IsClass && prop.PropertyType.FullName != "System.String" && prop.PropertyType.FullName != "System.Decimal")
			{
				ProcessSingleObject(entityInfo, elasticsearchCrudJsonWriter, prop, createPropertyMappings);
			}
			else
			{
				MapSimpleProperty(entityInfo, elasticsearchCrudJsonWriter, prop, createPropertyMappings, beginMappingTree);
			}
		}

		private void MapGeoProperty(EntityContextInfo entityInfo, ElasticsearchCrudJsonWriter elasticsearchCrudJsonWriter, PropertyInfo prop, bool createPropertyMappings)
		{
			var obj = prop.Name.ToLower();
			if (createPropertyMappings)
			{
				object[] attrs = prop.GetCustomAttributes(typeof(ElasticsearchCoreTypes), true);
				if ((attrs[0] as ElasticsearchCoreTypes) != null)
				{
					elasticsearchCrudJsonWriter.JsonWriter.WritePropertyName(obj);
					elasticsearchCrudJsonWriter.JsonWriter.WriteRawValue((attrs[0] as ElasticsearchCoreTypes).JsonString());
				}
			}
			else
			{
				var data = prop.GetValue(entityInfo.Document) as IGeoType;
				elasticsearchCrudJsonWriter.JsonWriter.WritePropertyName(obj);
				data.WriteJson(elasticsearchCrudJsonWriter);
			}
		}

		private void MapSimpleProperty(EntityContextInfo entityInfo, ElasticsearchCrudJsonWriter elasticsearchCrudJsonWriter, PropertyInfo prop, bool createPropertyMappings, bool beginMappingTree)
		{
			if (ProcessChildDocumentsAsSeparateChildIndex && !beginMappingTree)
			{
				return;
			}

			TraceProvider.Trace(TraceEventType.Verbose, "ElasticsearchMapping: Property is a simple Type: {0}, {1}", prop.Name.ToLower(), prop.PropertyType.FullName);

			if (createPropertyMappings)
			{
				WriteSimplePropertyMapping(elasticsearchCrudJsonWriter, prop);
			}
			else
			{
				MapValue(prop.Name.ToLower(), prop.GetValue(entityInfo.Document), elasticsearchCrudJsonWriter.JsonWriter);
			}
		}

		private void WriteSimplePropertyMapping(ElasticsearchCrudJsonWriter elasticsearchCrudJsonWriter, PropertyInfo prop)
		{
			var obj = prop.Name.ToLower();
			if (Attribute.IsDefined(prop, typeof(ElasticsearchCoreTypes)))
			{
				object[] attrs = prop.GetCustomAttributes(typeof(ElasticsearchCoreTypes), true);
				if ((attrs[0] as ElasticsearchCoreTypes) != null)
				{
					elasticsearchCrudJsonWriter.JsonWriter.WritePropertyName(obj);
					elasticsearchCrudJsonWriter.JsonWriter.WriteRawValue((attrs[0] as ElasticsearchCoreTypes).JsonString());
				}
			}
			else
			{
				elasticsearchCrudJsonWriter.JsonWriter.WritePropertyName(obj);
				if (prop.PropertyType.FullName == "System.DateTime" || prop.PropertyType.FullName == "System.DateTimeOffset")
				{
					elasticsearchCrudJsonWriter.JsonWriter.WriteRawValue("{ \"type\" : \"date\", \"format\": \"dateOptionalTime\"}");
				}
				else
				{
					elasticsearchCrudJsonWriter.JsonWriter.WriteRawValue("{ \"type\" : \"" + GetElasticsearchType(prop.PropertyType) + "\" }");
				}
			}
		}

		private void BeginNewEntityToDocumentMapping(EntityContextInfo entityInfo, bool beginMappingTree)
		{
			if (beginMappingTree)
			{
				SerializedTypes = new HashSet<string>();
				TraceProvider.Trace(TraceEventType.Verbose, "ElasticsearchMapping: Serialize BEGIN for Type: {0}", entityInfo.Document.GetType());
			}
		}

		private void ProcessSingleObject(EntityContextInfo entityInfo, ElasticsearchCrudJsonWriter elasticsearchCrudJsonWriter, PropertyInfo prop, bool createPropertyMappings)
		{
			TraceProvider.Trace(TraceEventType.Verbose, "ElasticsearchMapping: Property is an Object: {0}", prop.ToString());

			if (createPropertyMappings && prop.GetValue(entityInfo.Document) == null)
			{
				prop.SetValue(entityInfo.Document, Activator.CreateInstance(prop.PropertyType));
			}
			if (prop.GetValue(entityInfo.Document) != null && SaveChildObjectsAsWellAsParent)
			{
				var child = GetDocumentType(prop.GetValue(entityInfo.Document).GetType());
				var parent = GetDocumentType(entityInfo.EntityType);
				if (!SerializedTypes.Contains(child + parent))
				{
					SerializedTypes.Add(parent + child);
					if (ProcessChildDocumentsAsSeparateChildIndex)
					{
						ProcessSingleObjectAsChildDocument(entityInfo, elasticsearchCrudJsonWriter, prop, createPropertyMappings);
					}
					else
					{
						ProcessSingleObjectAsNestedObject(entityInfo, elasticsearchCrudJsonWriter, prop, createPropertyMappings);
					}
				}
			}
		}

		private void ProcessArrayOrCollection(EntityContextInfo entityInfo, ElasticsearchCrudJsonWriter elasticsearchCrudJsonWriter, PropertyInfo prop, bool createPropertyMappings)
		{
			TraceProvider.Trace(TraceEventType.Verbose, "ElasticsearchMapping: IsPropertyACollection: {0}", prop.Name.ToLower());

			if (createPropertyMappings && prop.GetValue(entityInfo.Document) == null)
			{
				if (prop.PropertyType.IsArray)
				{
					prop.SetValue(entityInfo.Document, Array.CreateInstance(prop.PropertyType.GetElementType(), 0));
				}
				else
				{
					prop.SetValue(entityInfo.Document, Activator.CreateInstance(prop.PropertyType));
				}
			}

			if (prop.GetValue(entityInfo.Document) != null && SaveChildObjectsAsWellAsParent)
			{
				if (ProcessChildDocumentsAsSeparateChildIndex)
				{
					ProcessArrayOrCollectionAsChildDocument(entityInfo, elasticsearchCrudJsonWriter, prop, createPropertyMappings);
				}
				else
				{
					ProcessArrayOrCollectionAsNestedObject(entityInfo, elasticsearchCrudJsonWriter, prop, createPropertyMappings);
				}
			}
		}

		private void ProcessSingleObjectAsNestedObject(EntityContextInfo entityInfo, ElasticsearchCrudJsonWriter elasticsearchCrudJsonWriter, PropertyInfo prop, bool createPropertyMappings)
		{
			elasticsearchCrudJsonWriter.JsonWriter.WritePropertyName(prop.Name.ToLower());
			elasticsearchCrudJsonWriter.JsonWriter.WriteStartObject();

			if (createPropertyMappings)
			{
				elasticsearchCrudJsonWriter.JsonWriter.WritePropertyName("properties");
				elasticsearchCrudJsonWriter.JsonWriter.WriteStartObject();
			}

			var entity = prop.GetValue(entityInfo.Document);
			var routingDefinition = new RoutingDefinition { ParentId = entityInfo.Id };
			var child = new EntityContextInfo { Document = entity, RoutingDefinition = routingDefinition, EntityType = entity.GetType(), DeleteDocument = entityInfo.DeleteDocument };

			MapEntityValues(child, elasticsearchCrudJsonWriter, false, createPropertyMappings);
			elasticsearchCrudJsonWriter.JsonWriter.WriteEndObject();

			if (createPropertyMappings)
			{
				elasticsearchCrudJsonWriter.JsonWriter.WriteEndObject();
			}
		}

		private void ProcessSingleObjectAsChildDocument(EntityContextInfo entityInfo, ElasticsearchCrudJsonWriter elasticsearchCrudJsonWriter, PropertyInfo prop, bool createPropertyMappings)
		{
			var entity = prop.GetValue(entityInfo.Document);
			CreateChildEntityForDocumentIndex(entityInfo, elasticsearchCrudJsonWriter, entity, createPropertyMappings);
		}

		private void CreateChildEntityForDocumentIndex(EntityContextInfo parentEntityInfo, ElasticsearchCrudJsonWriter elasticsearchCrudJsonWriter, object entity, bool createPropertyMappings)
		{
			var propertyInfo = entity.GetType().GetProperties();
			foreach (var property in propertyInfo)
			{
				if (Attribute.IsDefined(property, typeof(KeyAttribute)) || Attribute.IsDefined(property, typeof(ElasticsearchIdAttribute)))
				{
					var obj = property.GetValue(entity);

					if (obj == null && createPropertyMappings)
					{
						obj = "0";
					}

					RoutingDefinition routingDefinition;
					if (parentEntityInfo.RoutingDefinition.RoutingId != null)
					{
						routingDefinition = new RoutingDefinition { ParentId = parentEntityInfo.Id, RoutingId = parentEntityInfo.RoutingDefinition.RoutingId };
					}
					else
					{
						routingDefinition = new RoutingDefinition { ParentId = parentEntityInfo.Id, RoutingId = parentEntityInfo.Id };
					}

					var child = new EntityContextInfo
					{
						Document = entity,
						RoutingDefinition = routingDefinition,
						EntityType = GetEntityDocumentType(entity.GetType()),
						ParentEntityType = GetEntityDocumentType(parentEntityInfo.EntityType),
						DeleteDocument = parentEntityInfo.DeleteDocument,
						Id = obj.ToString()
					};
					ChildIndexEntities.Add(child);
					MapEntityValues(child, elasticsearchCrudJsonWriter, false, createPropertyMappings);

					return;
				}
			}

			throw new ElasticsearchCrudException("No Key found for child object: " + parentEntityInfo.Document.GetType());
		}

		private void ProcessArrayOrCollectionAsNestedObject(EntityContextInfo entityInfo, ElasticsearchCrudJsonWriter elasticsearchCrudJsonWriter, PropertyInfo prop, bool createPropertyMappings)
		{
			elasticsearchCrudJsonWriter.JsonWriter.WritePropertyName(prop.Name.ToLower());
			TraceProvider.Trace(TraceEventType.Verbose, "ElasticsearchMapping: BEGIN ARRAY or COLLECTION: {0} {1}", prop.Name.ToLower(), elasticsearchCrudJsonWriter.JsonWriter.Path);
			var typeOfEntity = prop.GetValue(entityInfo.Document).GetType().GetGenericArguments();
			if (typeOfEntity.Length > 0)
			{
				var child = GetDocumentType(typeOfEntity[0]);
				var parent = GetDocumentType(entityInfo.EntityType);

				if (!SerializedTypes.Contains(child + parent))
				{
					SerializedTypes.Add(parent + child);
					TraceProvider.Trace(TraceEventType.Verbose,
						"ElasticsearchMapping: SerializedTypes type ok, BEGIN ARRAY or COLLECTION: {0}", typeOfEntity[0]);
					TraceProvider.Trace(TraceEventType.Verbose, "ElasticsearchMapping: SerializedTypes new Type added: {0}",
						GetDocumentType(typeOfEntity[0]));
					MapCollectionOrArray(prop, entityInfo, elasticsearchCrudJsonWriter, createPropertyMappings);
				}
				else
				{
					elasticsearchCrudJsonWriter.JsonWriter.WriteRawValue("null");
				}
			}
			else
			{
				TraceProvider.Trace(TraceEventType.Verbose, "ElasticsearchMapping: BEGIN ARRAY or COLLECTION NOT A GENERIC: {0}",
					prop.Name.ToLower());
				MapCollectionOrArray(prop, entityInfo, elasticsearchCrudJsonWriter, createPropertyMappings);
			}
		}

		private void ProcessArrayOrCollectionAsChildDocument(EntityContextInfo entityInfo, ElasticsearchCrudJsonWriter elasticsearchCrudJsonWriter, PropertyInfo prop, bool createPropertyMappings)
		{
			TraceProvider.Trace(TraceEventType.Verbose, "ElasticsearchMapping: BEGIN ARRAY or COLLECTION: {0} {1}", prop.Name.ToLower(), elasticsearchCrudJsonWriter.JsonWriter.Path);
			var typeOfEntity = prop.GetValue(entityInfo.Document).GetType().GetGenericArguments();
			if (typeOfEntity.Length > 0)
			{
				var child = GetDocumentType(typeOfEntity[0]);
				var parent = GetDocumentType(entityInfo.EntityType);

				if (!SerializedTypes.Contains(child + parent))
				{
					SerializedTypes.Add(parent + child);
					TraceProvider.Trace(TraceEventType.Verbose,
						"ElasticsearchMapping: SerializedTypes type ok, BEGIN ARRAY or COLLECTION: {0}", typeOfEntity[0]);
					TraceProvider.Trace(TraceEventType.Verbose, "ElasticsearchMapping: SerializedTypes new Type added: {0}",
						GetDocumentType(typeOfEntity[0]));

					MapCollectionOrArray(prop, entityInfo, elasticsearchCrudJsonWriter, createPropertyMappings);
				}
			}
			else
			{
				TraceProvider.Trace(TraceEventType.Verbose, "ElasticsearchMapping: BEGIN ARRAY or COLLECTION NOT A GENERIC: {0}",
					prop.Name.ToLower());
				MapCollectionOrArray(prop, entityInfo, elasticsearchCrudJsonWriter, createPropertyMappings);
			}
		}

		protected virtual void MapCollectionOrArray(PropertyInfo prop, EntityContextInfo entityInfo, ElasticsearchCrudJsonWriter elasticsearchCrudJsonWriter, bool createPropertyMappings)
		{
			Type type = prop.PropertyType;

			if (type.HasElementType)
			{
				var ienumerable = (Array)prop.GetValue(entityInfo.Document);
				if (ProcessChildDocumentsAsSeparateChildIndex)
				{
					MapIEnumerableEntitiesForChildIndexes(elasticsearchCrudJsonWriter, ienumerable, entityInfo, prop, createPropertyMappings);
				}
				else
				{
					if (createPropertyMappings)
					{
						MapIEnumerableEntitiesForMapping(elasticsearchCrudJsonWriter, entityInfo, prop, true);
					}
					else
					{
						MapIEnumerableEntities(elasticsearchCrudJsonWriter, ienumerable, entityInfo, false);
					}
				}
			}
			else if (prop.PropertyType.IsGenericType)
			{
				var ienumerable = (IEnumerable)prop.GetValue(entityInfo.Document);

				if (ProcessChildDocumentsAsSeparateChildIndex)
				{
					MapIEnumerableEntitiesForChildIndexes(elasticsearchCrudJsonWriter, ienumerable, entityInfo, prop, createPropertyMappings);
				}
				else
				{
					if (createPropertyMappings)
					{
						MapIEnumerableEntitiesForMapping(elasticsearchCrudJsonWriter, entityInfo, prop, true);
					}
					else
					{
						MapIEnumerableEntities(elasticsearchCrudJsonWriter, ienumerable, entityInfo, false);
					}
				}
			}
		}

		private void MapIEnumerableEntitiesForChildIndexes(ElasticsearchCrudJsonWriter elasticsearchCrudJsonWriter, IEnumerable ienumerable, EntityContextInfo parentEntityInfo, PropertyInfo prop, bool createPropertyMappings)
		{
			if (createPropertyMappings)
			{
				object item;
				if (prop.PropertyType.GenericTypeArguments.Length == 0)
				{
					item = Activator.CreateInstance(prop.PropertyType.GetElementType());
				}
				else
				{
					item = Activator.CreateInstance(prop.PropertyType.GenericTypeArguments[0]);
				}

				CreateChildEntityForDocumentIndex(parentEntityInfo, elasticsearchCrudJsonWriter, item, true);
			}
			else
			{
				if (ienumerable != null)
				{
					foreach (var item in ienumerable)
					{
						CreateChildEntityForDocumentIndex(parentEntityInfo, elasticsearchCrudJsonWriter, item, false);
					}
				}
			}
		}

		private void MapIEnumerableEntitiesForMapping(ElasticsearchCrudJsonWriter elasticsearchCrudJsonWriter,
			 EntityContextInfo parentEntityInfo, PropertyInfo prop, bool createPropertyMappings)
		{
			object item;
			if (prop.PropertyType.GenericTypeArguments.Length == 0)
			{
				item = Activator.CreateInstance(prop.PropertyType.GetElementType());
			}
			else
			{
				item = Activator.CreateInstance(prop.PropertyType.GenericTypeArguments[0]);
			}

			var typeofArrayItem = item.GetType();
			if (typeofArrayItem.IsClass && typeofArrayItem.FullName != "System.String" &&
				typeofArrayItem.FullName != "System.Decimal")
			{
				elasticsearchCrudJsonWriter.JsonWriter.WriteStartObject();

				if (Attribute.IsDefined(prop, typeof(ElasticsearchNestedAttribute)))
				{
					elasticsearchCrudJsonWriter.JsonWriter.WritePropertyName("type");
					elasticsearchCrudJsonWriter.JsonWriter.WriteValue("nested");

					object[] attrs = prop.GetCustomAttributes(typeof(ElasticsearchNestedAttribute), true);

					if ((attrs[0] as ElasticsearchNestedAttribute) != null)
					{
						(attrs[0] as ElasticsearchNestedAttribute).WriteJson(elasticsearchCrudJsonWriter);
					}
				}

				elasticsearchCrudJsonWriter.JsonWriter.WritePropertyName("properties");
				elasticsearchCrudJsonWriter.JsonWriter.WriteStartObject();

				var routingDefinition = new RoutingDefinition
				{
					ParentId = parentEntityInfo.Id,
					RoutingId = parentEntityInfo.RoutingDefinition.RoutingId
				};
				var child = new EntityContextInfo
				{
					Document = item,
					RoutingDefinition = routingDefinition,
					EntityType = item.GetType(),
					DeleteDocument = parentEntityInfo.DeleteDocument
				};
				MapEntityValues(child, elasticsearchCrudJsonWriter, false, createPropertyMappings);
				elasticsearchCrudJsonWriter.JsonWriter.WriteEndObject();
				elasticsearchCrudJsonWriter.JsonWriter.WriteEndObject();
			}
			else
			{
				elasticsearchCrudJsonWriter.JsonWriter.WriteStartObject();
				elasticsearchCrudJsonWriter.JsonWriter.WritePropertyName("type");
				elasticsearchCrudJsonWriter.JsonWriter.WriteValue(GetElasticsearchType(item.GetType()));
				elasticsearchCrudJsonWriter.JsonWriter.WriteEndObject();
			}
		}

		private void MapIEnumerableEntities(ElasticsearchCrudJsonWriter elasticsearchCrudJsonWriter, IEnumerable ienumerable, EntityContextInfo parentEntityInfo, bool createPropertyMappings)
		{
			string json = null;
			bool isSimpleArrayOrCollection = true;
			bool doProccessingIfTheIEnumerableHasAtLeastOneItem = false;
			if (ienumerable != null)
			{
				var sbCollection = new StringBuilder();
				sbCollection.Append("[");
				foreach (var item in ienumerable)
				{
					doProccessingIfTheIEnumerableHasAtLeastOneItem = true;

					var childElasticsearchCrudJsonWriter = new ElasticsearchCrudJsonWriter(sbCollection);
					elasticsearchCrudJsonWriter.ElasticsearchCrudJsonWriterChildItem = childElasticsearchCrudJsonWriter;

					var typeofArrayItem = item.GetType();
					if (typeofArrayItem.IsClass && typeofArrayItem.FullName != "System.String" &&
						typeofArrayItem.FullName != "System.Decimal")
					{
						isSimpleArrayOrCollection = false;
						childElasticsearchCrudJsonWriter.JsonWriter.WriteStartObject();
						var routingDefinition = new RoutingDefinition { ParentId = parentEntityInfo.Id, RoutingId = parentEntityInfo.RoutingDefinition.RoutingId };
						var child = new EntityContextInfo { Document = item, RoutingDefinition = routingDefinition, EntityType = item.GetType(), DeleteDocument = parentEntityInfo.DeleteDocument };
						MapEntityValues(child, childElasticsearchCrudJsonWriter, false, createPropertyMappings);
						childElasticsearchCrudJsonWriter.JsonWriter.WriteEndObject();
					}
					else
					{
						json = JsonConvert.SerializeObject(ienumerable);
						break;
					}
					sbCollection.Append(",");
				}

				if (isSimpleArrayOrCollection && doProccessingIfTheIEnumerableHasAtLeastOneItem)
				{
					elasticsearchCrudJsonWriter.JsonWriter.WriteRawValue(json);
				}
				else
				{
					if (doProccessingIfTheIEnumerableHasAtLeastOneItem)
					{
						sbCollection.Remove(sbCollection.Length - 1, 1);
					}

					sbCollection.Append("]");
					elasticsearchCrudJsonWriter.JsonWriter.WriteRawValue(sbCollection.ToString());
				}
			}
			else
			{
				elasticsearchCrudJsonWriter.JsonWriter.WriteRawValue("");
			}
		}
	}
}
