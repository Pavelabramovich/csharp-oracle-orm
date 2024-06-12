using System.Linq.Expressions;


namespace OracleOrm;


public class ProjectionExpression : SqlExpression
{
    public SelectExpression Source { get; }
    public Expression Projector { get; }

   
    internal ProjectionExpression(SelectExpression source, Expression projector)
        : base(projector.Type)
    {
        Source = source;
        Projector = projector;
    }

    protected internal override Expression Accept(SqlExpressionVisitor sqlVisitor)
    {
        return sqlVisitor.VisitProjection(this);
    }

    public override string ToString()
    {
        return $"({Source}) => {Projector}";
    }
}