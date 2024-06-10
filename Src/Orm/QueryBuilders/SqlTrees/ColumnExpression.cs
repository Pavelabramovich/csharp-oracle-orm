
using System.Linq.Expressions;

namespace OracleOrm;


public class ColumnExpression : SqlExpression
{
    public string Alias { get; }
    public string Name { get; }

    public int Ordinal { get; }


    internal ColumnExpression(Type type, string alias, string name, int ordinal)
        : base(type)
    {
        Alias = alias;
        Name = name;
        Ordinal = ordinal;
    }

    protected internal override Expression Accept(SqlExpressionVisitor sqlVisitor)
    {
        return sqlVisitor.VisitColumn(this);
    }
}
