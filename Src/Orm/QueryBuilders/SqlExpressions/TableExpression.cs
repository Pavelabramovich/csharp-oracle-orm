using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


namespace OracleOrm;


public class TableExpression : SqlExpression
{
    public string Name { get; }
    public string Alias { get; }

    public Type ElementsType { get; }


    internal TableExpression(Type type, string alias, string name)
        : base(type)
    {
        Alias = alias;
        Name = name;

        ElementsType = TypeSystem.GetElementType(type); 
    }

    protected internal override Expression Accept(SqlExpressionVisitor sqlVisitor)
    {
        return sqlVisitor.VisitTable(this);
    }

    public override string ToString()
    {
        return $"{Name} {Alias}";
    }
}