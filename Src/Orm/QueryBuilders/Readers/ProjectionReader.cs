using System.Collections;
using System.Data.Common;
using System.Linq.Expressions;


namespace OracleOrm;


internal class ProjectionReader<T> : IEnumerable<T>, IEnumerable
{
    Enumerator enumerator;

    internal ProjectionReader(DbDataReader reader, Func<ProjectionRow, T> projector, IQueryProvider provider)
    {
        enumerator = new Enumerator(reader, projector, provider);
    }

    public IEnumerator<T> GetEnumerator()
    {
        Enumerator e = enumerator;

        if (e == null)
        {
            throw new InvalidOperationException("Cannot enumerate more than once");
        }

        enumerator = null;

        return e;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private class Enumerator : ProjectionRow, IEnumerator<T>, IEnumerator, IDisposable
    {
        private DbDataReader _reader;
        private T _current;

        private Func<ProjectionRow, T> _projector;
        private IQueryProvider _provider;

        internal Enumerator(DbDataReader reader, Func<ProjectionRow, T> projector, IQueryProvider provider)
        {
            _reader = reader;
            _projector = projector;
            _provider = provider;
        }

        public override object GetValue(int index)
        {
            if (index >= 0)
            {
                if (_reader.IsDBNull(index))
                {
                    return null;
                }
                else
                {
                    return _reader.GetValue(index);
                }
            }

            throw new IndexOutOfRangeException();
        }

        public T Current
        {
            get { return _current; }
        }

        object IEnumerator.Current => Current!;


        public bool MoveNext()
        {
            if (_reader.Read())
            {
                _current = _projector(this);

                return true;
            }

            return false;
        }

        public void Reset()
        { }

        public void Dispose()
        {
            _reader.Dispose();
        }

        public override IEnumerable<E> ExecuteSubQuery<E>(LambdaExpression query)
        {
            ProjectionExpression projection = (ProjectionExpression)new Replacer().Replace(query.Body, query.Parameters[0], Expression.Constant(this));

            projection = (ProjectionExpression)PretranslateEvaluator.Evaluate(projection, CanEvaluateLocally);
            IEnumerable<E> result = (IEnumerable<E>)_provider.Execute(projection);
            List<E> list = new List<E>(result);

            if (typeof(IQueryable<E>).IsAssignableFrom(query.Body.Type))
            {
                return list.AsQueryable();
            }

            return list;
        }

        private static bool CanEvaluateLocally(Expression expression)
        {
            if (expression.NodeType == ExpressionType.Parameter ||
                expression is SqlExpression)
            {
                return false;
            }

            return true;
        }
    }
}
