using Neo4jClient.Cypher;
using RevStackCore.Pattern;
using RevStackCore.Pattern.Graph;

namespace RevStackCore.Neo4j
{
    public interface INeo4jRepository<TEntity,TKey> : IGraphRepository<TEntity,TKey> where TEntity:class, IEntity<TKey>
    {
        ICypherFluentQuery Cypher { get; }
    }
}
