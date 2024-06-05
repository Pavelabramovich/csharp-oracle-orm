using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


namespace OracleOrm;


public class TableExpression : Expression
{
    public Type ElementsType { get; set; }

    string alias;
    string name;

    internal TableExpression(Type type, string alias, string name)
        : base((ExpressionType)DbExpressionType.Table, type)
    {
        this.alias = alias;
        this.name = name;


        ElementsType = TypeSystem.GetElementType(type); 
    }

    internal string Alias => alias;
    internal string Name => name;
}