using System.Collections.Generic;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;

namespace Bmbsqd.ElasticIdentity
{
	public class ElasticUser : IUser
	{
		private readonly List<UserLoginInfo> _logins;
		private readonly HashSet<ElasticClaim> _claims;
		private readonly HashSet<string> _roles;
		private string _userName;

		public ElasticUser()
		{
			_logins = new List<UserLoginInfo>();
			_claims = new HashSet<ElasticClaim>();
			_roles = new HashSet<string>();
		}

		public ElasticUser( string userName ) : this()
		{
			UserName = userName;
		}

		[JsonIgnore]
		public string Id
		{
			get { return UserName; }
		}

		public string UserName
		{
			get { return _userName; }
			set { _userName = UserNameUtils.FormatUserName( value ); }
		}

		public string PasswordHash { get; set; }
		public string SecurityStamp { get; set; }

		public List<UserLoginInfo> Logins
		{
			get { return _logins; }
		}

		public ISet<ElasticClaim> Claims
		{
			get { return _claims; }
		}

		public ISet<string> Roles
		{
			get { return _roles; }
		}

		public override string ToString()
		{
			return UserName;
		}
	}
}