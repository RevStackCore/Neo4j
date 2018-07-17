# RevStackCore.Neo4j

A Neo4j/cypher implementation of the RevStackCore repository/graph pattern

# Nuget Installation

``` bash
Install-Package RevStackCore.Neo4j

```

# Repositories

```cs
 public class Neo4jRepository<TEntity, TKey> : IGraphRepository<TEntity, TKey> where TEntity : class, IEntity<TKey>
 public interface INeo4jCypherRepository<TEntity,TKey> : IGraphRepository<TEntity,TKey> where TEntity:class, IEntity<TKey>
 {
     ICypherFluentQuery Cypher { get; }
 }
 public class Neo4jCypherRepository<TEntity, TKey> : Neo4jRepository<TEntity, TKey>, INeo4jCypherRepository<TEntity, TKey> where TEntity : class, IEntity<TKey>
```

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
            .AddSingleton<IGraphRepository<Post, string>, Neo4jRepository<Post, string>>()
            .AddSingleton<INeo4jCypherRepository<Post, string>, Neo4jCypherRepository<Post, string>>();

}

```

## Example

```cs
public class DataService : IDataService
{
    private readonly IGraphRepository<User, string> _userRepository;
    private readonly IGraphRepository<Post, string> _postRepository;
    private readonly INeo4jCypherRepository<Post, string> _cypherRepository;
    public DataService(IGraphRepository<User, string> userRepository, IGraphRepository<Post, string> postRepository, INeo4jCypherRepository<Post, string> cypherRepository)
    {
        _userRepository = userRepository;
        _postRepository = postRepository;
        _cypherRepository = cypherRepository;
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

        //for this, we need to write out the cypher for the match query using cypher and use Neo4jCypherRepository that extends Neo4jRepository to return a Cypher property
        var cypher = _cypherRepository.Cypher;
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

