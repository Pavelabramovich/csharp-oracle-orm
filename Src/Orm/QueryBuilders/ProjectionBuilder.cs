using System.Linq.Expressions;
using System.Reflection;


namespace OracleOrm;


internal class ProjectionBuilder : SqlExpressionVisitor
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

    internal LambdaExpression Build(Expression expression, string alias)
    {
        this.row = Expression.Parameter(typeof(ProjectionRow), "row");

        this.rowAlias = alias;
        Expression body = this.Visit(expression);

        return Expression.Lambda(body, this.row);
    }



    protected internal override Expression VisitColumn(ColumnExpression column)
    {
        if (column.Alias == this.rowAlias)
        {
            var rowExpression = Expression.Call(this.row, miGetValue, Expression.Constant(column.Ordinal));

            var convertExpression = Expression.Call(instance: null, s_ChangeTypeMethod, rowExpression, Expression.Constant(column.Type));
            return Expression.Convert(convertExpression, column.Type);
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    protected internal override Expression VisitProjection(ProjectionExpression proj)
    {
        LambdaExpression subQuery = Expression.Lambda(base.VisitProjection(proj), this.row);

        Type elementType = TypeSystem.GetElementType(subQuery.Body.Type);
        MethodInfo mi = miExecuteSubQuery.MakeGenericMethod(elementType);

        var res = Expression.Call(this.row, mi, Expression.Constant(subQuery));

        var convertExpression = Expression.Call(instance: null, s_ChangeTypeMethod, res, Expression.Constant(proj.Type));
        return Expression.Convert(convertExpression, proj.Type);
    }
}
