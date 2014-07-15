using Nest;

namespace Bmbsqd.ElasticIdentity
{
	public class ElasticUserEmail : ElasticUserConfirmed
	{
		[ElasticProperty( IncludeInAll = false, Index = FieldIndexOption.NotAnalyzed )]
		public string Address { get; set; }
	}
}