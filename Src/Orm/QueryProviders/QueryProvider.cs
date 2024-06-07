using System.Linq.Expressions;


namespace OracleOrm;


public abstract class QueryProvider : IQueryProvider
{
    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new Queryable<TElement>(this, expression);
    }
    public IQueryable CreateQuery(Expression expression)
    {
        Type elementType = expression.Type.GetElementType()
            ?? throw new InvalidOperationException("Type of expression is not enumerable.");
  
        object? query = Activator.CreateInstance(typeof(Queryable<>).MakeGenericType(elementType), [this, expression]);

        return (IQueryable)query!;
    }

    public TResult Execute<TResult>(Expression expression) 
    {
        return (TResult)Execute(expression)!;
    }
    public abstract object? Execute(Expression expression);
}