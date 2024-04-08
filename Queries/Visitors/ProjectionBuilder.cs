using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OracleOrm.Queries.Expressions;


namespace OracleOrm.Queries.Visitors;


public abstract class ProjectionRow
{
    public abstract object GetValue(int index);
}


internal class ProjectionBuilder : DbExpressionVisitor
{
    ParameterExpression row;

    private static MethodInfo miGetValue;


    internal ProjectionBuilder()
    {
        if (miGetValue == null)
        {
            miGetValue = typeof(ProjectionRow).GetMethod("GetValue");
        }
    }

    internal LambdaExpression Build(Expression expression)
    {
        row = Expression.Parameter(typeof(ProjectionRow), "row");
        Expression body = Visit(expression);

        return Expression.Lambda(body, row);
    }

    protected override Expression VisitColumn(ColumnExpression column)
    {
        return Expression.Convert(Expression.Call(row, miGetValue, Expression.Constant(column.Ordinal)), column.Type);
    }
}
