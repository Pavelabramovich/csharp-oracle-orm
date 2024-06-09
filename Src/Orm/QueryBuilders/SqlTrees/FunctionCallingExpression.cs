using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace OracleOrm;


public class FunctionCallingExpression : SqlExpression
{
    public MethodInfo Method { get; }
    public Expression? Instance { get; }
    public IEnumerable<Expression> Params { get; }


    public FunctionCallingExpression(MethodInfo method, Expression? instance, IEnumerable<Expression> @params)
        : base(SqlExpressionType.FunctionCalling, method.ReturnType)
    {
        this.Method = method;
        this.Instance = instance;
        this.Params = @params;
    }
}
