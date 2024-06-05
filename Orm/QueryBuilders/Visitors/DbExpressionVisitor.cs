using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;



namespace OracleOrm;

public class DbExpressionVisitor : ExpressionVisitor
{
    public override Expression Visit(Expression? expr)
    {
        if (expr == null)
        {
            return null;
        }

        return (DbExpressionType)expr.NodeType switch
        {
            DbExpressionType.Table => VisitTable((TableExpression)expr),
            DbExpressionType.Column => VisitColumn((ColumnExpression)expr),
            DbExpressionType.Select => VisitSelect((SelectExpression)expr),
            DbExpressionType.Projection => VisitProjection((ProjectionExpression)expr),
            DbExpressionType.FunctionCalling => VisitFunctionCalling((FunctionCallingExpression)expr),
            DbExpressionType.Join => VisitJoin((JoinExpression)expr),
            DbExpressionType.SubQuery => VisitSubQuery((SubQueryExpression)expr), 
            _ => base.Visit(expr),
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


    public virtual Expression VisitColumn(ColumnExpression column)
    {
        return column;
    }

    protected virtual Expression VisitJoin(JoinExpression join)
    {

        Expression left = this.Visit(join.Left);

        Expression right = this.Visit(join.Right);

        Expression condition = this.Visit(join.Condition);

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


    public virtual Expression VisitProjection(ProjectionExpression proj)
    {
        SelectExpression source = (SelectExpression)Visit(proj.Source);
        Expression projector = Visit(proj.Projector);

        if (source != proj.Source || projector != proj.Projector)
        {
            return new ProjectionExpression(source, projector);
        }

        return proj;
    }

    public virtual Expression VisitFunctionCalling(FunctionCallingExpression funcCalling)
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
