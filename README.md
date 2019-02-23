# RevStackCore.Neo4j

[![Build status](https://ci.appveyor.com/api/projects/status/2c799v0lrk93sb6l?svg=true)](https://ci.appveyor.com/project/tachyon1337/neo4j)

A Neo4j/cypher implementation of the RevStackCore repository/graph pattern

# Nuget Installation

``` bash
Install-Package RevStackCore.Neo4j

```

# Repositories

```cs
 public interface INeo4jRepository<TEntity,TKey> : IGraphRepository<TEntity,TKey> where TEntity:class, IEntity<TKey>
 {
     ICypherFluentQuery Cypher { get; }
 }
 public class Neo4jRepository<TEntity, TKey> : INeo4jRepository<TEntity, TKey> where TEntity : class, IEntity<TKey>
```

# Implementations
Neo4jRepository<TEntity,Tkey> implements IRepository<TEntity,TKey> for basic Crud operations and Find
Neo4jRepository<TEntity,Tkey> implements IGraphRepository<TEntity,TKey> for Crud + Graph operations
Neo4jRepository<TEntity,Tkey> implements INeo4jRepository<TEntity,TKey> for Crud + Graph operations + Cypher


# Usage

## Dependency Injection

```cs
private static void ConfigureServices(IServiceCollection services)
{
    string uri = "http://localhost:7474/db/data";
    string user = "user";
    string password = "password";
    services.AddSingleton(p => new Neo4jDbContext(uri, user, password))
            .AddSingleton<IGraphRepository<User, string>, Neo4jRepository<User, string>>()
            .AddSingleton<INeo4jRepository<Post, string>, Neo4jRepository<Post, string>>();

}

```

## Example

```cs
public class DataService : IDataService
{
    private readonly IGraphRepository<User, string> _userRepository;
    private readonly INeo4jRepository<Post, string> _postRepository;
    public DataService(IGraphRepository<User, string> userRepository, INeo4jRepository<Post, string> postRepository)
    {
        _userRepository = userRepository;
        _postRepository = postRepository;
    }

    public Post AddPost(User user, Post post)
    {
        post = _postRepository.Add(post);
        //add relationship with datetime stamp property
        _userRepository.AddRelationShip<Post, DateStamp>(user.Id, post.Id, "AUTHORED", new DateStamp { Date = DateTime.Now });
        //add relationship with datetime stamp property
        _postRepository.AddRelationShip<User, DateStamp>(post.Id, user.Id, "AUTHORED_BY", new DateStamp { Date = DateTime.Now });
        return post;
    }

    public ProfileViewModel GetProfile(string userId)
    {
        //return profile data and the following/followers count
        return new ProfileViewModel
        {
            Profile=_userRepository.GetById(userId).ToProfile(),
            FollowingCount=_userRepository.GetRelatedCount<User>(userId, "FOLLOWING");
            FollowersCount=_userRepository.GetRelatedCount<User>(userId, "FOLLOWED_BY");
        }
    }

    public IEnumerable<PostModel> GetUserFriendsLatestPosts(string userId)
    {
        //traverse the graph to return the latest 50 posts(with comments) by a user's friends. Shape the result into a client view model
        //to be consumed in the view. Limit the returned post comments to 2 per post.

        //for this, we need to write out the match query using the Neo4jRepository Cypher property available on the postRepository that implements INeo4jRepository
        var cypher = _postRepository.Cypher;
        return cypher
            .Match("(v:User)-[:FOLLOWING]->(u:User)<-[:AUTHORED_BY]-(x:Post)-[:HAS_COMMENT]->(y:Comment)-[:COMMENT_POSTED_BY]->(z:User)")
            .With("v,x,u,{Comment:y,Author:z} as c")
            .Where((User v) => v.Id == userId)
            .Return((x, u, c) => new PostGraph //shape the traversed graph into a graph model we can use
            {
                Post = x.As<Post>(),
                Author = u.As<User>(),
                Comments = c.CollectAs<CommentAuthorLeaf>()
            })
            .OrderByDescending("x.Date")
            .Skip(0)
            .Limit(50)
            .Results.Select(r => PostMapper.Map(r, 2)); //shape the data into the client view model
    }
}

```

# AspNetCore Identity framework
Neo4jRepository can be plugged into the RevStackCore generic implementation of the AspNetCore Identity framework
https://github.com/RevStackCore/Identity


# Asynchronous Services
```cs
 public interface INeo4jService<TEntity,TKey> : IGraphService<TEntity,TKey> where TEntity:class, IEntity<TKey>
 {
     ICypherFluentQuery Cypher { get; }
 }
 public class Neo4jService<TEntity, TKey> : INeo4jService<TEntity, TKey> where TEntity : class, IEntity<TKey>
```

# Implementations
Neo4jService<TEntity,Tkey> implements IService<TEntity,TKey> for basic Async Crud operations and FindAsync
Neo4jService<TEntity,Tkey> implements IGraphService<TEntity,TKey> for Async Crud + Graph operations
Neo4jService<TEntity,Tkey> implements INeo4jService<TEntity,TKey> for Async Crud + Graph operations + Cypher

