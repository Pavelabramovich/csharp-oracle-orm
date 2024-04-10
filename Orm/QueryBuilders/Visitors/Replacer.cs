using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace OracleOrm;

internal class Replacer : DbExpressionVisitor
{
    Expression searchFor;
    Expression replaceWith;

    internal Expression Replace(Expression expression, Expression searchFor, Expression replaceWith)
    {
        this.searchFor = searchFor;
        this.replaceWith = replaceWith;

        return this.Visit(expression);
    }

    public override Expression Visit(Expression? exp)
    {
        if (exp == this.searchFor)
        {
            return this.replaceWith;
        }

        return base.Visit(exp);
    }
}
