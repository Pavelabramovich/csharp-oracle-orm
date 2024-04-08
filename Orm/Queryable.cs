using System.Collections;
using System.Dynamic;
using System.Linq.Expressions;


namespace OracleOrm;


public class Queryable<T> : IQueryable<T>
{
    public Type ElementType { get; }
    public Expression Expression { get; }
    public IQueryProvider Provider { get; }


    public Queryable(IQueryProvider queryProvider, Expression expression)
    {
        ArgumentNullException
            .ThrowIfNull(queryProvider, nameof(queryProvider));

        ArgumentNullException
            .ThrowIfNull(expression, nameof(expression));

        if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
            throw new ArgumentOutOfRangeException(nameof(expression));

        this.Expression = expression;
        this.Provider = queryProvider;
        this.ElementType = typeof(T);
    }

    public Queryable(IQueryProvider queryProvider)
    {
        ArgumentNullException
            .ThrowIfNull(queryProvider, nameof(queryProvider));

        this.Expression = Expression.Constant(this);
        this.Provider = queryProvider;
        this.ElementType = typeof(T);
    }


    public IEnumerator<T> GetEnumerator()
    {
        var res = Provider.Execute(Expression);

        if (res is IEnumerable<ExpandoObject> objects)
        {
            return objects.Select(o => QueryMapper.Map<T>(o)).GetEnumerator();
        }
        else
        {
            throw new NotSupportedException();
        }
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
