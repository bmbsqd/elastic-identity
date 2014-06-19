using Nest;

namespace Bmbsqd.ElasticIdentity
{
	public class ElasticUserLoginInfo
	{
        [ElasticProperty(IncludeInAll = false, Index = FieldIndexOption.not_analyzed)]                
        public string LoginProvider { get; set; }

        [ElasticProperty(IncludeInAll = false, Index = FieldIndexOption.not_analyzed)]                
        public string ProviderKey { get; set; }

	}
}