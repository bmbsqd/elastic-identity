#region MIT License
/*
	The MIT License (MIT)

	Copyright (c) 2013 Bombsquad AB

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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Nest;

namespace Bmbsqd.ElasticIdentity
{
	public class ElasticUserStore<TUser> :
			IUserLoginStore<TUser>,
			IUserClaimStore<TUser>,
			IUserRoleStore<TUser>,
			IUserPasswordStore<TUser>,
			IUserSecurityStampStore<TUser>
			where TUser : ElasticUser
	{
		private const int _defaultSizeForAll = 1000*1000;

		private readonly IElasticClient _connection;
		public event EventHandler<ElasticUserStoreTraceEventArgs> Trace;

		private T Wrap<T>( T result, [CallerMemberName] string operation = "" ) where T : IResponse
		{
			var c = result.ConnectionStatus;
			OnTrace( operation, c.RequestUrl, c.Request, c.Result );
			return result;
		}

		protected virtual void OnTrace( string operation, string url, string request, string response )
		{
			var trace = Trace;
			if( trace != null ) {
				trace( this, new ElasticUserStoreTraceEventArgs( operation, url, request, response ) );
			}
		}

		private static IElasticClient CreateClient( Uri connectionString, string indexName, string entityName )
		{
			var settings = new ConnectionSettings( connectionString )
				.SetDefaultIndex( indexName )
				.MapDefaultTypeIndices( t => t.Add( typeof(TUser), indexName ) )
				.MapDefaultTypeNames( t => t.Add( typeof(TUser), entityName ) );
			return new ElasticClient( settings );
		}

		private static IndexSettings CreateIndexSettings( string entityName )
		{
			var settings = new IndexSettings();
			settings.Analysis.Analyzers.Add( "lowercaseKeyword", new CustomAnalyzer {
				Tokenizer = "keyword",
				Filter = new[] { "standard", "lowercase" }
			} );
			var keywordString = new StringMapping {Analyzer = "lowercaseKeyword", IncludeInAll = false};
			var simpleString = new StringMapping {Index = FieldIndexOption.not_analyzed, IncludeInAll = false};
			
			settings.Mappings.Add( new RootObjectMapping {
				Name = entityName,
				TypeNameMarker = entityName,
				IdFieldMapping = new IdFieldMapping().SetPath( "userName" ),
				IncludeInAll = false,
				Properties = new Dictionary<string, IElasticType> {
					{"userName", keywordString},
					{"passwordHash", simpleString},
					{"securityStamp", simpleString},
					{"roles", simpleString},
					{"claims", new ObjectMapping {
							Name = "claim",
							IncludeInAll = false,
							Properties = new Dictionary<string, IElasticType> {
								{"type", simpleString},
								{"value", simpleString}
							}
						}
					}, 
					{"logins", new ObjectMapping {
							Name = "login",
							IncludeInAll = false,
							Properties = new Dictionary<string, IElasticType> {
								{"loginProvider", simpleString},
								{"providerKey", simpleString}
							}
						}
					},
				}
			} );
			return settings;
		}

		private void SetupIndex( string indexName, string entityName, bool forceRecreate, Action<ElasticUserStore<TUser>> seed )
		{
			if( forceRecreate ) {
				Wrap( _connection.DeleteIndex( indexName ) );
			}

			if( !Wrap( _connection.IndexExists( indexName ) ).Exists ) {
				Wrap( _connection.CreateIndex( indexName, CreateIndexSettings( entityName ) ) );
				if( seed != null )
					seed( this );
			}
		}

		public ElasticUserStore( Uri connectionString, string indexName = "users", string entityName = "user", bool forceRecreate = false, Action<ElasticUserStore<TUser>> seed = null )
		{
			
			if( connectionString == null ) throw new ArgumentNullException( "connectionString" );
			if( indexName == null ) throw new ArgumentNullException( "indexName" );
			if( entityName == null ) throw new ArgumentNullException( "entityName" );

			_connection = CreateClient( connectionString, indexName, entityName );
			SetupIndex( indexName, entityName, forceRecreate, seed );
		}

		void IDisposable.Dispose()
		{
		}

		private async Task CreateOrUpdateAsync( TUser user, bool create )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			Wrap( await _connection.IndexAsync( user, new IndexParameters {
				Refresh = true,
				OpType = create ? OpType.Create : OpType.None
			} ) );
		}

		public Task CreateAsync( TUser user )
		{
			return CreateOrUpdateAsync( user, true );
		}

		public Task UpdateAsync( TUser user )
		{
			return CreateOrUpdateAsync( user, false );
		}

		public async Task DeleteAsync( TUser user )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			Wrap( await _connection.DeleteByIdAsync<TUser>( user.Id, new DeleteParameters {Refresh = true} ) );
		}

		public Task<TUser> FindByIdAsync( string userId )
		{
			return FindByNameAsync( userId );
		}

		public async Task<TUser> FindByNameAsync( string userName )
		{
			var result = Wrap( await _connection.SearchAsync<TUser>( search => search.Filter( filter => filter.Term( user => user.UserName, UserNameUtils.FormatUserName( userName ) ) ) ) );
			return result.Documents.FirstOrDefault();
		}

		public Task AddLoginAsync( TUser user, UserLoginInfo login )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			if( login == null ) throw new ArgumentNullException( "login" );
			user.Logins.Add( login );
			return Task.FromResult( true );
		}

		public Task RemoveLoginAsync( TUser user, UserLoginInfo login )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			if( login == null ) throw new ArgumentNullException( "login" );
			user.Logins.RemoveAll( x => x.LoginProvider == login.LoginProvider && x.ProviderKey == login.ProviderKey );
			return Task.FromResult( true );
		}

		public Task<IList<UserLoginInfo>> GetLoginsAsync( TUser user )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			return Task.FromResult( (IList<UserLoginInfo>)user.Logins );
		}

		public async Task<TUser> FindAsync( UserLoginInfo login )
		{
			if( login == null ) throw new ArgumentNullException( "login" );
			var result = Wrap( await _connection.SearchAsync<TUser>( search => search.
				Filter( filter => 
					filter.Term( user => user.Logins[0].ProviderKey, login.ProviderKey ) 
					&& filter.Term( user => user.Logins[0].LoginProvider, login.LoginProvider ) ) ) );
			return result.Documents.FirstOrDefault();
		}

		public Task<IList<Claim>> GetClaimsAsync( TUser user )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			var result = (IList<Claim>)user
				.Claims
				.Select( x => x.AsClaim() )
				.ToList();
			return Task.FromResult( result );
		}

		public Task AddClaimAsync( TUser user, Claim claim )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			if( claim == null ) throw new ArgumentNullException( "claim" );
			user.Claims.Add( claim );
			return Task.FromResult( true );
		}

		public Task RemoveClaimAsync( TUser user, Claim claim )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			if( claim == null ) throw new ArgumentNullException( "claim" );
			user.Claims.Remove( claim );
			return Task.FromResult( true );
		}

		public Task AddToRoleAsync( TUser user, string role )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			if( role == null ) throw new ArgumentNullException( "role" );
			user.Roles.Add( role );
			return Task.FromResult( true );
		}

		public Task RemoveFromRoleAsync( TUser user, string role )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			if( role == null ) throw new ArgumentNullException( "role" );
			user.Roles.Remove( role );
			return Task.FromResult( true );
		}

		public Task<IList<string>> GetRolesAsync( TUser user )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			var result = user.Roles.ToList();
			return Task.FromResult( (IList<string>)result );
		}

		public Task<bool> IsInRoleAsync( TUser user, string role )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			if( role == null ) throw new ArgumentNullException( "role" );
			return Task.FromResult( user.Roles.Contains( role ) );
		}

		public Task SetPasswordHashAsync( TUser user, string passwordHash )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			user.PasswordHash = passwordHash;
			return Task.FromResult( true );
		}

		public Task<string> GetPasswordHashAsync( TUser user )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			return Task.FromResult( user.PasswordHash );
		}

		public Task<bool> HasPasswordAsync( TUser user )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			return Task.FromResult( user.PasswordHash != null );
		}

		public Task SetSecurityStampAsync( TUser user, string stamp )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			user.SecurityStamp = stamp;
			return Task.FromResult( true );
		}

		public Task<string> GetSecurityStampAsync( TUser user )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			return Task.FromResult( user.SecurityStamp );
		}

		public async Task<IEnumerable<TUser>> GetAllAsync()
		{
			var result = Wrap( await _connection.SearchAsync<TUser>( search => search.MatchAll().Size( _defaultSizeForAll ) ) );
			return result.Documents;
		}
	}
}
