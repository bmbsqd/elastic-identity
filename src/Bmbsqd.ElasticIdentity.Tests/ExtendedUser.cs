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
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Bmbsqd.ElasticIdentity.Tests
{
	[TestFixture]
	public class ExtendedUser : TestBase
	{

		public abstract class CarBase
		{
			public string LicensePlate { get; set; }
		}

		public abstract class CarBase<TModel> : CarBase where TModel : struct, IConvertible
		{
			public TModel Model { get; set; }
		}

		public enum TeslaModel
		{
			Roadster,
			ModelS,
			ModelX
		}

		public enum KoenigseggModel
		{
			CCR,
			CCX,
			CCRX,
			Agera,
			AgeraR,
			AgeraS,
			One
		}

		public class Tesla : CarBase<TeslaModel>
		{
		}

		public class Koenigsegg : CarBase<KoenigseggModel>
		{
		}


		public class HelloUser : ElasticUser
		{
			[JsonProperty( TypeNameHandling = TypeNameHandling.Objects )]
			public CarBase Car { get; set; }
		}



		[Test]
		public async Task TestExtendedProperties()
		{
			const string indexName = "hello-users";
			try {
				var store = new ElasticUserStore<HelloUser>( _connectionString,
					indexName,
					forceRecreate: true );

				await store.CreateAsync( new HelloUser {
					UserName = "abc123",
					Car = new Tesla {
						LicensePlate = "ABC123",
						Model = TeslaModel.ModelS
					}
				} );

				await store.CreateAsync( new HelloUser {
					UserName = "def456",
					Car = new Koenigsegg {
						LicensePlate = "ABC123",
						Model = KoenigseggModel.One
					}
				} );

				var users = await store.GetAllAsync();

				var teslaUser = users.FirstOrDefault( x => x.UserName == "abc123" );
				var koenigseggUser = users.FirstOrDefault( x => x.UserName == "def456" );

				Assert.That( teslaUser, Is.Not.Null, "No Telsa user found" );
				Assert.That( koenigseggUser, Is.Not.Null, "No Koenigsegg user found" );

				Assert.That( teslaUser.Car, Is.AssignableTo<Tesla>(), "Tesla Car is not Tesla" );
				Assert.That( koenigseggUser.Car, Is.AssignableTo<Koenigsegg>(), "Koenigsegg Car is not Koenigsegg" );

			}
			finally {
				Client.DeleteIndex( i => i.Index( indexName ) );
			}
		}
	}
}