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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Microsoft.AspNet.Identity;
using Nest;

namespace Bmbsqd.ElasticIdentity
{
	public class ElasticUserStore<TUser> :
		ElasticUserStore,
		IUserStore<TUser>,
		IUserLoginStore<TUser>,
		IUserClaimStore<TUser>,
		IUserRoleStore<TUser>,
		IUserPasswordStore<TUser>,
		IUserSecurityStampStore<TUser>,
		IUserTwoFactorStore<TUser, string>,
		IUserEmailStore<TUser, string>,
		IUserPhoneNumberStore<TUser, string>
		//IUserLockoutStore<TUser,string>
		where TUser : ElasticUser
	{
		private readonly Lazy<Task<IElasticClient>> _connection;

		private static IElasticClient CreateClient( Uri connectionString, string indexName, string entityName )
		{
			var settings = new ConnectionSettings( connectionString )
				.SetDefaultIndex( indexName )
				.MapDefaultTypeIndices( x => x.Add( typeof( TUser ), indexName ) )
				.MapDefaultTypeNames( x => x.Add( typeof( TUser ), entityName ) )
				.DisablePing()
				.SetJsonSerializerSettingsModifier( s => s.Converters.Add( new ElasticEnumConverter() ) );
			return new ElasticClient( settings );
		}

		private async Task SetupIndexAsync( IElasticClient connection, string indexName, string entityName, bool forceRecreate )
		{

			//var exists = Wrap( await connection.IndexExistsAsync( x => x.Index( indexName ) ).ConfigureAwait( false ) ).Exists; // TODO: Async Version fails in NEST 1.0.0-beta1
			var exists = Wrap( connection.IndexExists( x => x.Index( indexName ) ) ).Exists;

			if( exists && forceRecreate ) {
				Wrap( await connection.DeleteIndexAsync( x => x.Index( indexName ) ).ConfigureAwait( false ) );
				exists = false;
			}

			if( !exists ) {
				var createResponse = Wrap( await connection.CreateIndexAsync( indexName,
					createIndexDescriptor => createIndexDescriptor
						.Analysis( a => a
							.Analyzers( x => x.Add( "lowercaseKeyword", new CustomAnalyzer {
								Tokenizer = "keyword",
								Filter = new[] { "standard", "lowercase" }
							} ) )
						)
						.AddMapping<TUser>( m => m
							.MapFromAttributes()
							.IncludeInAll( false )
							.Type( entityName )
						)
					).ConfigureAwait( false ) );
				AssertIndexCreateSuccess( createResponse );
				await SeedAsync().ConfigureAwait( false );
			}
		}

		private static void AssertIndexCreateSuccess( IIndicesOperationResponse createResponse )
		{
			var status = createResponse.ConnectionStatus;
			if( !status.Success ) {
				if( status.OriginalException != null ) {
					throw status.OriginalException;
				}
				throw new ApplicationException( "Error while creating index, " + Encoding.UTF8.GetString( status.ResponseRaw ) );
			}
		}

		public ElasticUserStore( Uri connectionString, string indexName = "users", string entityName = "user", bool forceRecreate = false )
		{
			if( connectionString == null ) throw new ArgumentNullException( "connectionString" );
			if( indexName == null ) throw new ArgumentNullException( "indexName" );
			if( entityName == null ) throw new ArgumentNullException( "entityName" );
			if( !_nameValidationRegex.IsMatch( indexName ) ) {
				throw new ArgumentException( "Invalid Characters in indexName, must be all lowercase", "indexName" );
			}
			if( !_nameValidationRegex.IsMatch( entityName ) ) {
				throw new ArgumentException( "Invalid Characters in entityName, must be all lowercase", "entityName" );
			}


			_connection = new Lazy<Task<IElasticClient>>( async () => {
				var connection = CreateClient( connectionString, indexName, entityName );
				await SetupIndexAsync( connection, indexName, entityName, forceRecreate ).ConfigureAwait( false );
				return connection;
			} );
		}

		void IDisposable.Dispose()
		{
		}

		protected virtual Task SeedAsync()
		{
			return DoneTask;
		}

		protected ConfiguredTaskAwaitable<IElasticClient> Connection
		{
			get { return _connection.Value.ConfigureAwait( false ); }
		}

		private async Task CreateOrUpdateAsync( TUser user, bool create )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			var connection = await Connection;
			Wrap( await connection.IndexAsync( user, x => x.Refresh().OpType( create ? OpTypeOptions.Create : OpTypeOptions.Index ) ) );
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
			var connection = await Connection;
			Wrap( await connection.DeleteAsync<TUser>( d => d
				.Id( user.Id )
				.Refresh() ) );
		}

		public Task<TUser> FindByIdAsync( string userId )
		{
			if( userId == null ) throw new ArgumentNullException( "userId" );
			return FindByNameAsync( userId );
		}

		public async Task<TUser> FindByNameAsync( string userName )
		{
			if( userName == null ) throw new ArgumentNullException( "userName" );
			var connection = await Connection;
			var result = Wrap( await connection.SearchAsync<TUser>( search => search.Filter( filter => filter.Term( user => user.UserName, UserNameUtils.FormatUserName( userName ) ) ) ) );
			return result.Documents.FirstOrDefault();

			// ShouldBe: but Nest throws on 404
			//var result = Wrap( await _connection.GetAsync<TUser>( x => x.Id( UserNameUtils.FormatUserName( userName ) ) ) );
			//return result.Source;

		}

		public async Task<TUser> FindByEmailAsync( string email )
		{
			if( email == null ) throw new ArgumentNullException( "email" );
			var connection = await Connection;
			var result = Wrap( await connection.SearchAsync<TUser>(
				search => search
					.Filter( filter => filter
						.Term( user => user.Email.Address, email ) )
				) );
			return result.Documents.FirstOrDefault();
		}

		public Task AddLoginAsync( TUser user, UserLoginInfo login )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			if( login == null ) throw new ArgumentNullException( "login" );
			user.Logins.Add( login );
			return DoneTask;
		}

		public Task RemoveLoginAsync( TUser user, UserLoginInfo login )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			if( login == null ) throw new ArgumentNullException( "login" );
			user.Logins.RemoveAll( x => x.LoginProvider == login.LoginProvider && x.ProviderKey == login.ProviderKey );
			return DoneTask;
		}

		public Task<IList<UserLoginInfo>> GetLoginsAsync( TUser user )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			return Task.FromResult( (IList<UserLoginInfo>)user.Logins );
		}

		public async Task<TUser> FindAsync( UserLoginInfo login )
		{
			if( login == null ) throw new ArgumentNullException( "login" );
			var connection = await Connection;
			var result = Wrap( await connection.SearchAsync<TUser>(
				search => search
					.Filter( filter => filter
						.Bool( b => b
							.Must(
								m => m.Term( user => user.Logins[0].ProviderKey, login.ProviderKey ),
								m => m.Term( user => user.Logins[0].LoginProvider, login.LoginProvider )
							) )
					) )
				);
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
			return DoneTask;
		}

		public Task RemoveClaimAsync( TUser user, Claim claim )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			if( claim == null ) throw new ArgumentNullException( "claim" );
			user.Claims.Remove( claim );
			return DoneTask;
		}

		public Task AddToRoleAsync( TUser user, string role )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			if( role == null ) throw new ArgumentNullException( "role" );
			user.Roles.Add( role );
			return DoneTask;
		}

		public Task RemoveFromRoleAsync( TUser user, string role )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			if( role == null ) throw new ArgumentNullException( "role" );
			user.Roles.Remove( role );
			return DoneTask;
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
			return DoneTask;
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
			return DoneTask;
		}

		public Task<string> GetSecurityStampAsync( TUser user )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			return Task.FromResult( user.SecurityStamp );
		}

		public async Task<IEnumerable<TUser>> GetAllAsync()
		{
			var connection = await Connection;
			var result = Wrap( await connection.SearchAsync<TUser>( search => search.MatchAll().Size( DefaultSizeForAll ) ) );
			return result.Documents;
		}

		public Task SetTwoFactorEnabledAsync( TUser user, bool enabled )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			user.TwoFactorAuthenticationEnabled = enabled;
			return DoneTask;
		}

		public Task<bool> GetTwoFactorEnabledAsync( TUser user )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			return Task.FromResult( user.TwoFactorAuthenticationEnabled );
		}

		public Task SetEmailAsync( TUser user, string email )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			user.Email = email == null
				? null
				: new ElasticUserEmail { Address = email };
			return DoneTask;
		}

		public Task<string> GetEmailAsync( TUser user )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			var elasticUserEmail = user.Email;

			return elasticUserEmail != null
				? Task.FromResult( elasticUserEmail.Address )
				: Task.FromResult<string>( null );
		}

		public Task<bool> GetEmailConfirmedAsync( TUser user )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			var elasticUserEmail = user.Email;

			return elasticUserEmail != null
				? Task.FromResult( elasticUserEmail.IsConfirmed )
				: Task.FromResult( false );
		}

		public Task SetEmailConfirmedAsync( TUser user, bool confirmed )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			var elasticUserEmail = user.Email;
			if( elasticUserEmail != null )
				elasticUserEmail.IsConfirmed = true;
			else throw new InvalidOperationException( "User have no configured email address" );
			return DoneTask;
		}

		public Task SetPhoneNumberAsync( TUser user, string phoneNumber )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			user.Phone = phoneNumber == null
				? null
				: new ElasticUserPhone { Number = phoneNumber };
			return DoneTask;
		}

		public Task<string> GetPhoneNumberAsync( TUser user )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			var elasticUserPhone = user.Phone;

			return elasticUserPhone != null
				? Task.FromResult( elasticUserPhone.Number )
				: Task.FromResult<string>( null );
		}

		public Task<bool> GetPhoneNumberConfirmedAsync( TUser user )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			var elasticUserPhone = user.Phone;

			return elasticUserPhone != null
				? Task.FromResult( elasticUserPhone.IsConfirmed )
				: Task.FromResult( false );
		}

		public Task SetPhoneNumberConfirmedAsync( TUser user, bool confirmed )
		{
			if( user == null ) throw new ArgumentNullException( "user" );
			var elasticUserPhone = user.Phone;
			if( elasticUserPhone != null )
				elasticUserPhone.IsConfirmed = true;
			else throw new InvalidOperationException( "User have no configured phone number" );
			return DoneTask;
		}
	}

	public abstract class ElasticUserStore
	{
		protected static readonly Task DoneTask = Task.FromResult( true );
		protected const int DefaultSizeForAll = 1000 * 1000;
		protected static readonly Regex _nameValidationRegex = new Regex( "^[a-z0-9-_]+$", RegexOptions.Singleline | RegexOptions.CultureInvariant );
		public event EventHandler<ElasticUserStoreTraceEventArgs> Trace;

		protected virtual void OnTrace( string operation, IElasticsearchResponse response )
		{
			var trace = Trace;
			if( trace != null ) {
				trace( this, new ElasticUserStoreTraceEventArgs( operation,
					response.RequestUrl,
					Encoding.UTF8.GetString( response.Request ),
					Encoding.UTF8.GetString( response.ResponseRaw ) ) );
			}
		}

		protected T Wrap<T>( T result, [CallerMemberName] string operation = "" ) where T : IResponse
		{
			var c = result.ConnectionStatus;
			OnTrace( operation, c );
			return result;
		}
	}
}