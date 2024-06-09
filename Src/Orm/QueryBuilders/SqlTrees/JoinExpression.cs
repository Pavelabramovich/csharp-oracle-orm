using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace OracleOrm;

internal enum JoinType
{

    CrossJoin,

    InnerJoin,

    CrossApply,

}

public class JoinExpression : SqlExpression
{

    JoinType joinType;

    Expression left;

    Expression right;

    Expression condition;

    internal JoinExpression(Type type, JoinType joinType, Expression left, Expression right, Expression condition)

        : base(SqlExpressionType.Join, type)
    {

        this.joinType = joinType;

        this.left = left;

        this.right = right;

        this.condition = condition;

    }

    internal JoinType Join
    {

        get { return this.joinType; }

    }

    internal Expression Left
    {

        get { return this.left; }

    }

    internal Expression Right
    {

        get { return this.right; }

    }

    internal new Expression Condition
    {

        get { return this.condition; }

    }

}
