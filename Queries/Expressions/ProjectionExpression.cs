using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


namespace OracleOrm.Queries.Expressions;


internal class ProjectionExpression : Expression
{
    SelectExpression source;
    Expression projector;

    internal ProjectionExpression(SelectExpression source, Expression projector)
        : base((ExpressionType)DbExpressionType.Projection, projector.Type)
    {
        this.source = source;
        this.projector = projector;
    }


    internal SelectExpression Source => source;
    internal Expression Projector => projector;
}