Elastic Identity - The ASP.NET Identity Provider for ElasticSearch
==================================================================

Why use elastic-identity
========================

Elastic-Identity wires up the storage and repository of ElasticSearch with ASP.NET Identity


Reversion History
==========

- 1.0.0-beta
  - Added support for additional services: 
     - IUserTwoFactorStore
     - IUserEmailStore
     - IUserPhoneNumberStore
  - Upgrade to ASP.NET Identity 2.x
  - Upgrade to support Nest 1.x
  - Breaking change in constructor, no more seed parameter, users should override SeedAsync() instead

How to use
==========

Install
-------
Get it from [nuget.org](https://www.nuget.org/packages/Bmbsqd.ElasticIdentity)

Simple
------

```csharp
new ElasticUserStore<ElasticUser>( new Uri( "http://localhost:9200/" ) );
```

With AutoFac
------------

```csharp
builder.Register( c => new ElasticUserStore<ElasticUser>( Settings.Default.UserServer ) )
	.AsSelf()
	.AsImplementedInterfaces()
	.SingleInstance();
```

You should probably consume IUserStore<ElasticUser> in your code


Let's seed the user store if the index is created
-------------------------------------------------

```csharp

public class MyElasticUserStore<TUser> : ElasticUserStore<TUser>
{
  public MyElasticUserStore( Uri connectionString ) : base( connectionString )
  {
  }
  
  static readonly string[] _roles =
  {
    "Admin", 
    "User", 
    ...
  };

  const string _seedUser = "elonmusk";
  const string _seedPassword = "tesla";

  protected override async Task SeedAsync()
  {
    var user = new ElasticUser {
      UserName = _seedUser
    };
    user.Roles.UnionWith( _roles );

    var userManager = new UserManager<ElasticUser>( this );
    await userManager.CreateAsync( user, _seedPassword );
  }
}


builder.Register( c => new MyElasticUserStore<ElasticUser>( Settings.Default.UserServer ) )
	.AsSelf()
	.AsImplementedInterfaces()
	.SingleInstance();
```


Extend the user
---------------

```csharp
public enum Transportation {
	Roadster,
	ModelS,
	Other
}


public class MyUser : ElasticUser
{
	public string Twitter { get; set; }
	public Transportation Car { get; set; }
}


new ElasticUserStore<MyUser>( new Uri( "http://localhost:9200/" ) );
```

More samples and documentation
------------------------------

The code is pretty tiny, so go ahead and explore the source code to find out what's possible.
Also, check out the options of the ElasticUserStore constructor

```csharp
ElasticUserStore( 
	Uri connectionString,							// where's your elasticsearch. Something like http://localhost:9200/ or http://users.tesla-co.internal/
	string indexName = "users",						// what index we're storing the users under. Defaults to "users"
	string entityName = "user",						// type name for each user. Defaults to "user"
	bool forceRecreate = false						// if index exists, drop it before creating it again.
	 )

protected override async Task SeedAsync()
{
  // Put your seeding logic here, stuff that's 
  // executed when the index is created 
}

```

Contributing
------------

Yes please

Copyright and license
---------------------

elastic-identity is licenced under the MIT license. Refer to LICENSE for more information.

