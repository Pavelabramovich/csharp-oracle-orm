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
    public override Expression Visit(Expression? exp)
    {
        if (exp == null)
        {
            return null;
        }

        return (DbExpressionType)exp.NodeType switch
        {
            DbExpressionType.Table => VisitTable((TableExpression)exp),
            DbExpressionType.Column => VisitColumn((ColumnExpression)exp),
            DbExpressionType.Select => VisitSelect((SelectExpression)exp),
            DbExpressionType.Projection => VisitProjection((ProjectionExpression)exp),
            _ => base.Visit(exp),
        };
    }


    protected virtual Expression VisitTable(TableExpression table)
    {
        return table;
    }


    public virtual Expression VisitColumn(ColumnExpression column)
    {
        return column;
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
