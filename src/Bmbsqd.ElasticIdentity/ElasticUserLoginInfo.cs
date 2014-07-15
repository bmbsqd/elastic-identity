using Nest;

namespace Bmbsqd.ElasticIdentity
{
	public class ElasticUserLoginInfo
	{
        [ElasticProperty(IncludeInAll = false, Index = FieldIndexOption.NotAnalyzed)]                
        public string LoginProvider { get; set; }

        [ElasticProperty(IncludeInAll = false, Index = FieldIndexOption.NotAnalyzed)]                
        public string ProviderKey { get; set; }

	}
}