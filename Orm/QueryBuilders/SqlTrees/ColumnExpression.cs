using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


namespace OracleOrm;


public class ColumnExpression : Expression
{
    string alias;
    string name;

    int ordinal;


    internal ColumnExpression(Type type, string alias, string name, int ordinal)
        : base((ExpressionType)DbExpressionType.Column, type)
    {
        this.alias = alias;
        this.name = name;
        this.ordinal = ordinal;
    }

    internal string Alias => alias;
    internal string Name => name;

    internal int Ordinal => ordinal;
}
