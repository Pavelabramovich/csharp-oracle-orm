using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


namespace OracleOrm;


public abstract partial class SqlExpression : Expression
{
    private readonly Type _type;
    public SqlExpressionType SqlNodeType { get; }
    
    public SqlExpression(SqlExpressionType sqlExpressionType, Type type)
    {
        _type = type;
        SqlNodeType = sqlExpressionType;
    }

    public override Type Type => _type;
    public override sealed ExpressionType NodeType => ExpressionType.Extension;
}


public partial class SqlExpression
{

}