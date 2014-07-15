using Nest;

namespace Bmbsqd.ElasticIdentity
{
	public class ElasticUserPhone : ElasticUserConfirmed
	{
		[ElasticProperty( IncludeInAll = false, Index = FieldIndexOption.NotAnalyzed )]
		public string Number { get; set; }
	}
}