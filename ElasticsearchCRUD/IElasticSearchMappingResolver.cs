using System;

namespace ElasticsearchCRUD
{
	public interface IElasticsearchMappingResolver
	{
		IElasticSearchMapping GetElasticSearchMapping(Type type);
	}
}