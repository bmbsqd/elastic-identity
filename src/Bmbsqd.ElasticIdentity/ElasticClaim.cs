#region MIT License
/*
	The MIT License (MIT)

	Copyright (c) 2013 Bombsquad Inc

	Permission is hereby granted, free of charge, to any person obtaining a copy of
	this software and associated documentation files (the "Software"), to deal in
	the Software without restriction, including without limitation the rights to
	use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
	the Software, and to permit persons to whom the Software is furnished to do so,
	subject to the following conditions:

	The above copyright notice and this permission notice shall be included in all
	copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
	FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
	COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
	IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
	CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion
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