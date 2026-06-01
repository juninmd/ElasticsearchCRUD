using System;

namespace ElasticsearchCRUD
{
	public class ElasticsearchCrudException(string message) : Exception(message);
}
