﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OracleOrm;

internal class QueryFormatter : SqlExpressionVisitor
{
    private OracleDbContext _context;
    private readonly StringBuilder sb;

    int indent = 2;
    int depth;

    bool monkeyPatch;

    internal QueryFormatter(OracleDbContext context, bool monkeyPatch = false)
    {
        _context = context;
        sb = new StringBuilder();

        this.monkeyPatch = monkeyPatch;
    }

    internal string Format(Expression expression)
    {
        sb.Clear();
        this.Visit(expression);

    //    Console.WriteLine(sb.ToString());

        return this.sb.ToString();
    }

    protected enum Identation
    {
        Same,
        Inner,
        Outer
    }

    internal int IdentationWidth
    {
        get { return this.indent; }
        set { this.indent = value; }
    }

    private void AppendNewLine(Identation style)
    {
        sb.AppendLine();
        if (style == Identation.Inner)
        {
            this.depth++;
        }
        else if (style == Identation.Outer)
        {
            this.depth--;

            System.Diagnostics.Debug.Assert(this.depth >= 0);
        }

        for (int i = 0, n = this.depth * this.indent; i < n; i++)
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
                this.Visit(u.Operand);
                break;

            default:
                throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
        }

        return u;
    }

    protected override Expression VisitBinary(BinaryExpression b)
    {
        sb.Append("(");
        this.Visit(b.Left);

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
                sb.Append(" != ");
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

            case ExpressionType.Add:
                sb.Append(" + ");
                break;

            case ExpressionType.Subtract:
                sb.Append(" - ");
                break;

            default:
                throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));
        }

        this.Visit(b.Right);
        sb.Append(")");

        return b;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return base.VisitParameter(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (monkeyPatch)
            sb.Append($" {CaseConverter.ToSnakeCase(node.Member.Name)} ");

        return base.VisitMember(node);
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
                    sb.Append(((bool)c.Value) ? 1 : 0);
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


    protected internal override Expression VisitColumn(ColumnExpression column)
    {
        if (!string.IsNullOrEmpty(column.Alias))
        {
            sb.Append(column.Alias);
            sb.Append(".");
        }

        sb.Append(CaseConverter.ToSnakeCase(column.Name));

        return column;
    }

    protected internal override Expression VisitSubQuery(SubQueryExpression subQuery)
    {
        string sql = subQuery.Sql;
        sb.Append(sql);
        return subQuery;
    }

    protected internal override Expression VisitSelect(SelectExpression select)
    {
        sb.Append("SELECT ");

        for (int i = 0, n = select.Columns.Count; i < n; i++)
        {
            ColumnDeclaration column = select.Columns[i];

            if (i > 0)
            {
                sb.Append(", ");
            }

            ColumnExpression c = this.Visit(column.Expression) as ColumnExpression;

            if (c == null || c.Name != select.Columns[i].Name)
            {
                sb.Append(" AS "); // After field
                sb.Append(column.Name);
            }
        }

        if (select.From != null)
        {
            this.AppendNewLine(Identation.Same);
            sb.Append("FROM ");
            this.VisitSource(select.From);
        }

        if (select.Where != null)
        {
            this.AppendNewLine(Identation.Same);

            sb.Append("WHERE ");

            this.Visit(select.Where);
        }

        return select;
    }

    protected internal override Expression VisitSource(Expression source)
    {
        if (source is not SqlExpression sqlExpression)
            throw new InvalidOperationException();

        switch (sqlExpression)
        {
            case TableExpression tableExpression:
                sb.Append(GetTableName(tableExpression.ElementsType));
                sb.Append(" "); // Oracle only
                sb.Append(tableExpression.Alias);
                break;

            case SelectExpression selectExpression:
                sb.Append("(");
                this.AppendNewLine(Identation.Inner);
                this.Visit(selectExpression);
                this.AppendNewLine(Identation.Outer);

                sb.Append(")");
                sb.Append(" "); // Oracle only
                sb.Append(selectExpression.Alias);
                break;

            case JoinExpression joinExpression:
                this.VisitJoin(joinExpression);
                break;

            default:
                throw new InvalidOperationException("Select source is not valid type");
        }

        return source;
    }

    protected internal override Expression VisitFunctionCalling(FunctionCallingExpression funcCalling)
    {
        var method = funcCalling.Method;
        var instance = funcCalling.Instance;
        var @params = funcCalling.Params.ToArray();

        if (method == ParsableMethods.StringIndexing)
        {
            Expression @string = instance!;
            Expression index = @params[0];

            Expression indexPlusOne = Expression.MakeBinary(ExpressionType.Add, index, Expression.Constant(1));

            CreateFunctionCallingString("SUBSTR", [@string, indexPlusOne, indexPlusOne]);
            return funcCalling;
        }
        else if (method.Name == "Exists")
        {
            throw new NotImplementedException();
        }
        else
        {
            throw new NotSupportedException();
        }

        void CreateFunctionCallingString(string name, params Expression[] @params)
        {
            sb.Append(name.ToUpper());
            sb.Append('(');

            for (int i = 0; i < @params.Length; i++)
            {
                Expression param = @params[i];

                Visit(param);

                if (i < @params.Length - 1)
                    sb.Append(", ");
            }

            sb.Append(')');
        }
    }

    protected internal override Expression VisitJoin(JoinExpression join)
    {

        this.VisitSource(join.Left);

        this.AppendNewLine(Identation.Same);

        switch (join.JoinType)
        {
            case JoinType.CrossJoin:
                sb.Append("CROSS JOIN ");
                break;

            case JoinType.InnerJoin:
                sb.Append("INNER JOIN ");
                break;

            case JoinType.CrossApply:
                sb.Append("CROSS APPLY ");
                break;
        }

        this.VisitSource(join.Right);

        if (join.Condition != null)
        {
            this.AppendNewLine(Identation.Inner);

            sb.Append("ON ");
            this.Visit(join.Condition);
            this.AppendNewLine(Identation.Outer);
        }

        return join;
    }



    private string GetTableName(Type elementType)
    {
        var dbSetProperty = _context
            .GetType()
            .GetProperties()
            .Single(p => p.PropertyType == typeof(DbSet<>).MakeGenericType(elementType));

        object? dbSet = dbSetProperty.GetValue(_context)
            ?? throw new InvalidOperationException("dbSet is null.");

        var tableInfo = dbSet
            .GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
            .Single(f => f.Name == nameof(DbSet<object>._tableInfo))
            .GetValue(dbSet)
                ?? throw new InvalidOperationException("tableInfo is null.");

        string tableName = ((TableInfo)tableInfo).Name;

        return tableName;
    }
}
