//using OracleOrm.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


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
        DbDataReader reader;
        T current;

        Func<ProjectionRow, T> projector;
        IQueryProvider provider;

        internal Enumerator(DbDataReader reader, Func<ProjectionRow, T> projector, IQueryProvider provider)
        {
            this.reader = reader;
            this.projector = projector;
            this.provider = provider;
        }

        public override object GetValue(int index)
        {
            if (index >= 0)
            {
                if (reader.IsDBNull(index))
                {
                    return null;
                }
                else
                {
                    return reader.GetValue(index);
                }
            }

            throw new IndexOutOfRangeException();
        }

        public T Current
        {
            get { return current; }
        }

        object IEnumerator.Current
        {
            get { return current; }
        }

        public bool MoveNext()
        {
            if (reader.Read())
            {
                current = projector(this);

                return true;
            }

            return false;
        }

        public void Reset()
        { }

        public void Dispose()
        {
            reader.Dispose();
        }

        public override IEnumerable<E> ExecuteSubQuery<E>(LambdaExpression query)
        {
            ProjectionExpression projection = (ProjectionExpression)new Replacer().Replace(query.Body, query.Parameters[0], Expression.Constant(this));

            projection = (ProjectionExpression)LocalVariablesEvaluater.Evaluate(projection, CanEvaluateLocally);
            IEnumerable<E> result = (IEnumerable<E>)this.provider.Execute(projection);
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
                expression.NodeType.IsDbExpression())
            {
                return false;
            }

            return true;
        }
    }
}

public static class ExpressionTypeExtension
{
    public static bool IsDbExpression(this ExpressionType expressionType)
    {
        return (int)expressionType >= 0;
    }
}