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
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.AspNet.Identity;
using Nest;
using Newtonsoft.Json;

namespace Bmbsqd.ElasticIdentity
{
	[ElasticType( IdProperty = "userName" )]
	public class ElasticUser : IUser
	{
		private readonly List<ElasticUserLoginInfo> _logins;
		private readonly HashSet<ElasticClaim> _claims;
		private readonly HashSet<string> _roles;
		private string _userName;

		public ElasticUser()
		{
            _logins = new List<ElasticUserLoginInfo>();
			_claims = new HashSet<ElasticClaim>();
			_roles = new HashSet<string>();
		}

		public ElasticUser( string userName )
			: this()
		{
			UserName = userName;
		}

		[JsonIgnore]
		[ElasticProperty( OptOut = true )]
		public string Id
		{
			get { return UserName; }
		}

		[ElasticProperty( Analyzer = "lowercaseKeyword", IncludeInAll = false )]
		public string UserName
		{
			get { return _userName; }
			set { _userName = UserNameUtils.FormatUserName( value ); }
		}

		[ElasticProperty( IncludeInAll = false, Index = FieldIndexOption.not_analyzed )]
		[JsonProperty( DefaultValueHandling = DefaultValueHandling.Ignore )]
		public string PasswordHash { get; set; }

		[ElasticProperty( IncludeInAll = false, Index = FieldIndexOption.not_analyzed )]
		[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
		public string SecurityStamp { get; set; }

		[JsonProperty( DefaultValueHandling = DefaultValueHandling.Ignore )]
        public List<ElasticUserLoginInfo> Logins
		{
			get { return _logins; }
		}

		[JsonProperty( DefaultValueHandling = DefaultValueHandling.Ignore )]
		public ISet<ElasticClaim> Claims
		{
			get { return _claims; }
		}

		[JsonProperty( DefaultValueHandling = DefaultValueHandling.Ignore )]
		public ISet<string> Roles
		{
			get { return _roles; }
		}

		[JsonProperty( DefaultValueHandling = DefaultValueHandling.Ignore )]
		public ElasticUserEmail Email { get; set; }

		[JsonProperty( DefaultValueHandling = DefaultValueHandling.Ignore )]
		public ElasticUserPhone Phone { get; set; }


		/// <summary>
		/// Convenience property
		/// </summary>
		[ElasticProperty( OptOut = true )]
		[JsonIgnore]
		public string EmailAddress
		{
			get { return Email != null ? Email.Address : null; }
		}

		[ElasticProperty( IncludeInAll = false, Index = FieldIndexOption.not_analyzed )]
		[JsonProperty( DefaultValueHandling = DefaultValueHandling.Ignore )]
		[DefaultValue( false )]
		public bool TwoFactorAuthenticationEnabled { get; set; }

		public override string ToString()
		{
			return UserName;
		}
	}
}