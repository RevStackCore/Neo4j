using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using RevStackCore.Pattern;
using RevStackCore.Pattern.Graph;

namespace RevStackCore.Neo4j
{
    public class Neo4jRepository<TEntity, TKey> : IGraphRepository<TEntity, TKey> where TEntity : class, IEntity<TKey>
    {
        protected readonly TypedClient<TEntity, TKey> _typedClient;
        public Neo4jRepository(Neo4jDbContext context)
        {
            _typedClient = new TypedClient<TEntity, TKey>(context);
        }
        public TEntity Add(TEntity entity)
        {
            return _typedClient.Insert(entity);
        }

        public bool AddLabel(TKey id, string label)
        {
            return _typedClient.AddLabel(id, label);
        }

        public bool CreateConstraint()
        {
            return _typedClient.CreateConstraint();
        }

        public bool CreateIndex()
        {
            return _typedClient.CreateIndex();
        }

        public bool CreateIndex(string property)
        {
            return _typedClient.CreateIndex(property);
        }

        public void Delete(TEntity entity)
        {
            _typedClient.Delete(entity);
        }

        public bool DeleteLabel(TKey id, string label)
        {
            return _typedClient.RemoveLabel(id, label);
        }

        public IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate)
        {
            return _typedClient.Find(predicate).AsQueryable();
        }

        public IQueryable<TEntity> Find(string expression)
        {
            return _typedClient.Find(expression).AsQueryable();
        }

        public IQueryable<TEntity> Find(string label, Expression<Func<TEntity, bool>> predicate)
        {
            return _typedClient.Find(label, predicate).AsQueryable();
        }

        public IQueryable<TEntity> Find(string label, string expression)
        {
            return _typedClient.Find(label, expression).AsQueryable();
        }

        public IEnumerable<TEntity> Get()
        {
            return _typedClient.GetAll();
        }

        public IEnumerable<TEntity> Get(int limit, int skip)
        {
            return _typedClient.GetAll(limit, skip);
        }

        public TEntity GetById(TKey id)
        {
            return _typedClient.GetById(id);
        }

        public IEnumerable<TEntity> GetByLabel(string label)
        {
            return _typedClient.GetAllByLabel(label);
        }

        public IEnumerable<string> GetLabels(TKey id)
        {
            return _typedClient.GetLabels(id);
        }

        public TEntity Update(TEntity entity)
        {
            return _typedClient.Update(entity);
        }

        public bool AddRelationShip<TOut>(TKey inboundId, TKey outboundId, string relationship) where TOut : class, IEntity<TKey>
        {
            return _typedClient.AddRelationship<TOut>(inboundId, outboundId, relationship);
        }

        public bool HasRelationship<TOut>(TKey inboundId, TKey outboundId, string relationship) where TOut : class, IEntity<TKey>
        {
            return _typedClient.HasRelationship<TOut>(inboundId, outboundId, relationship);
        }

        public bool DeleteRelationShip<TOut>(TKey inboundId, TKey outboundId, string relationship) where TOut : class, IEntity<TKey>
        {
            return _typedClient.DeleteRelationship<TOut>(inboundId, outboundId, relationship);
        }

        public IEnumerable<TOut> GetRelated<TOut>(TKey id, string relationship) where TOut : class, IEntity<TKey>
        {
            return _typedClient.GetRelatedNodes<TOut>(id, relationship);
        }

        public int GetRelatedCount<TOut>(TKey id, string relationship) where TOut : class, IEntity<TKey>
        {
            return _typedClient.GetRelatedNodesCount<TOut>(id, relationship);
        }

        public IEnumerable<TOut> GetRelated<TOut, TRelation>(TKey id, string relationship, Expression<Func<TRelation, bool>> predicate) where TOut : class, IEntity<TKey> where TRelation:class
        {
            return _typedClient.GetRelatedNodes<TOut,TRelation>(id, relationship, predicate);
        }

        public int GetRelatedCount<TOut, TRelation>(TKey id, string relationship, Expression<Func<TRelation, bool>> predicate) where TOut : class, IEntity<TKey> where TRelation : class
        {
            return _typedClient.GetRelatedNodesCount<TOut, TRelation>(id, relationship, predicate);
        }

        public bool AddRelationShip<TOut, TRelation>(TKey inboundId, TKey outboundId, string relationship, TRelation relation) where TOut : class, IEntity<TKey> where TRelation : class
        {
            return _typedClient.AddRelationship<TOut, TRelation>(inboundId, outboundId, relationship, relation);
        }

        public IEnumerable<TEntity> Get(string label)
        {
            return _typedClient.GetAllByLabel(label);
        }

        public IEnumerable<TEntity> Get(string label, int limit, int skip)
        {
            return _typedClient.GetAllByLabel(label, limit, skip);
        }

        public IQueryable<TEntity> Find(string label, Expression<Func<TEntity, bool>> predicate, int limit, int skip)
        {
            return _typedClient.Find(label, predicate, limit, skip).AsQueryable();
        }

        public IQueryable<TEntity> Find(string label, string expression, int limit, int skip)
        {
            return _typedClient.Find(label, expression, limit, skip).AsQueryable();
        }

        public IQueryable<TEntity> Find(string expression, int limit, int skip)
        {
            return _typedClient.Find(expression, limit, skip).AsQueryable();
        }
    }
}
