using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RevStackCore.Neo4j
{
    public static class ExpressionParser
    {
        public static bool PassesExpressionTest<TEntity>(this Expression<Func<TEntity, bool>> predicate)
        {
            string expBody = ((LambdaExpression)predicate).Body.ToString(); 
            if(expBody.IndexOf(".Equals", StringComparison.Ordinal) > -1 
               || expBody.IndexOf(".ToLower",StringComparison.Ordinal) > -1 
               || expBody.IndexOf(".ToUpper",StringComparison.Ordinal) > -1)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static string ToCypherStringQuery<TEntity>(this Expression<Func<TEntity, bool>> predicate)
        {
            string query = RecurseExpression(predicate.Body);
            return query;
        }


        private static string RecurseExpression(Expression expression, bool isUnary = false, bool quote = true)
        {
            if (expression is UnaryExpression)
            {
                var unary = (UnaryExpression)expression;
                if (unary.NodeType == ExpressionType.Convert)
                {
                    return RecurseExpression(unary.Operand, true);
                }
                
                var right = RecurseExpression(unary.Operand, true);
                return "(" + NodeTypeToString(unary.NodeType) + " " + right + ")";
            }
            if (expression is BinaryExpression)
            {
                var body = (BinaryExpression)expression;
                var right = RecurseExpression(body.Right);
                return "(" + RecurseExpression(body.Left) + " " + NodeTypeToString(body.NodeType) + " " + right + ")";
            }
            if (expression is ConstantExpression)
            {
                var constant = (ConstantExpression)expression;
                return ValueToString(constant.Value, isUnary, quote);
            }
            if (expression is MemberExpression)
            {
                var member = (MemberExpression)expression;
              
                if (member.Member is FieldInfo)
                {
                    return ValueToString(GetValue(member), isUnary, quote);
                }

                if (member.Member is PropertyInfo)
                {
                    string strMember= member.ToString();
                    if(strMember.IndexOf("value(", StringComparison.Ordinal) > -1)
                    {
                        return ValueToString(GetValue(member), isUnary, quote);
                    }
                    else
                    {
                        return strMember;
                    }
                }
               
                throw new Exception($"Expression does not refer to a property or field: {expression}");
            }

            if (expression is MethodCallExpression)
            {
                var methodCall = (MethodCallExpression)expression;
               
                if (methodCall.Method.Name == "ToLower")
                {
                    return "LOWER(" + RecurseExpression(methodCall.Object) + ")";
                }
                if (methodCall.Method.Name == "ToUpper")
                {
                    return "UPPER(" + RecurseExpression(methodCall.Object) + ")";
                }
                if (methodCall.Method.Name == "Equals")
                {
                    return "(" + RecurseExpression(methodCall.Object) + " = " + RecurseExpression(methodCall.Arguments[0], quote: false) + ")";
                }
                if (methodCall.Method.Name == "Contains")
                {
                    return "(" + RecurseExpression(methodCall.Object) + " CONTAINS " + RecurseExpression(methodCall.Arguments[0], quote: false) + ")";
                }
                if (methodCall.Method.Name == "StartsWith")
                {
                    return "(" + RecurseExpression(methodCall.Object) + " STARTS WITH " + RecurseExpression(methodCall.Arguments[0], quote: false) + ")";
                }
                if (methodCall.Method.Name == "EndsWith")
                {
                    return "(" + RecurseExpression(methodCall.Object) + " ENDS WITH " + RecurseExpression(methodCall.Arguments[0], quote: false) + ")";
                }
              
                throw new Exception("Unsupported method call: " + methodCall.Method.Name);
            }
            string name = expression.GetType().Name;
            throw new Exception("Unsupported expression: " + expression.GetType().Name);
        }

        private static string ValueToString(object value, bool isUnary, bool quote)
        {
            if(value is string)
            {
                return "\"" + value.ToString() + "\"";
            }
            else
            {
                return value.ToString();
            }
           
        }

        private static bool IsEnumerableType(Type type)
        {
            return type
                .GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }

        private static object GetValue(Expression member)
        {
            var objectMember = Expression.Convert(member, typeof(object));
            var getterLambda = Expression.Lambda<Func<object>>(objectMember);
            var getter = getterLambda.Compile();
            return getter();
        }

        private static object NodeTypeToString(ExpressionType nodeType)
        {
            switch (nodeType)
            {
                case ExpressionType.Add:
                    return "+";
                case ExpressionType.And:
                    return "&";
                case ExpressionType.AndAlso:
                    return "AND";
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.ExclusiveOr:
                    return "^";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.Modulo:
                    return "%";
                case ExpressionType.Multiply:
                    return "*";
                case ExpressionType.Negate:
                    return "-";
                case ExpressionType.Not:
                    return "NOT";
                case ExpressionType.NotEqual:
                    return "<>";
                case ExpressionType.Or:
                    return "|";
                case ExpressionType.OrElse:
                    return "OR";
                case ExpressionType.Subtract:
                    return "-";
                case ExpressionType.Convert:
                    return "";
            }
            throw new Exception($"Unsupported node type: {nodeType}");
        }
    }
}
