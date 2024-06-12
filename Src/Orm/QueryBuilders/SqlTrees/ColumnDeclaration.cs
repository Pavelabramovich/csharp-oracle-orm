using System.Linq.Expressions;


namespace OracleOrm;


public class ColumnDeclaration
{
    public string Name { get; }
    public Expression Expression { get; }


    public ColumnDeclaration(string name, ColumnExpression expression)
    {
        Name = name;
        Expression = expression;
    }

    public override string ToString()
    {
        return $"{Expression} as {Name}";
    }
}
