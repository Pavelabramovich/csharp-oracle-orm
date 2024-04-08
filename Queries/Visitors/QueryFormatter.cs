using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using OracleOrm.Queries.Expressions;


namespace OracleOrm.Queries.Visitors;


internal class QueryFormatter : DbExpressionVisitor
{
    StringBuilder sb;
    int indent = 2;
    int depth;


    internal QueryFormatter()
    { }

    internal string Format(Expression expression)
    {
        sb = new StringBuilder();
        Visit(expression);

        return sb.ToString();
    }


    protected enum Identation
    {
        Same,
        Inner,
        Outer
    }


    internal int IdentationWidth
    {
        get { return indent; }
        set { indent = value; }
    }

    private void AppendNewLine(Identation style)
    {
        sb.AppendLine();

        if (style == Identation.Inner)
        {
            depth++;
        }
        else if (style == Identation.Outer)
        {
            depth--;

            System.Diagnostics.Debug.Assert(depth >= 0);
        }

        for (int i = 0, n = depth * indent; i < n; i++)
        {
            sb.Append(" ");
        }
    }

    protected override Expression VisitMethodCall(MethodCallExpression m)
    {
        throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));
    }

    protected override Expression VisitUnary(UnaryExpression u)
    {
        switch (u.NodeType)
        {
            case ExpressionType.Not:
                sb.Append(" NOT ");

                Visit(u.Operand);
                break;

            default:
                throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
        }

        return u;
    }

    protected override Expression VisitBinary(BinaryExpression b)
    {
        sb.Append("(");
        Visit(b.Left);

        switch (b.NodeType)
        {
            case ExpressionType.And:
                sb.Append(" AND ");
                break;

            case ExpressionType.Or:
                sb.Append(" OR");
                break;

            case ExpressionType.Equal:
                sb.Append(" = ");
                break;

            case ExpressionType.NotEqual:
                sb.Append(" <> ");
                break;

            case ExpressionType.LessThan:
                sb.Append(" < ");
                break;

            case ExpressionType.LessThanOrEqual:
                sb.Append(" <= ");
                break;

            case ExpressionType.GreaterThan:
                sb.Append(" > ");
                break;

            case ExpressionType.GreaterThanOrEqual:
                sb.Append(" >= ");
                break;

            default:
                throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));
        }

        Visit(b.Right);
        sb.Append(")");
        return b;
    }

    protected override Expression VisitConstant(ConstantExpression c)
    {
        if (c.Value == null)
        {
            sb.Append("NULL");
        }
        else
        {
            switch (Type.GetTypeCode(c.Value.GetType()))
            {
                case TypeCode.Boolean:
                    sb.Append((bool)c.Value ? 1 : 0);
                    break;

                case TypeCode.String:
                    sb.Append("'");
                    sb.Append(c.Value);
                    sb.Append("'");
                    break;

                case TypeCode.Object:
                    throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", c.Value));

                default:
                    sb.Append(c.Value);
                    break;
            }
        }

        return c;
    }

    protected override Expression VisitColumn(ColumnExpression column)
    {
        if (!string.IsNullOrEmpty(column.Alias))
        {
            sb.Append(column.Alias);
            sb.Append(".");
        }

        sb.Append(column.Name);

        return column;
    }

    protected override Expression VisitSelect(SelectExpression select)
    {
        sb.Append("SELECT ");
        for (int i = 0, n = select.Columns.Count; i < n; i++)
        {
            ColumnDeclaration column = select.Columns[i];

            if (i > 0)
            {
                sb.Append(", ");
            }

            ColumnExpression c = Visit(column.Expression) as ColumnExpression;

            if (c == null || c.Name != select.Columns[i].Name)
            {
                sb.Append(" AS ");
                sb.Append(column.Name);
            }
        }

        if (select.From != null)
        {
            AppendNewLine(Identation.Same);
            sb.Append("FROM ");

            VisitSource(select.From);
        }

        if (select.Where != null)
        {
            AppendNewLine(Identation.Same);
            sb.Append("WHERE ");
            Visit(select.Where);
        }

        return select;
    }

    protected override Expression VisitSource(Expression source)
    {
        switch ((DbExpressionType)source.NodeType)
        {
            case DbExpressionType.Table:
                TableExpression table = (TableExpression)source;
                sb.Append(table.Name);
                sb.Append(" AS ");
                sb.Append(table.Alias);
                break;

            case DbExpressionType.Select:
                SelectExpression select = (SelectExpression)source;
                sb.Append("(");

                AppendNewLine(Identation.Inner);
                Visit(select);

                AppendNewLine(Identation.Outer);

                sb.Append(")");
                sb.Append(" AS ");
                sb.Append(select.Alias);
                break;

            default:
                throw new InvalidOperationException("Select source is not valid type");
        }

        return source;
    }
}
