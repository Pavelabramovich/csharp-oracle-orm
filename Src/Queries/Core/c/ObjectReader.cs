using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OracleOrm.Queries.Core.c;


internal class ObjectReader<T> : IEnumerable<T>, IEnumerable where T : class, new()
{
    Enumerator enumerator;

    internal ObjectReader(DbDataReader reader)
    {
        enumerator = new Enumerator(reader);
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

    class Enumerator : IEnumerator<T>, IEnumerator, IDisposable
    {
        DbDataReader reader;
        PropertyInfo[] fields;
        int[] fieldLookup;
        T current;

        internal Enumerator(DbDataReader reader)
        {
            this.reader = reader;
            fields = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
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
                if (fieldLookup == null)
                {
                    InitFieldLookup();
                }

                T instance = new T();

                for (int i = 0, n = fields.Length; i < n; i++)
                {
                    int index = fieldLookup[i];

                    if (index >= 0)
                    {
                        PropertyInfo fi = fields[i];

                        if (reader.IsDBNull(index))
                        {
                            fi.SetValue(instance, null);
                        }
                        else
                        {
                            object value = Convert.ChangeType(reader.GetValue(index), fi.PropertyType);

                            fi.SetValue(instance, value);
                        }
                    }
                }

                current = instance;

                return true;
            }

            return false;
        }

        public void Reset()
        {
        }

        public void Dispose()
        {
            reader.Dispose();
        }

        private void InitFieldLookup()
        {
            var map = new Dictionary<string, int>(new CaseStringComparer());

            for (int i = 0, n = reader.FieldCount; i < n; i++)
            {
                map.Add(reader.GetName(i), i);
            }

            fieldLookup = new int[fields.Length];

            for (int i = 0, n = fields.Length; i < n; i++)
            {
                int index;

                if (map.TryGetValue(fields[i].Name, out index))
                {
                    fieldLookup[i] = index;
                }
                else
                {
                    fieldLookup[i] = -1;
                }
            }
        }
    }
}