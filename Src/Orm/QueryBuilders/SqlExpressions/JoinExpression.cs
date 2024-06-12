using System.Linq.Expressions;


namespace OracleOrm;


public enum JoinType
{
    CrossJoin,
    InnerJoin,
    CrossApply,
}


public class JoinExpression : SqlExpression
{
    public JoinType JoinType { get; }

    public Expression Left { get; }
    public Expression Right { get; }
    public new Expression Condition { get; }


    internal JoinExpression(Type type, JoinType joinType, Expression left, Expression right, Expression condition)
        : base(type)
    {
        JoinType = joinType;
        Left = left;
        Right = right;
        Condition = condition;
    }

    protected internal override Expression Accept(SqlExpressionVisitor sqlVisitor)
    {
        return sqlVisitor.VisitJoin(this);
    }

    public override string ToString()
    {
        return $"{Left} join {Right} on {Condition}";
    }
}
