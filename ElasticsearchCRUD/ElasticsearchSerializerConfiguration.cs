namespace ElasticsearchCRUD
{
	public class ElasticsearchSerializerConfiguration
	{
		public ElasticsearchSerializerConfiguration(
			IElasticsearchMappingResolver elasticsearchMappingResolver,
			bool saveChildObjectsAsWellAsParent = true,
			bool processChildDocumentsAsSeparateChildIndex = false,
			bool userDefinedRouting = false)
		{
			ElasticsearchMappingResolver = elasticsearchMappingResolver;
			SaveChildObjectsAsWellAsParent = saveChildObjectsAsWellAsParent;
			ProcessChildDocumentsAsSeparateChildIndex = processChildDocumentsAsSeparateChildIndex;
			UserDefinedRouting = userDefinedRouting;
		}

		public IElasticsearchMappingResolver ElasticsearchMappingResolver { get; }
		public bool SaveChildObjectsAsWellAsParent { get; }
		public bool ProcessChildDocumentsAsSeparateChildIndex { get; }
		public bool UserDefinedRouting { get; }
	}
}
