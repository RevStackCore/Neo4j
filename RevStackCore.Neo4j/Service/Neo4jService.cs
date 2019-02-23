using System;
using Neo4jClient.Cypher;
using RevStackCore.Pattern;
using RevStackCore.Pattern.Graph;

namespace RevStackCore.Neo4j
{
    public class Neo4jService<TEntity,TKey> : GraphService<TEntity,TKey>, INeo4jService<TEntity,TKey> where TEntity : class, IEntity<TKey>
    {
        private INeo4jRepository<TEntity, TKey> _cypherRepository;
        public Neo4jService(INeo4jRepository<TEntity, TKey> repository) : base(repository) 
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
