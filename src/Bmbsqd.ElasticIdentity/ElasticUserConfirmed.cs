using System.ComponentModel;
using Nest;
using Newtonsoft.Json;

namespace Bmbsqd.ElasticIdentity
{
	public class ElasticUserConfirmed
	{
		[ElasticProperty( IncludeInAll = false, Index = FieldIndexOption.NotAnalyzed )]
		[JsonProperty( DefaultValueHandling = DefaultValueHandling.Ignore )]
		[DefaultValue( false )]
		public bool IsConfirmed { get; set; }
	}
}