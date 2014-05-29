using System.ComponentModel;
using Nest;
using Newtonsoft.Json;

namespace Bmbsqd.ElasticIdentity
{
	public class ElasticUserConfirmed
	{
		[ElasticProperty( IncludeInAll = false, Index = FieldIndexOption.not_analyzed )]
		[JsonProperty( DefaultValueHandling = DefaultValueHandling.Ignore )]
		[DefaultValue( false )]
		public bool IsConfirmed { get; set; }
	}
}