using Nest;

namespace Bmbsqd.ElasticIdentity
{
	public class ElasticUserPhone : ElasticUserConfirmed
	{
		[ElasticProperty( IncludeInAll = false, Index = FieldIndexOption.not_analyzed )]
		public string Number { get; set; }
	}
}