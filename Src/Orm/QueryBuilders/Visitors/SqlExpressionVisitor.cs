using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace OracleOrm;


public class SqlExpressionVisitor : ExpressionVisitor
{
    protected override Expression VisitExtension(Expression node)
    {
        if (node is SqlExpression sqlExpression)
            return VisitSqlExpression(sqlExpression);

        return base.VisitExtension(node);
    }

    private Expression VisitSqlExpression(SqlExpression sqlExpression)
    {
        return sqlExpression.SqlNodeType switch
        {
            SqlExpressionType.Table => VisitTable((TableExpression)sqlExpression),
            SqlExpressionType.Column => VisitColumn((ColumnExpression)sqlExpression),
            SqlExpressionType.Select => VisitSelect((SelectExpression)sqlExpression),
            SqlExpressionType.Projection => VisitProjection((ProjectionExpression)sqlExpression),
            SqlExpressionType.FunctionCalling => VisitFunctionCalling((FunctionCallingExpression)sqlExpression),
            SqlExpressionType.Join => VisitJoin((JoinExpression)sqlExpression),
            SqlExpressionType.SubQuery => VisitSubQuery((SubQueryExpression)sqlExpression),

            _ => throw new NotImplementedException($"This type sql node type {sqlExpression.SqlNodeType} is not supported.")
        };
    }


    protected virtual Expression VisitTable(TableExpression table)
    {
        return table;
    }

    protected virtual Expression VisitSubQuery(SubQueryExpression subQuery)
    {
        return subQuery;
    }


    protected virtual Expression VisitColumn(ColumnExpression column)
    {
        return column;
    }

    protected virtual Expression VisitJoin(JoinExpression join)
    {
        Expression left = Visit(join.Left);

        Expression right = Visit(join.Right);

        Expression condition = Visit(join.Condition);

        if (left != join.Left || right != join.Right || condition != join.Condition)
        {

            return new JoinExpression(join.Type, join.Join, left, right, condition);

        }

        return join;

    }


    protected virtual Expression VisitSelect(SelectExpression select)
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


    protected virtual Expression VisitSource(Expression source)
    {
        return Visit(source);
    }


    protected virtual Expression VisitProjection(ProjectionExpression proj)
    {
        SelectExpression source = (SelectExpression)Visit(proj.Source);
        Expression projector = Visit(proj.Projector);

        if (source != proj.Source || projector != proj.Projector)
        {
            return new ProjectionExpression(source, projector);
        }

        return proj;
    }

    protected virtual Expression VisitFunctionCalling(FunctionCallingExpression funcCalling)
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
                alternate.Add(new ColumnDeclaration(column.Name, e));
            }
        }

        if (alternate != null)
        {
            return alternate.AsReadOnly();
        }


        return columns;
    }
}
