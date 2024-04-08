﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace OracleOrm.Core;


public class QueryBuilder: ExpressionVisitor
{
    private StringBuilder _sb;

    internal QueryBuilder()
    {
        _sb = new StringBuilder();
    }

    public string Compile(Expression expression)
    {
        _sb = new StringBuilder();

        Visit(expression);
        return _sb.ToString();
    }

    private static Expression StripQuotes(Expression e)
    {
        while (e.NodeType == ExpressionType.Quote)
        {
            e = ((UnaryExpression)e).Operand;
        }

        return e;
    }

    protected override Expression VisitMethodCall(MethodCallExpression callExpression)
    {
        if (callExpression.Method.DeclaringType == typeof(Queryable) && callExpression.Method.Name == "Where")
        {
            _sb.Append("SELECT * FROM (");

            Visit(callExpression.Arguments[0]);

            _sb.Append(") AS T WHERE ");

            LambdaExpression lambda = (LambdaExpression)StripQuotes(callExpression.Arguments[1]);

            Visit(lambda.Body);

            return callExpression;
        }

        throw new NotSupportedException(string.Format("The method '{0}' is not supported", callExpression.Method.Name));
    }

    protected override Expression VisitUnary(UnaryExpression u)
    {
        switch (u.NodeType)
        {
            case ExpressionType.Not:
                _sb.Append(" NOT ");
                Visit(u.Operand);
                break;

            default:
                throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
        }

        return u;
    }

    protected override Expression VisitBinary(BinaryExpression b)
    {
        _sb.Append("(");

        Visit(b.Left);

        switch (b.NodeType)
        {
            case ExpressionType.And:
                _sb.Append(" AND ");
                break;

            case ExpressionType.Or:
                _sb.Append(" OR");
                break;

            case ExpressionType.Equal:
                _sb.Append(" = ");
                break;

            case ExpressionType.NotEqual:
                _sb.Append(" <> ");
                break;

            case ExpressionType.LessThan:
                _sb.Append(" < ");
                break;

            case ExpressionType.LessThanOrEqual:
                _sb.Append(" <= ");
                break;

            case ExpressionType.GreaterThan:
                _sb.Append(" > ");
                break;

            case ExpressionType.GreaterThanOrEqual:
                _sb.Append(" >= ");
                break;

            default:
                throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));
        }

        Visit(b.Right);

        _sb.Append(")");

        return b;
    }

    protected override Expression VisitConstant(ConstantExpression c)
    {
        if (c.Value is IQueryable queryable)
        {
            _sb.Append("SELECT * FROM ");
            _sb.Append(queryable.ElementType.Name);
        }
        else if (c.Value == null)
        {
            _sb.Append("NULL");
        }
        else
        {
            switch (Type.GetTypeCode(c.Value.GetType()))
            {
                case TypeCode.Boolean:
                    _sb.Append(((bool)c.Value) ? 1 : 0);
                    break;

                case TypeCode.String:
                    _sb.Append("'");
                    _sb.Append(c.Value);
                    _sb.Append("'");
                    break;

                case TypeCode.Object:
                    throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", c.Value));

                default:
                    _sb.Append(c.Value);
                    break;
            }
        }

        return c;
    }

    protected override Expression VisitMember(MemberExpression m)
    {
        if (m.Expression != null && m.Expression.NodeType == ExpressionType.Parameter)
        {
            _sb.Append(m.Member.Name);
            return m;
        }

        throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
    }
}