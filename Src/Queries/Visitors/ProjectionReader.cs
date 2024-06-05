using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace OracleOrm.Queries.Visitors;


internal class ProjectionReader<T> : IEnumerable<T>, IEnumerable
{
    Enumerator enumerator;

    internal ProjectionReader(DbDataReader reader, Func<ProjectionRow, T> projector)
    {
        enumerator = new Enumerator(reader, projector);
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

        internal Enumerator(DbDataReader reader, Func<ProjectionRow, T> projector)
        {
            this.reader = reader;
            this.projector = projector;
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
    }
}