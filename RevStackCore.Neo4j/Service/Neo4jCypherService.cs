using System;
using Neo4jClient.Cypher;
using RevStackCore.Pattern;
using RevStackCore.Pattern.Graph;

namespace RevStackCore.Neo4j
{
    public class Neo4jCypherService<TEntity,TKey> : GraphService<TEntity,TKey>, INeo4jCypherService<TEntity,TKey> where TEntity : class, IEntity<TKey>
    {
        private INeo4jCypherRepository<TEntity, TKey> _cypherRepository;
        public Neo4jCypherService(INeo4jCypherRepository<TEntity, TKey> repository) : base(repository) 
        {
            _cypherRepository = repository;
        }
        public ICypherFluentQuery Cypher
        {
            get
            {
                return _cypherRepository.Cypher;
            }
        }
    }
}
