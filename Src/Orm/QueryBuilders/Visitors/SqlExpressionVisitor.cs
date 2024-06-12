using System.Collections.ObjectModel;
using System.Linq.Expressions;


namespace OracleOrm;


public class SqlExpressionVisitor : ExpressionVisitor
{
    protected internal virtual Expression VisitTable(TableExpression table)
    {
        return table;
    }

    protected internal virtual Expression VisitSubQuery(SubQueryExpression subQuery)
    {
        return subQuery;
    }


    protected internal virtual Expression VisitColumn(ColumnExpression column)
    {
        return column;
    }

    protected internal virtual Expression VisitJoin(JoinExpression join)
    {
        Expression left = Visit(join.Left);

        Expression right = Visit(join.Right);

        Expression condition = Visit(join.Condition);

        if (left != join.Left || right != join.Right || condition != join.Condition)
        {
            return new JoinExpression(join.Type, join.JoinType, left, right, condition);
        }

        return join;

    }


    protected internal virtual Expression VisitSelect(SelectExpression select)
    {
        Expression from = VisitSource(select.From);
        Expression where = Visit(select.Where);

        ReadOnlyCollection<ColumnDeclaration> columns = VisitColumnDeclarations(select.Columns);

        if (from != select.From || where != select.Where || columns != select.Columns)
        {
            return new SelectExpression(select.Type, select.Alias, columns, from, where);
        }

        return select;
    }


    protected internal virtual Expression VisitSource(Expression source)
    {
        return Visit(source);
    }


    protected internal virtual Expression VisitProjection(ProjectionExpression proj)
    {
        SelectExpression source = (SelectExpression)Visit(proj.Source);
        Expression projector = Visit(proj.Projector);

        if (source != proj.Source || projector != proj.Projector)
        {
            return new ProjectionExpression(source, projector);
        }

        return proj;
    }

    protected internal virtual Expression VisitFunctionCalling(FunctionCallingExpression funcCalling)
    {
        return funcCalling;
    }




    public ReadOnlyCollection<ColumnDeclaration> VisitColumnDeclarations(ReadOnlyCollection<ColumnDeclaration> columns)
    {
        List<ColumnDeclaration> alternate = null;

        for (int i = 0, n = columns.Count; i < n; i++)
        {
            ColumnDeclaration column = columns[i];
            Expression e = Visit(column.Expression);

            if (alternate == null && e != column.Expression)
            {
                alternate = columns.Take(i).ToList();
            }

            if (alternate != null)
            {
                alternate.Add(new ColumnDeclaration(column.Name, (ColumnExpression)e));
            }
        }

        if (alternate != null)
        {
            return alternate.AsReadOnly();
        }


        return columns;
    }
}
