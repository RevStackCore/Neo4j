using System;
using Neo4jClient.Cypher;
using RevStackCore.Pattern;

namespace RevStackCore.Neo4j
{
    public class Neo4jCypherRepository<TEntity, TKey> : Neo4jRepository<TEntity, TKey>, INeo4jCypherRepository<TEntity, TKey> where TEntity : class, IEntity<TKey>
    {
        public Neo4jCypherRepository(Neo4jDbContext context) : base(context) {}
        public ICypherFluentQuery Cypher
        {
            get
            {
                return _typedClient.Cypher;
            }
        }
    }
}
