using System;
using System.Security.Claims;

namespace Bmbsqd.ElasticIdentity
{
	public class ElasticClaim : IEquatable<ElasticClaim>
	{
		public string Type { get; set; }
		public string Value { get; set; }

		public ElasticClaim()
		{
		}

		public ElasticClaim( string type, string value )
		{
			Type = type;
			Value = value;
		}

		public static implicit operator Claim( ElasticClaim claim )
		{
			return new Claim( claim.Type, claim.Value );
		}

		public static implicit operator ElasticClaim( Claim claim )
		{
			return new ElasticClaim {
				Type = claim.Type,
				Value = claim.Value
			};
		}

		public Claim AsClaim()
		{
			return this;
		}

		public bool Equals( ElasticClaim other )
		{
			if( ReferenceEquals( null, other ) ) return false;
			if( ReferenceEquals( this, other ) ) return true;
			return string.Equals( Value, other.Value ) && string.Equals( Type, other.Type );
		}

		public override bool Equals( object obj )
		{
			if( ReferenceEquals( null, obj ) ) return false;
			if( ReferenceEquals( this, obj ) ) return true;
			if( obj.GetType() != GetType() ) return false;
			return Equals( (ElasticClaim)obj );
		}

		public override int GetHashCode()
		{
			unchecked {
				return ((Value != null ? Value.GetHashCode() : 0)*397) ^ (Type != null ? Type.GetHashCode() : 0);
			}
		}

		public static bool operator ==( ElasticClaim left, ElasticClaim right )
		{
			return Equals( left, right );
		}

		public static bool operator !=( ElasticClaim left, ElasticClaim right )
		{
			return !Equals( left, right );
		}

		public override string ToString()
		{
			return Type + ": " + Value;
		}
	}
}