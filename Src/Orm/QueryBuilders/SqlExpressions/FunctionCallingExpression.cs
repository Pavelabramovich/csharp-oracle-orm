using System.Linq.Expressions;
using System.Reflection;


namespace OracleOrm;


public class FunctionCallingExpression : SqlExpression
{
    public MethodInfo Method { get; }
    public Expression? Instance { get; }
    public IEnumerable<Expression> Params { get; }


    internal FunctionCallingExpression(MethodInfo method, Expression? instance, IEnumerable<Expression> @params)
        : base(method.ReturnType)
    {
        Method = method;
        Instance = instance;
        Params = @params;
    }

    protected internal override Expression Accept(SqlExpressionVisitor sqlVisitor)
    {
        return sqlVisitor.VisitFunctionCalling(this);
    }

    public override string ToString()
    {
        return $"{Instance}.{Method.Name}({string.Join(", ", Params)})";
    }
}
