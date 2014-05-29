using Nest;

namespace Bmbsqd.ElasticIdentity
{
	public class ElasticUserEmail : ElasticUserConfirmed
	{
		[ElasticProperty( IncludeInAll = false, Index = FieldIndexOption.not_analyzed )]
		public string Address { get; set; }

	}
}