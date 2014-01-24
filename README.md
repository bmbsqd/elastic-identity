Elastic Identity - The ASP.NET Identity Provider for ElasticSearch
==================================================================

Piglet is a library for lexing and parsing text, in the spirit of those big parser and lexer genererators such as bison, antlr and flex. While not as feature packed as those, it is also a whole lot leaner and much easier to understand.

Why use elastic-identity
========================

Elastic-Identity wires up the storage and repository of ElasticSearch with ASP.NET Identity

How to use
==========



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
static readonly string[] _roles =
{
	"Admin", 
	"User", 
	...
};

const string _seedUser = "elon-musk";
const string _seedPassword = "tesla";

static void SeedUserStore( ElasticUserStore<ElasticUser> store )
{
	var user = new ElasticUser {
		UserName = _seedUser
	};

	user.Roles.UnionWith( _roles );

	var userManager = new UserManager<ElasticUser>( store );
	userManager.Create( user, _seedPassword );
}

builder.Register( c => new ElasticUserStore<ElasticUser>( Settings.Default.UserServer, seed: SeedUserStore ) )
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
	bool forceRecreate = false,						// if index exists, drop it before creating it again.
	Action<ElasticUserStore<TUser>> seed = null		// if the index was created, run this
	 )
```

Contributing
------------

Yes please

Copyright and license
---------------------

elastic-identity is licenced under the MIT license. Refer to LICENSE for more information.
