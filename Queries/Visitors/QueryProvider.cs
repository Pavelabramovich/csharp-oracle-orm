using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace OracleOrm.Queries.Visitors;


public class Query<T> : IQueryable<T>, IQueryable, IEnumerable<T>, IEnumerable, IOrderedQueryable<T>, IOrderedQueryable
{
    QueryProvider provider;
    Expression expression;

    public Query(QueryProvider provider)
    {
        if (provider == null)
            throw new ArgumentNullException("provider");

        this.provider = provider;
        expression = Expression.Constant(this);
    }

    public Query(QueryProvider provider, Expression expression)
    {
        if (provider == null)
            throw new ArgumentNullException("provider");

        if (expression == null)
            throw new ArgumentNullException("expression");

        if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
            throw new ArgumentOutOfRangeException("expression");

        this.provider = provider;
        this.expression = expression;
    }

    Expression IQueryable.Expression
    {
        get { return expression; }
    }

    Type IQueryable.ElementType
    {
        get { return typeof(T); }
    }

    IQueryProvider IQueryable.Provider
    {
        get { return provider; }
    }

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)provider.Execute(expression)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)provider.Execute(expression)).GetEnumerator();
    }

    public override string ToString()
    {
        return provider.GetQueryText(expression);
    }
}



public abstract class QueryProvider : IQueryProvider
{
    protected QueryProvider()
    {
    }

    IQueryable<S> IQueryProvider.CreateQuery<S>(Expression expression)
    {
        return new Query<S>(this, expression);
    }

    IQueryable IQueryProvider.CreateQuery(Expression expression)
    {
        Type elementType = null; // TypeSystem.GetElementType(expression.Type);

        try
        {
            return (IQueryable)Activator.CreateInstance(typeof(Query<>).MakeGenericType(elementType), new object[] { this, expression });
        }
        catch (TargetInvocationException tie)
        {
            throw tie.InnerException;
        }
    }

    S IQueryProvider.Execute<S>(Expression expression)
    {
        return (S)Execute(expression);
    }

    object IQueryProvider.Execute(Expression expression)
    {
        return Execute(expression);
    }

    public abstract string GetQueryText(Expression expression);

    public abstract object Execute(Expression expression);
}