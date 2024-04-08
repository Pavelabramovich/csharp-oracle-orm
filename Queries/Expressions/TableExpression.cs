﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


namespace OracleOrm.Queries.Expressions;


internal class TableExpression : Expression
{
    string alias;
    string name;

    internal TableExpression(Type type, string alias, string name)
        : base((ExpressionType)DbExpressionType.Table, type)
    {
        this.alias = alias;
        this.name = name;
    }

    internal string Alias => alias;
    internal string Name => name;
}