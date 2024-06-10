using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


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