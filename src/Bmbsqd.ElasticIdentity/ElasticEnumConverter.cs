using Newtonsoft.Json.Converters;

namespace Bmbsqd.ElasticIdentity
{
	public class ElasticEnumConverter : StringEnumConverter
	{
		public ElasticEnumConverter()
		{
			AllowIntegerValues = true;
			CamelCaseText = true;
		}
	}
}