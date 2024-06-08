using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


namespace OracleOrm;


public class TableExpression : Expression
{
    public string Name { get; }
    public string Alias { get; }

    public Type ElementsType { get; }


    //public TableExpression(Type type, string alias, string name)
    //{
    //    Alias = alias;
    //    Name = name;

    //}


    internal TableExpression(Type type, string alias, string name)
        : base((ExpressionType)DbExpressionType.Table, type)
    {
        Alias = alias;
        Name = name;

        ElementsType = TypeSystem.GetElementType(type); 
    }
}