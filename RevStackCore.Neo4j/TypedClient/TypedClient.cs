using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Neo4jClient;
using Neo4jClient.Cypher;
using RevStackCore.Pattern;
using RevStackCore.Extensions;


namespace RevStackCore.Neo4j
{
	public class TypedClient<TEntity, TKey> where TEntity : class, IEntity<TKey>
    {
        private readonly GraphClient _client;
        private readonly string _type;
        private Type _tKeyType;

        public TypedClient(Neo4jDbContext context)
        {
            _client = context.GraphClient;
            _type = typeof(TEntity).Name;
            _tKeyType = typeof(TKey);
        }

        /// <summary>
        /// Returns the cypher fluent query instance
        /// </summary>
        /// <value>The cypher.</value>
        public ICypherFluentQuery Cypher
        {
            get
            {
                return _client.Cypher;
            }
        }

        /// <summary>
        /// Gets all TEntity nodes
        /// </summary>
        /// <returns>The all.</returns>
        public IEnumerable<TEntity> GetAll()
        {
            string nodeMatch = "(x:" + _type + ")";
            return _client.Cypher
                          .Match(nodeMatch)
                          .Return(x => x.As<TEntity>())
                          .Results;
        }

        /// <summary>
        /// Gets paged TEntity nodes.
        /// </summary>
        /// <returns>The paged.</returns>
        /// <param name="limit">Limit.</param>
        /// <param name="skip">Skip.</param>
        public IEnumerable<TEntity> GetAll(int limit, int skip)
        {
            string nodeMatch = "(x:" + _type + ")";
            return _client.Cypher
                          .Match(nodeMatch)
                          .Return(x => x.As<TEntity>())
                          .Skip(skip)
                          .Limit(limit)
                          .Results;
        }

        /// <summary>
        /// Gets the node by identifier.
        /// </summary>
        /// <returns>The by identifier.</returns>
        /// <param name="id">Identifier.</param>
        public TEntity GetById(TKey id)
        {
            string nodeMatch = "(x:" + _type + ")";

            var results = _client.Cypher
                                 .Match(nodeMatch)
                                 .Where((TEntity x) => x.Id.ToString() == id.ToString())
                                 .Return(x => x.As<TEntity>())
                                 .Results;

            return results.FirstOrDefault();
        }

        /// <summary>
        /// Insert the specified node.
        /// </summary>
        /// <returns>The insert.</returns>
        /// <param name="entity">Entity.</param>
        public TEntity Insert(TEntity entity)
        {
            //check for null reference type
            if (entity == default(TEntity))
                return default(TEntity);
            //check if entity has Id assigned
            if (!entity.HasPropertyValue<TEntity, TKey>())
            {
                entity = assignEntityId(entity);
                if (entity == null)
                {
                    throw new Exception("Entity requires an assigned Id value for non strong Id prop");
                }
            }

            string nodeCreate = "(x:" + _type + "{entity})";
            _client.Cypher
                   .Create(nodeCreate)
                   .WithParam("entity", entity)
                   .ExecuteWithoutResults();

            return entity;
        }

        /// <summary>
        /// Update the specified node.
        /// </summary>
        /// <returns>The update.</returns>
        /// <param name="entity">Entity.</param>
        public TEntity Update(TEntity entity)
        {
            //check for null reference type
            if (entity == default(TEntity))
                return default(TEntity);
            //check if entity has Id assigned, throw error if no assignment
            if (!entity.HasPropertyValue<TEntity, TKey>())
            {
                throw new Exception("Entity requires an assigned Id value");
            }
            string nodeMatch = "(x:" + _type + ")";
            _client.Cypher
                   .Match(nodeMatch)
                   .Where((TEntity x) => x.Id.ToString() == entity.Id.ToString())
                   .Set("x = {entity}")
                   .WithParam("entity", entity)
                   .ExecuteWithoutResults();

            return entity;
        }

        /// <summary>
        /// Delete the specified entity.
        /// </summary>
        /// <returns>void</returns>
        /// <param name="entity">Entity.</param>
        public void Delete(TEntity entity)
        {
            if (entity == default(TEntity))
                return;
                
            //delete node and ingpoing/outgoing relationships
            string nodeMatch = "(x:" + _type + ")";
            string nodeOptionalMatch = "(x)-[r]-()";
            _client.Cypher
                   .Match(nodeMatch)
                   .OptionalMatch(nodeOptionalMatch)
                   .Where((TEntity x) => x.Id.ToString() == entity.Id.ToString())
                   .Delete("r, x")
                   .ExecuteWithoutResults();
                  
        }

        /// <summary>
        /// Find the specified predicate.
        /// </summary>
        /// <returns>The find.</returns>
        /// <param name="predicate">Predicate.</param>
        public IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> predicate)
        {
            if (predicate.PassesExpressionTest<TEntity>())
            {
                return find(predicate);
            }
            else
            {
                var expression = predicate.ToCypherStringQuery<TEntity>();
                return Find(expression);
            }
        }

        public IEnumerable<TEntity> Find(Expression<Func<TEntity, bool>> predicate, int limit, int skip)
        {
            if (predicate.PassesExpressionTest<TEntity>())
            {
                return find(predicate,limit,skip);
            }
            else
            {
                var expression = predicate.ToCypherStringQuery<TEntity>();
                return Find(expression,limit,skip);
            }
        }

        /// <summary>
        /// Find the specified label and predicate.
        /// </summary>
        /// <returns>The find.</returns>
        /// <param name="label">Label.</param>
        /// <param name="predicate">Predicate.</param>
        public IEnumerable<TEntity> Find(string label, Expression<Func<TEntity, bool>> predicate)
        {
            if (predicate.PassesExpressionTest<TEntity>())
            {
                return findByLabel(label, predicate);
            }
            else
            {
                var expression = predicate.ToCypherStringQuery<TEntity>();
                return findByLabel(label, expression);
            }
        }

        public IEnumerable<TEntity> Find(string label, Expression<Func<TEntity, bool>> predicate, int limit, int skip)
        {
            if (predicate.PassesExpressionTest<TEntity>())
            {
                return findByLabel(label, predicate,limit, skip);
            }
            else
            {
                var expression = predicate.ToCypherStringQuery<TEntity>();
                return findByLabel(label, expression, limit, skip);
            }
        }


        /// <summary>
        /// Find the specified expression.
        /// </summary>
        /// <returns>The find.</returns>
        /// <param name="expression">Expression.</param>
        public IEnumerable<TEntity> Find(string expression)
        {
            string nodeMatch = "(x:" + _type + ")";
            return _client.Cypher
                          .Match(nodeMatch)
                          .Where(expression)
                          .Return(x => x.As<TEntity>())
                          .Results;
        }

        public IEnumerable<TEntity> Find(string expression, int limit, int skip)
        {
            string nodeMatch = "(x:" + _type + ")";
            return _client.Cypher
                          .Match(nodeMatch)
                          .Where(expression)
                          .Return(x => x.As<TEntity>())
                          .Skip(skip)
                          .Limit(limit)
                          .Results;
        }

        /// <summary>
        /// Find the specified label and expression.
        /// </summary>
        /// <returns>The find.</returns>
        /// <param name="label">Label.</param>
        /// <param name="expression">Expression.</param>
        public IEnumerable<TEntity> Find(string label, string expression)
        {
            return findByLabel(label, expression);
        }

        public IEnumerable<TEntity> Find(string label, string expression, int limit, int skip)
        {
            return findByLabel(label, expression,limit, skip);
        }

        /// <summary>
        /// Gets all nodes by label.
        /// </summary>
        /// <returns>The all by label.</returns>
        /// <param name="label">Label.</param>
        public IEnumerable<TEntity> GetAllByLabel(string label)
        {
            string whereMatch = "(x:" + _type + ":" + label + ")";
            return _client.Cypher
                          .Match("(x)")
                          .Where(whereMatch)
                          .Return(x => x.As<TEntity>())
                          .Results;
        }

        /// <summary>
        /// Gets all by label.
        /// </summary>
        /// <returns>The all by label.</returns>
        /// <param name="label">Label.</param>
        /// <param name="limit">Limit.</param>
        /// <param name="skip">Skip.</param>
        public IEnumerable<TEntity> GetAllByLabel(string label, int limit, int skip)
        {
            string whereMatch = "(x:" + _type + ":" + label + ")";
            return _client.Cypher
                          .Match("(x)")
                          .Where(whereMatch)
                          .Return(x => x.As<TEntity>())
                          .Results;
        }

        /// <summary>
        /// Adds the label to a node.
        /// </summary>
        /// <returns><c>true</c>, if label was added, <c>false</c> otherwise.</returns>
        /// <param name="entity">Entity.</param>
        /// <param name="label">Label.</param>
        public bool AddLabel(TEntity entity, string label)
        {
            return AddLabel(entity.Id, label);
        }

        /// <summary>
        /// Adds the label to a node.
        /// </summary>
        /// <returns><c>true</c>, if label was added, <c>false</c> otherwise.</returns>
        /// <param name="id">Identifier.</param>
        /// <param name="label">Label.</param>
        public bool AddLabel(TKey id, string label)
        {
            string nodeMatch = "(x:" + _type + ")";
            _client.Cypher
                   .Match(nodeMatch)
                   .Where((TEntity x) => x.Id.ToString() == id.ToString())
                   .Set("x:" + label)
                   .ExecuteWithoutResults();

            return true;
        }

        /// <summary>
        /// Removes the label from a node.
        /// </summary>
        /// <returns><c>true</c>, if label was removed, <c>false</c> otherwise.</returns>
        /// <param name="entity">Entity.</param>
        /// <param name="label">Label.</param>
        public bool RemoveLabel(TEntity entity, string label)
        {
            return RemoveLabel(entity.Id, label);
        }

        /// <summary>
        /// Removes the label form a node.
        /// </summary>
        /// <returns><c>true</c>, if label was removed, <c>false</c> otherwise.</returns>
        /// <param name="id">Identifier.</param>
        /// <param name="label">Label.</param>
        public bool RemoveLabel(TKey id, string label)
        {
            string nodeMatch = "(x:" + _type + ")";
            _client.Cypher
                   .Match(nodeMatch)
                   .Where((TEntity x) => x.Id.ToString() == id.ToString())
                   .Remove("x:" + label)
                   .ExecuteWithoutResults();

            return true;
        }

        /// <summary>
        /// Get all the labels for a node.
        /// </summary>
        /// <returns>The labels.</returns>
        /// <param name="entity">Entity.</param>
        public IEnumerable<string> GetLabels(TEntity entity)
        {
            return GetLabels(entity.Id);
        }

        /// <summary>
        /// Get all the labels for a node.
        /// </summary>
        /// <returns>The labels.</returns>
        /// <param name="id">Identifier.</param>
        public IEnumerable<string> GetLabels(TKey id)
        {
            string nodeMatch = "(x:" + _type + ")";
            var results = _client.Cypher
                                 .Match(nodeMatch)
                                 .Where((TEntity x) => x.Id.ToString() == id.ToString())
                                 .Return(x => x.Labels())
                                 .Results;

            return results.FirstOrDefault();
        }

        /// <summary>
        /// Adds a relationship between two nodes.
        /// </summary>
        /// <returns><c>true</c>, if relationship was added, <c>false</c> otherwise.</returns>
        /// <param name="inbound">Inbound.</param>
        /// <param name="outbound">Outbound.</param>
        /// <param name="relationship">Relationship.</param>
        /// <typeparam name="TOut">The 1st type parameter.</typeparam>
        public bool AddRelationship<TOut>(TEntity inbound, TOut outbound, string relationship) where TOut : class, IEntity<TKey>
        {
            return AddRelationship<TOut>(inbound.Id, outbound.Id, relationship);
        }

        /// <summary>
        /// Adds a relationship between two nodes.
        /// </summary>
        /// <returns><c>true</c>, if relationship was added, <c>false</c> otherwise.</returns>
        /// <param name="inboundId">Inbound identifier.</param>
        /// <param name="outboundId">Outbound identifier.</param>
        /// <param name="relationship">Relationship.</param>
        /// <typeparam name="TOut">The 1st type parameter.</typeparam>
        public bool AddRelationship<TOut>(TKey inboundId, TKey outboundId, string relationship) where TOut : class, IEntity<TKey>
        {
            string nodeMatch1 = "(x:" + _type + ")";
            string _type2 = typeof(TOut).Name;
            string nodeMatch2 = "(y:" + _type2 + ")";
            string relation = "(x)-[:" + relationship + "]->(y)";
            _client.Cypher
                   .Match(nodeMatch1, nodeMatch2)
                   .Where((TEntity x) => x.Id.ToString() == inboundId.ToString())
                   .AndWhere((TOut y) => y.Id.ToString() == outboundId.ToString())
                   .CreateUnique(relation)
                   .ExecuteWithoutResults();

            return true;
        }

        /// <summary>
        /// Adds a relationship.
        /// </summary>
        /// <returns><c>true</c>, if relationship was added, <c>false</c> otherwise.</returns>
        /// <param name="inboundId">Inbound identifier.</param>
        /// <param name="outboundId">Outbound identifier.</param>
        /// <param name="relationship">Relationship.</param>
        /// <param name="relation">Relation.</param>
        /// <typeparam name="TOut">The 1st type parameter.</typeparam>
        /// <typeparam name="TRelation">The 2nd type parameter.</typeparam>
        public bool AddRelationship<TOut, TRelation>(TKey inboundId, TKey outboundId, string relationship, TRelation relation)
            where TOut : class, IEntity<TKey>
            where TRelation : class
        {
            string props = relation.ToObjectLiteralString();
            string nodeMatch1 = "(x:" + _type + ")";
            string _type2 = typeof(TOut).Name;
            string nodeMatch2 = "(y:" + _type2 + ")";
            string rel = "(x)-[:" + relationship + " " + props + "]->(y)";
            _client.Cypher
                   .Match(nodeMatch1, nodeMatch2)
                   .Where((TEntity x) => x.Id.ToString() == inboundId.ToString())
                   .AndWhere((TOut y) => y.Id.ToString() == outboundId.ToString())
                   .CreateUnique(rel)
                   .ExecuteWithoutResults();

            return true;
        }

        /// <summary>
        /// Has a relationship.
        /// </summary>
        /// <returns><c>true</c>, if relationship was hased, <c>false</c> otherwise.</returns>
        /// <param name="inboundId">Inbound identifier.</param>
        /// <param name="outboundId">Outbound identifier.</param>
        /// <param name="relationship">Relationship.</param>
        /// <typeparam name="TOut">The 1st type parameter.</typeparam>
        public bool HasRelationship<TOut>(TKey inboundId, TKey outboundId, string relationship) where TOut : class, IEntity<TKey>
        {
            string _type2 = typeof(TOut).Name;
            string optionalMatch = "(x:" + _type + ")-[" + relationship + "]->(y:" + _type2 + ")";
            var result = _client.Cypher
                                .OptionalMatch(optionalMatch)
                                .Where((TEntity x) => x.Id.ToString() == inboundId.ToString())
                                .AndWhere((TOut y) => y.Id.ToString() == outboundId.ToString())
                                .Return(y => y.As<TOut>())
                                .Results;

            var entity = result.FirstOrDefault();
            return (entity != null);

        }

        /// <summary>
        /// Gets the related nodes.
        /// </summary>
        /// <returns>The related nodes.</returns>
        /// <param name="entity">Entity.</param>
        /// <param name="relationship">Relationship.</param>
        /// <typeparam name="TOut">The 1st type parameter.</typeparam>
        public IEnumerable<TOut> GetRelatedNodes<TOut>(TEntity entity, string relationship) where TOut : class, IEntity<TKey>
        {
            return GetRelatedNodes<TOut>(entity.Id, relationship);
        }

        /// <summary>
        /// Gets the related nodes.
        /// </summary>
        /// <returns>The related nodes.</returns>
        /// <param name="id">Identifier.</param>
        /// <param name="relationship">Relationship.</param>
        /// <typeparam name="TOut">The 1st type parameter.</typeparam>
        public IEnumerable<TOut> GetRelatedNodes<TOut>(TKey id, string relationship) where TOut : class, IEntity<TKey>
        {
            string _type2 = typeof(TOut).Name;
            string optionalMatch = "(x:" + _type + ")-[" + relationship + "]->(y:" + _type2 + ")";
            var results = _client.Cypher
                                 .OptionalMatch(optionalMatch)
                                 .Where((TEntity x) => x.Id.ToString() == id.ToString())
                                 .Return((y) => y.CollectAs<TOut>())
                                 .Results;

            return results.FirstOrDefault();
        }

        /// <summary>
        /// Gets the related nodes.
        /// </summary>
        /// <returns>The related nodes.</returns>
        /// <param name="id">Identifier.</param>
        /// <param name="relationship">Relationship.</param>
        /// <param name="predicate">Predicate.</param>
        /// <typeparam name="TOut">The 1st type parameter.</typeparam>
        /// <typeparam name="TRelation">The 2nd type parameter.</typeparam>
        public IEnumerable<TOut> GetRelatedNodes<TOut, TRelation>(TKey id, string relationship, Expression<Func<TRelation, bool>> predicate)
            where TOut : class, IEntity<TKey>
            where TRelation : class
        {
            string _type2 = typeof(TOut).Name;
            string optionalMatch = "(x:" + _type + ")-[r:" + relationship + "]->(y:" + _type2 + ")";
            var results = _client.Cypher
                                 .OptionalMatch(optionalMatch)
                                 .Where((TEntity x) => x.Id.ToString() == id.ToString())
                                 .AndWhere(predicate)
                                 .Return((y) => y.CollectAs<TOut>())
                                 .Results;

            return results.FirstOrDefault();
        }

        /// <summary>
        /// Gets the related nodes count.
        /// </summary>
        /// <returns>The related nodes count.</returns>
        /// <param name="entity">Entity.</param>
        /// <param name="relationship">Relationship.</param>
        /// <typeparam name="TOut">The 1st type parameter.</typeparam>
        public int GetRelatedNodesCount<TOut>(TEntity entity, string relationship) where TOut : class, IEntity<TKey>
        {
            return GetRelatedNodesCount<TOut>(entity.Id, relationship);
        }

        /// <summary>
        /// Gets the related nodes count.
        /// </summary>
        /// <returns>The related nodes count.</returns>
        /// <param name="id">Identifier.</param>
        /// <param name="relationship">Relationship.</param>
        /// <typeparam name="TOut">The 1st type parameter.</typeparam>
        public int GetRelatedNodesCount<TOut>(TKey id, string relationship) where TOut : class, IEntity<TKey>
        {
            string _type2 = typeof(TOut).Name;
            string optionalMatch = "(x:" + _type + ")-[" + relationship + "]->(y:" + _type2 + ")";
            var results = _client.Cypher
                                 .OptionalMatch(optionalMatch)
                                 .Where((TEntity x) => x.Id.ToString() == id.ToString())
                                 .Return((y) => y.Count())
                                 .Results;

            long count = results.FirstOrDefault();
            return Convert.ToInt32(count);
        }

        public int GetRelatedNodesCount<TOut, TRelation>(TKey id, string relationship, Expression<Func<TRelation, bool>> predicate)
            where TOut : class, IEntity<TKey>
            where TRelation : class
        {
            string _type2 = typeof(TOut).Name;
            string optionalMatch = "(x:" + _type + ")-[" + relationship + "]->(y:" + _type2 + ")";
            var results = _client.Cypher
                                 .OptionalMatch(optionalMatch)
                                 .Where((TEntity x) => x.Id.ToString() == id.ToString())
                                 .AndWhere(predicate)
                                 .Return((y) => y.Count())
                                 .Results;

            long count = results.FirstOrDefault();
            return Convert.ToInt32(count);
        }


        /// <summary>
        /// Deletes a relationship.
        /// </summary>
        /// <returns><c>true</c>, if relationship was deleted, <c>false</c> otherwise.</returns>
        /// <param name="inbound">Inbound.</param>
        /// <param name="outbound">Outbound.</param>
        /// <param name="relationship">Relationship.</param>
        /// <typeparam name="TOut">The 1st type parameter.</typeparam>
        public bool DeleteRelationship<TOut>(TEntity inbound, TOut outbound, string relationship) where TOut : class, IEntity<TKey>
        {
            return DeleteRelationship<TOut>(inbound.Id, outbound.Id, relationship);
        }

        /// <summary>
        /// Deletes a relationship.
        /// </summary>
        /// <returns><c>true</c>, if relationship was deleted, <c>false</c> otherwise.</returns>
        /// <param name="inboundId">Inbound identifier.</param>
        /// <param name="outboundId">Outbound identifier.</param>
        /// <param name="relationship">Relationship.</param>
        /// <typeparam name="TOut">The 1st type parameter.</typeparam>
        public bool DeleteRelationship<TOut>(TKey inboundId, TKey outboundId, string relationship) where TOut : class, IEntity<TKey>
        {
            string nodeMatch1 = "(x:" + _type + ")";
            string _type2 = typeof(TOut).Name;
            string nodeMatch2 = "(y:" + _type2 + ")";
            string relation = "x-[:" + relationship + "]->y";
            _client.Cypher
                   .Match(nodeMatch1, nodeMatch2)
                   .Where((TEntity x) => x.Id.ToString() == inboundId.ToString())
                   .AndWhere((TOut y) => y.Id.ToString() == outboundId.ToString())
                   .Remove(relation)
                   .ExecuteWithoutResults();

            return true;
        }

        /// <summary>
        /// Creates the restraint.
        /// </summary>
        /// <returns><c>true</c>, if restraint was created, <c>false</c> otherwise.</returns>
        public bool CreateConstraint()
        {
            string identity = "x:" + _type;
            _client.Cypher
                   .CreateUniqueConstraint(identity, "x.Id")
                   .ExecuteWithoutResults();

            return true;
        }

        /// <summary>
        /// Creates the index.
        /// </summary>
        /// <returns><c>true</c>, if index was created, <c>false</c> otherwise.</returns>
        public bool CreateIndex()
        {
            string index = "INDEX ON :" + _type + "(Id)";
            _client.Cypher
                   .Create(index)
                   .ExecuteWithoutResults();

            return true;
        }

        /// <summary>
        /// Creates the index on the specified property.
        /// </summary>
        /// <returns><c>true</c>, if index was created, <c>false</c> otherwise.</returns>
        /// <param name="property">Property.</param>
        public bool CreateIndex(string property)
        {
            string index = "INDEX ON :" + _type + "(" + property + ")";
            _client.Cypher
                   .Create(index)
                   .ExecuteWithoutResults();

            return true;
        }

        #region "Private"

        /// <summary>
        /// Find by the specified predicate.
        /// </summary>
        /// <returns>The find.</returns>
        /// <param name="predicate">Predicate.</param>
        private IEnumerable<TEntity> find(Expression<Func<TEntity, bool>> predicate)
        {
            string nodeMatch = "(x:" + _type + ")";
            return _client.Cypher
                          .Match(nodeMatch)
                          .Where(predicate)
                          .Return(x => x.As<TEntity>())
                          .Results;
        }

        /// <summary>
        /// Find by the specified predicate.
        /// </summary>
        /// <returns>The paged.</returns>
        /// <param name="predicate">Predicate.</param>
        /// <param name="limit">Limit.</param>
        /// <param name="skip">Skip.</param>
        private IEnumerable<TEntity> find(Expression<Func<TEntity, bool>> predicate, int limit, int skip)
        {
            string nodeMatch = "(x:" + _type + ")";
            return _client.Cypher
                          .Match(nodeMatch)
                          .Where(predicate)
                          .Return(x => x.As<TEntity>())
                          .Skip(skip)
                          .Limit(limit)
                          .Results;
        }

        /// <summary>
        /// Finds  by label and expression.
        /// </summary>
        /// <returns>The by label.</returns>
        /// <param name="label">Label.</param>
        /// <param name="expression">Expression.</param>
        private IEnumerable<TEntity> findByLabel(string label, string expression)
        {
            string nodeMatch = "(x:" + _type + ":" + label + ")";
            return _client.Cypher
                          .Match(nodeMatch)
                          .Where(expression)
                          .Return(x => x.As<TEntity>())
                          .Results;
        }

        /// <summary>
        /// Finds by label and expression.
        /// </summary>
        /// <returns>The by label.</returns>
        /// <param name="label">Label.</param>
        /// <param name="expression">Expression.</param>
        /// <param name="limit">Limit.</param>
        /// <param name="skip">Skip.</param>
        private IEnumerable<TEntity> findByLabel(string label, string expression, int limit, int skip)
        {
            string nodeMatch = "(x:" + _type + ":" + label + ")";
            return _client.Cypher
                          .Match(nodeMatch)
                          .Where(expression)
                          .Return(x => x.As<TEntity>())
                          .Skip(skip)
                          .Limit(limit)
                          .Results;
        }

        /// <summary>
        /// Finds the by label and predicate.
        /// </summary>
        /// <returns>The by label.</returns>
        /// <param name="label">Label.</param>
        /// <param name="predicate">Predicate.</param>
        private IEnumerable<TEntity> findByLabel(string label, Expression<Func<TEntity, bool>> predicate)
        {
            string nodeMatch = "(x:" + _type + ":" + label + ")";
            return _client.Cypher
                          .Match(nodeMatch)
                          .Where(predicate)
                          .Return(x => x.As<TEntity>())
                          .Results;
        }

        /// <summary>
        /// Finds by label and predicate.
        /// </summary>
        /// <returns>The by label.</returns>
        /// <param name="label">Label.</param>
        /// <param name="predicate">Predicate.</param>
        /// <param name="limit">Limit.</param>
        /// <param name="skip">Skip.</param>
        private IEnumerable<TEntity> findByLabel(string label, Expression<Func<TEntity, bool>> predicate, int limit, int skip)
        {
            string nodeMatch = "(x:" + _type + ":" + label + ")";
            return _client.Cypher
                          .Match(nodeMatch)
                          .Where(predicate)
                          .Return(x => x.As<TEntity>())
                          .Skip(skip)
                          .Limit(limit)
                          .Results;
        }

        /// <summary>
        /// Assigns the entity identifier.
        /// </summary>
        /// <returns>The entity identifier.</returns>
        /// <param name="entity">Entity.</param>
        private TEntity assignEntityId(TEntity entity)
        {
            Type type = entity.GetType();
            var info = type.GetProperty("Id");
            if (_tKeyType == typeof(int))
            {
                return null;
            }
            else if (_tKeyType == typeof(long))
            {
                return null;
            }
            else if (_tKeyType == typeof(Guid))
            {
                info.SetValue(entity, Guid.NewGuid());
                return entity;
            }
            else if (_tKeyType == typeof(String))
            {
                info.SetValue(entity, Guid.NewGuid().ToString());
                return entity;
            }
            else
            {
                return null;
            }
        }

        #endregion


    }
}
