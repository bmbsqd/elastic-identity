using System;
using Microsoft.AspNet.Identity;
using Nest;
using NUnit.Framework;

namespace Bmbsqd.ElasticIdentity.Tests
{
	public abstract class TestBase
	{
		protected readonly Uri _connectionString = new Uri( "http://localhost:9200/" );
		protected readonly string _defaultIndex = "users_tests";

		private IElasticClient _client;

		protected IElasticClient Client
		{
			get { return _client ?? (_client = new ElasticClient( new ConnectionSettings( _connectionString, _defaultIndex ) )); }
		}


		protected IdentityResult AssertIdentityResult( IdentityResult identityResult )
		{
			Assert.True( identityResult.Succeeded, "Errors in identity result: {0}", String.Join( ", ", identityResult.Errors ) );
			return identityResult;
		}
	}
}