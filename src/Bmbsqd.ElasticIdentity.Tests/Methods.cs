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

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using NUnit.Framework;

namespace Bmbsqd.ElasticIdentity.Tests
{
	[TestFixture]
	public class Methods : TestBase
	{
		private const string UserName = "icanhasjonas";

		private ElasticUserStore<ElasticUser> CreateStore()
		{
			return new ElasticUserStore<ElasticUser>( _connectionString, forceRecreate: true );
		}

		private async Task CreateUser( IUserStore<ElasticUser> store, ElasticUser user )
		{
			var manager = new UserManager<ElasticUser>( store );
			AssertIdentityResult(
				await manager.CreateAsync( user, "some-password" ) );
		}

		[TearDown]
		public void Done()
		{
			Client.DeleteIndex( i => i.Index( "users" ) );
			Thread.Sleep( 200 ); // ES seems to not like it very much when delete/create comes to close to each other.. 
		}

		[Test]
		public async Task CreateUser()
		{
			var store = CreateStore();
			await CreateUser( store, new ElasticUser( UserName ) {
				Phone = new ElasticUserPhone {
					Number = "555 123 1234",
					IsConfirmed = true
				},
				Email = new ElasticUserEmail {
					Address = "hello@world.com",
					IsConfirmed = false
				}
			} );

			var user = await store.FindByNameAsync( UserName );
			Assert.That( user, Is.Not.Null );
			Assert.That( user.UserName, Is.EqualTo( UserName ) );
		}

		[Test]
		public async Task DeleteUser()
		{
			var store = CreateStore();
			var elasticUser = new ElasticUser( UserName );
			await CreateUser( store, elasticUser );

			await store.DeleteAsync( elasticUser );

			var user = await store.FindByNameAsync( elasticUser.UserName );
			Assert.That( user, Is.Null );
		}

		[Test]
		public async Task UpdateUser()
		{
			var store = CreateStore();
			var elasticUser = new ElasticUser( UserName );
			await CreateUser( store, elasticUser );

			var user = await store.FindByIdAsync( elasticUser.UserName );
			user.Roles.Add( "hello" );

			await store.UpdateAsync( user );
			user = await store.FindByIdAsync( elasticUser.Id );

			Assert.That( user.Roles, Contains.Item( "hello" ) );
			
		}
	}
}