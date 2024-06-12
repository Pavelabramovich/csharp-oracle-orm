using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OracleOrm;

public class SubQueryExpression : FunctionCallingExpression
{
    public string Sql { get; set; }

    internal SubQueryExpression(MethodInfo method, Expression? instance, IEnumerable<Expression> @params, string sql)
        : base(method, instance, @params)
    {
        this.Sql = sql;
    }

    protected internal override Expression Accept(SqlExpressionVisitor sqlVisitor)
    {
        return sqlVisitor.VisitSubQuery(this);
    }

    public override string ToString()
    {
        return $"rawSql({Sql})";
    }
}
