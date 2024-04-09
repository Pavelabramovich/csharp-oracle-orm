using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OracleOrm;

internal class ProjectionBuilder : DbExpressionVisitor
{
    private static readonly MethodInfo s_ChangeTypeMethod
        = typeof(Convert).GetMethod(nameof(Convert.ChangeType), [typeof(object), typeof(Type)])!;


    ParameterExpression row;

    private static MethodInfo miGetValue;


    string rowAlias;
    static MethodInfo miExecuteSubQuery;


    internal ProjectionBuilder()
    {
        if (miGetValue == null)
        {
            miGetValue = typeof(ProjectionRow).GetMethod("GetValue");
            miExecuteSubQuery = typeof(ProjectionRow).GetMethod("ExecuteSubQuery");
        }
    }

    internal LambdaExpression Build(Expression expression)
    {
        this.row = Expression.Parameter(typeof(ProjectionRow), "row");
        Expression body = this.Visit(expression);

        return Expression.Lambda(body, this.row);
    }

    public override Expression VisitColumn(ColumnExpression column)
    {
        var rowExpression = Expression.Call(this.row, miGetValue, Expression.Constant(column.Ordinal));

        var convertExpression = Expression.Call(instance: null, s_ChangeTypeMethod, rowExpression, Expression.Constant(column.Type));
        return Expression.Convert(convertExpression, column.Type);
    }
}

