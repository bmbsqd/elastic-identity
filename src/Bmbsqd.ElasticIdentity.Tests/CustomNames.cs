#region MIT License

// /*
// 	The MIT License (MIT)
// 
// 	Copyright (c) 2013 Bombsquad Inc
// 
// 	Permission is hereby granted, free of charge, to any person obtaining a copy of
// 	this software and associated documentation files (the "Software"), to deal in
// 	the Software without restriction, including without limitation the rights to
// 	use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// 	the Software, and to permit persons to whom the Software is furnished to do so,
// 	subject to the following conditions:
// 
// 	The above copyright notice and this permission notice shall be included in all
// 	copies or substantial portions of the Software.
// 
// 	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// 	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// 	FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// 	COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// 	IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// 	CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// */

#endregion

using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Nest;
using NUnit.Framework;

namespace Bmbsqd.ElasticIdentity.Tests
{
	[TestFixture]
	public class CustomNames : TestBase
	{
		[Test]
		public void CustomIndexName()
		{
			const string indexName = "hello";
			Assert.False( Client.IndexExists( i => i.Index( indexName ) ).Exists );
			new ElasticUserStore<ElasticUser>(
				_connectionString,
				indexName: indexName,
				entityName: "world",
				forceRecreate: true );
			Assert.True( Client.IndexExists( i => i.Index( indexName ) ).Exists );
			Client.DeleteIndex( i => i.Index( indexName ) );
		}

		[Test]
		public async Task CustomTypeName()
		{
			const string indexName = "some-index";
			const string entityName = "world";
			try {
				var userStore = new ElasticUserStore<ElasticUser>(
					_connectionString,
					indexName: indexName,
					entityName: entityName,
					forceRecreate: true
					);

				var user = new ElasticUser {
					UserName = "elonmusk"
				};

				user.Roles.UnionWith( new[] {"hello"} );

				var userManager = new UserManager<ElasticUser>( userStore );
				AssertIdentityResult( await userManager.CreateAsync( user, "some password" ) );


				var response = Client.Get<ElasticUser>( user.Id, indexName, entityName );
				Assert.That( response.Source, Is.Not.Null );
				Assert.That( response.Source.UserName, Is.EqualTo( user.UserName ) );
			}
			finally {
				Client.DeleteIndex( i => i.Index( indexName ) );
			}
		}
	}
}