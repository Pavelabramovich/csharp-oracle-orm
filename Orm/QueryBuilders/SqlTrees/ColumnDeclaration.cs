﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace OracleOrm;

public class ColumnDeclaration
{
    string name;
    Expression expression;

    internal ColumnDeclaration(string name, Expression expression)
    {
        this.name = name;
        this.expression = expression;
    }

    internal string Name => name;
    internal Expression Expression => expression;
}
