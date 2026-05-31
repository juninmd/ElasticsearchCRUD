using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using ElasticsearchCRUD.ContextAddDeleteUpdate.IndexModel;
using ElasticsearchCRUD.ContextAddDeleteUpdate.IndexModel.MappingModel;
using ElasticsearchCRUD.ContextAddDeleteUpdate.IndexModel.SettingsModel;
using ElasticsearchCRUD.Model;
using ElasticsearchCRUD.Tracing;
using ElasticsearchCRUD.Utils;

namespace ElasticsearchCRUD.ContextAddDeleteUpdate
{
	public class ElasticsearchSerializer  : IDisposable
	{
		private readonly ITraceProvider _traceProvider;
		private ElasticsearchCrudJsonWriter _elasticsearchCrudJsonWriter;
		private readonly ElasticsearchSerializerConfiguration _elasticsearchSerializerConfiguration;
		private readonly bool _saveChangesAndInitMappingsForChildDocuments;
		private ElasticSerializationResult _elasticSerializationResult = new ElasticSerializationResult();
		private readonly IndexMappings _indexMappings;

		public ElasticsearchSerializer(ITraceProvider traceProvider, ElasticsearchSerializerConfiguration elasticsearchSerializerConfiguration, bool saveChangesAndInitMappingsForChildDocuments)
		{
			_elasticsearchSerializerConfiguration = elasticsearchSerializerConfiguration;
			_saveChangesAndInitMappingsForChildDocuments = saveChangesAndInitMappingsForChildDocuments;
			_traceProvider = traceProvider;
			_indexMappings = new IndexMappings(_traceProvider, _elasticsearchSerializerConfiguration);
			_elasticSerializationResult.IndexMappings = _indexMappings;
		}

		public ElasticSerializationResult Serialize(IEnumerable<EntityContextInfo> entities)
		{
			if (entities == null)
			{
				return null;
			}

			_elasticSerializationResult = new ElasticSerializationResult();
			_elasticsearchCrudJsonWriter = new ElasticsearchCrudJsonWriter();

			foreach (var entity in entities)
			{
				string index = _elasticsearchSerializerConfiguration.ElasticsearchMappingResolver.GetElasticSearchMapping(entity.EntityType).GetIndexForType(entity.EntityType);
				MappingUtils.GuardAgainstBadIndexName(index);

				if (_saveChangesAndInitMappingsForChildDocuments)
				{
					_indexMappings.CreateIndexSettingsForDocument(index, new IndexSettings{NumberOfShards=5,NumberOfReplicas=1}, new IndexAliases(), new IndexWarmers() );
					_indexMappings.CreatePropertyMappingForTopDocument(entity, new MappingDefinition{Index=index});
				}

				if (entity.DeleteDocument)
				{
					DeleteEntity(entity);
				}
				else
				{
					AddUpdateEntity(entity);
				}
			}

			_elasticsearchCrudJsonWriter.Dispose();
			_elasticSerializationResult.Content = _elasticsearchCrudJsonWriter.Stringbuilder.ToString();
			_elasticSerializationResult.IndexMappings = _indexMappings;
			return _elasticSerializationResult;
		}

		private void DeleteEntity(EntityContextInfo entityInfo)
		{
			var elasticSearchMapping = _elasticsearchSerializerConfiguration.ElasticsearchMappingResolver.GetElasticSearchMapping(entityInfo.EntityType);
			elasticSearchMapping.TraceProvider = _traceProvider;
			elasticSearchMapping.SaveChildObjectsAsWellAsParent = _elasticsearchSerializerConfiguration.SaveChildObjectsAsWellAsParent;
			elasticSearchMapping.ProcessChildDocumentsAsSeparateChildIndex = _elasticsearchSerializerConfiguration.ProcessChildDocumentsAsSeparateChildIndex;
			_elasticsearchCrudJsonWriter.JsonWriter.WriteStartObject();

			_elasticsearchCrudJsonWriter.JsonWriter.WritePropertyName("delete");

			// Write the batch "index" operation header
			_elasticsearchCrudJsonWriter.JsonWriter.WriteStartObject();
			WriteValue("_index", elasticSearchMapping.GetIndexForType(entityInfo.EntityType));
			WriteValue("_type", elasticSearchMapping.GetDocumentType(entityInfo.EntityType));
			WriteValue("_id", entityInfo.Id);
			if (entityInfo.RoutingDefinition.ParentId != null && _elasticsearchSerializerConfiguration.ProcessChildDocumentsAsSeparateChildIndex)
			{
				// It's a document which belongs to a parent
				WriteValue("_parent", entityInfo.RoutingDefinition.ParentId);
			}
			if (entityInfo.RoutingDefinition.RoutingId != null && _elasticsearchSerializerConfiguration.ProcessChildDocumentsAsSeparateChildIndex &&
				_elasticsearchSerializerConfiguration.UserDefinedRouting)
			{
				// It's a document which has a specific route
				WriteValue("_routing", entityInfo.RoutingDefinition.RoutingId);
			}
			_elasticsearchCrudJsonWriter.JsonWriter.WriteEndObject();
			_elasticsearchCrudJsonWriter.JsonWriter.WriteEndObject();	
			_elasticsearchCrudJsonWriter.JsonWriter.WriteRaw("\n");
		}
	
		private void AddUpdateEntity(EntityContextInfo entityInfo)
		{
			var elasticSearchMapping = _elasticsearchSerializerConfiguration.ElasticsearchMappingResolver.GetElasticSearchMapping(entityInfo.EntityType);
			elasticSearchMapping.TraceProvider = _traceProvider;
			elasticSearchMapping.SaveChildObjectsAsWellAsParent = _elasticsearchSerializerConfiguration.SaveChildObjectsAsWellAsParent;
			elasticSearchMapping.ProcessChildDocumentsAsSeparateChildIndex = _elasticsearchSerializerConfiguration.ProcessChildDocumentsAsSeparateChildIndex;

			CreateBulkContentForParentDocument(entityInfo, elasticSearchMapping);

			if (_elasticsearchSerializerConfiguration.ProcessChildDocumentsAsSeparateChildIndex)
			{
				if (elasticSearchMapping.ChildIndexEntities.Count > 0)
				{
					// Only save the top level items now
					elasticSearchMapping.SaveChildObjectsAsWellAsParent = false;
					foreach (var item in elasticSearchMapping.ChildIndexEntities)
					{
						CreateBulkContentForChildDocument(entityInfo, elasticSearchMapping, item);
					}
				}
			}
			elasticSearchMapping.ChildIndexEntities.Clear();			
		}

		private void CreateBulkContentForParentDocument(EntityContextInfo entityInfo, ElasticsearchMapping elasticsearchMapping)
		{
			WriteBulkIndexHeader(
				elasticsearchMapping.GetIndexForType(entityInfo.EntityType),
				elasticsearchMapping.GetDocumentType(entityInfo.EntityType),
				entityInfo.Id,
				entityInfo.RoutingDefinition.ParentId,
				entityInfo.RoutingDefinition.RoutingId
			);

			elasticsearchMapping.MapEntityValues(entityInfo, _elasticsearchCrudJsonWriter, true);

			_elasticsearchCrudJsonWriter.JsonWriter.WriteEndObject();
			_elasticsearchCrudJsonWriter.JsonWriter.WriteRaw("\n");
		}

		private void CreateBulkContentForChildDocument(EntityContextInfo entityInfo, ElasticsearchMapping elasticsearchMapping,
			EntityContextInfo item)
		{
			var childMapping =
				_elasticsearchSerializerConfiguration.ElasticsearchMappingResolver.GetElasticSearchMapping(item.EntityType);

			WriteBulkIndexHeader(
				childMapping.GetIndexForType(entityInfo.EntityType),
				childMapping.GetDocumentType(item.EntityType),
				item.Id,
				item.RoutingDefinition.ParentId,
				_elasticsearchSerializerConfiguration.UserDefinedRouting ? item.RoutingDefinition.RoutingId : null
			);

			childMapping.MapEntityValues(item, _elasticsearchCrudJsonWriter, true);

			_elasticsearchCrudJsonWriter.JsonWriter.WriteEndObject();
			_elasticsearchCrudJsonWriter.JsonWriter.WriteRaw("\n");
		}

		private void WriteBulkIndexHeader(string index, string type, object id, object parentId, object routingId)
		{
			_elasticsearchCrudJsonWriter.JsonWriter.WriteStartObject();
			_elasticsearchCrudJsonWriter.JsonWriter.WritePropertyName("index");
			_elasticsearchCrudJsonWriter.JsonWriter.WriteStartObject();
			WriteValue("_index", index);
			WriteValue("_type", type);
			WriteValue("_id", id);
			if (parentId != null && _elasticsearchSerializerConfiguration.ProcessChildDocumentsAsSeparateChildIndex)
			{
				WriteValue("_parent", parentId);
			}
			if (routingId != null && _elasticsearchSerializerConfiguration.UserDefinedRouting)
			{
				WriteValue("_routing", routingId);
			}
			_elasticsearchCrudJsonWriter.JsonWriter.WriteEndObject();
			_elasticsearchCrudJsonWriter.JsonWriter.WriteEndObject();
			_elasticsearchCrudJsonWriter.JsonWriter.WriteRaw("\n");
			_elasticsearchCrudJsonWriter.JsonWriter.WriteStartObject();
		}

		private void WriteValue(string key, object valueObj)
		{
			_elasticsearchCrudJsonWriter.JsonWriter.WritePropertyName(key);
			_elasticsearchCrudJsonWriter.JsonWriter.WriteValue(valueObj);
		}

		public void Dispose()
		{
			_elasticsearchCrudJsonWriter.Dispose();
		}
	}
}
