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
using NUnit.Framework;

namespace Bmbsqd.ElasticIdentity.Tests
{
	[TestFixture]
	public class Methods : TestBase
	{
		private const string UserName = "icanhasjonas";

		private ElasticUserStore<ElasticUser> CreateStore()
		{
			return new ElasticUserStore<ElasticUser>( _connectionString, indexName: _defaultIndex, forceRecreate: true );
		}

		[TearDown]
		public async void Done()
		{
			await Client.DeleteIndexAsync( i => i.Index( _defaultIndex ) );
		}

		[Test]
		public async Task CreateUser()
		{
			var store = CreateStore();
			var user = new ElasticUser( UserName ) {
				Phone = new ElasticUserPhone {
					Number = "555 123 1234",
					IsConfirmed = true
				},
				Email = new ElasticUserEmail {
					Address = "hello@world.com",
					IsConfirmed = false
				}
			};

			await store.CreateAsync( user );

			user = await store.FindByNameAsync( UserName );
			Assert.That( user, Is.Not.Null );
			Assert.That( user.UserName, Is.EqualTo( UserName ) );
		}

		[Test]
		public async Task FindById()
		{
			var store = CreateStore();
			var user = new ElasticUser( UserName );

			await store.CreateAsync( user );

			var elasticUser = await store.FindByIdAsync( user.Id );

			Assert.IsNotNull( elasticUser );
			Assert.AreEqual( user.Id, elasticUser.Id );
		}

		[Test]
		public async Task MissingUserShouldBeNull()
		{
			var store = CreateStore();

			// should not throw when 404 is returned, it should return null instead to indicate resource not found
			var user404 = await store.FindByIdAsync( "missing" );

			Assert.IsNull( user404 );
		}

		[Test]
		public async Task FindByEmail()
		{
			var store = CreateStore();
			var user = new ElasticUser( UserName ) {
				Email = new ElasticUserEmail {
					Address = "hello@world.com",
					IsConfirmed = false
				}
			};

			await store.CreateAsync( user );

			var elasticUser = await store.FindByEmailAsync( user.Email.Address );

			Assert.IsNotNull( elasticUser );
			Assert.AreEqual( user.EmailAddress, elasticUser.EmailAddress );
		}

		[Test]
		public async Task DeleteUser()
		{
			var store = CreateStore();
			var user = new ElasticUser( UserName );

			await store.CreateAsync( user );

			await store.DeleteAsync( user );

			user = await store.FindByNameAsync( user.UserName );
			Assert.That( user, Is.Null );
		}

		[Test]
		public async Task UpdateUser()
		{
			var store = CreateStore();
			var user = new ElasticUser( UserName );

			await store.CreateAsync( user );

			user = await store.FindByIdAsync( user.Id );
			user.Roles.Add( "hello" );

			await store.UpdateAsync( user );
			user = await store.FindByIdAsync( user.Id );

			Assert.That( user.Roles, Contains.Item( "hello" ) );
		}
	}
}