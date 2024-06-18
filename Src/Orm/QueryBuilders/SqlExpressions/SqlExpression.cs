using System.Linq.Expressions;


namespace OracleOrm;


public abstract partial class SqlExpression : Expression
{
    internal SqlExpression(Type type)
    {
        Type = type;
    }

    public override Type Type { get; }
    public override sealed ExpressionType NodeType => ExpressionType.Extension;

    protected override sealed Expression Accept(ExpressionVisitor visitor)
    {
        return visitor is SqlExpressionVisitor sqlVisitor
            ? Accept(sqlVisitor) 
            : base.Accept(visitor);
    }

    protected internal virtual Expression Accept(SqlExpressionVisitor sqlVisitor)
    {
        return base.Accept(sqlVisitor);
    }
}


//public partial class SqlExpression
//{

//}