using System.Data.Common;
using System.Dynamic;

namespace OracleOrm;


public static class ReaderExtension
{
    public static IEnumerable<ExpandoObject> ReadAll(this DbDataReader reader)
    {
        while (reader.Read())
        {
            ExpandoObject dbRow = new();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string colName = reader.GetName(i);
                object value =  reader.GetValue(i);

                (dbRow as IDictionary<string, object?>)[colName] = value;
            }

            yield return dbRow;
        }
    }
}
