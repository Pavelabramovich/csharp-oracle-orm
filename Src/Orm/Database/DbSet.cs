//using Microsoft.EntityFrameworkCore.Metadata;
//using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.CompilerServices;


namespace OracleOrm;


internal record TableInfo(Type EntityType, string Name, List<string> Fields);

public class DbSet<T> : Queryable<T>
{
    internal static readonly Dictionary<string, (string, IEnumerable<string>)> s_dataTypeMapping = new()
    { 
        [typeof(string).Name] = ("VARCHAR2", ["VARCHAR2(64)", "NOT NULL"]),
        [typeof(int).Name] = ("NUMBER", ["INT", "NOT NULL"]),
        [typeof(double).Name] = ("NUMBER", ["NUMBER", "NOT NULL"]),
        [typeof(decimal).Name] = ("NUMBER", ["NUMBER", "NOT NULL"]),
        [typeof(bool).Name] = ("NUMBER", ["NUMBER", "NOT NULL"])
    };

    internal TableInfo? _tableInfo;


    private OracleDbContext _context;


    public DbSet(OracleDbContext context) 
        : base(new OracleQueryProvider(context))
    { 
        _context = context;
        CheckTableCreation();
    }

    public void Clear()
    {
        string tableName = _tableInfo.Name;

        string clearSql = $"DELETE FROM {tableName.ToUpper()}";

        _context.ExecuteQuery(clearSql);
    }

    public void Delete(Expression<Func<T, bool>> predicate)
    {
        var qf = new QueryFormatter(_context, monkeyPatch: true);

        var whereClause = qf.Format(predicate);

        string deleteSql = $"""
            DELETE FROM {_tableInfo.Name}
             WHERE {whereClause}
            """;

    //    Console.WriteLine(deleteSql + '\n');

        _context.ExecuteQuery(deleteSql);
    }


    public void Update(string property, object newValue, Expression<Func<T, bool>> predicate)
    {
        Update([(property, newValue)], predicate);
    }

    public void Update(IEnumerable<(string, object)> setProperties, Expression<Func<T, bool>> predicate)
    {
        var qf = new QueryFormatter(_context, monkeyPatch: true);

        string whereClause = qf.Format(predicate);

        IEnumerable<string> setClauses = setProperties.Select(item => $"{item.Item1} = {new QueryFormatter(_context, true).Format(Expression.Constant(item.Item2))}");
        string setClause = string.Join(",\n       ", setClauses);

        string updateSql = $"""
            UPDATE {_tableInfo.Name}
               SET {setClause}
             WHERE {whereClause}
            """;

    //    Console.WriteLine(updateSql + '\n');

        _context.ExecuteQuery(updateSql);
    }

    public bool Exists(Func<T, bool> predicate) 
    {
        return true;
    }

    public bool NotExists(Func<T, bool> predicate)
    {
        return true;
    }


    private void CheckTableCreation()
    {
        var props = typeof(T).GetProperties();

        if (props.Count(p => p.Name == "Id") is int count and not 1)
            throw new InvalidOperationException($"{typeof(T).Name} type contains {count} of Id field, must be one.");

        string tableName = CaseConverter.Plurarize(typeof(T).Name);

        //var tableAlreadyExists = _context
        //    .ExecuteQuery<bool>($"SELECT COUNT(*) FROM dba_tables WHERE owner = '{_context.SchemaName}' AND table_name = '{tableName.ToUpper()}'")
        //    .Single();

       // var t = _context
       //  .ExecuteQuery<bool>($"SELECT COUNT(*) FROM dba_tables WHERE owner = '{_context.SchemaName}' AND table_name = '{tableName.ToUpper()}'")
       //y .ToList();

        var tableAlreadyExists = _context
          .ExecuteQuery<bool>($"SELECT COUNT(*) FROM dba_tables WHERE owner = '{_context.SchemaName}' AND table_name = '{tableName.ToUpper()}'")
          .Single();

        List<string> fields = [];

        if (tableAlreadyExists)
        {
            string tableColumnsQuery = $"""
                SELECT c.column_name,
                       c.data_type,
                       c.data_length,
                       c.data_scale,
                       c.nullable
                  FROM all_tab_columns c
                 WHERE c.table_name = '{tableName.ToUpper()}'
                   AND c.owner = '{_context.SchemaName}'
                """;

            var columns = _context.ExecuteQuery(tableColumnsQuery).ToArray();

            if (columns.Length != props.Length)
            {
                throw new InvalidOperationException($"""
                    The given type {typeof(T).Name} and the table {tableName} differ in the number of fields. Maybe you forgot to do the migration?
                    """);
            }

            foreach (dynamic column in columns)
            {
                string oracleType = column.DataType;

                string cSharpType = props
                    .Single(p => p.Name == CaseConverter.ToPascalCase(column.ColumnName))
                    .PropertyType.Name;

                string columnName = column.ColumnName;

                if (s_dataTypeMapping.TryGetValue(cSharpType, out (string, IEnumerable<string>) oraclePair))
                {
                    if (oracleType != oraclePair.Item1)
                        throw new InvalidOperationException($"The given type {cSharpType} and the table type {oracleType} is not paired.");
                }
                else
                {
                    throw new InvalidOperationException($"{cSharpType} is not supported as column type.");
                }

                fields.Add(GetFieldCreation(cSharpType, columnName));
            }

            _tableInfo = new TableInfo(EntityType: typeof(T), tableName, fields);
        }
        else
        {
            foreach (var property in props)
            {
                string columnName = property.Name;
                string cSharpType = property.PropertyType.Name;

                fields.Add(GetFieldCreation(cSharpType, columnName));
            }

            string tableCreationSql = $"CREATE TABLE {tableName} (\n{string.Join(",\n", fields.Select(s => $"\t{s}"))}\n)";

            _tableInfo = new TableInfo(EntityType: typeof(T), tableName, fields);

            _context.ExecuteQuery(tableCreationSql);
        }
    }


    private string GetFieldCreation(string cSharpType, string columnName)
    {
        if (s_dataTypeMapping.TryGetValue(cSharpType, out (string, IEnumerable<string>) oraclePair))
        {
            if (columnName.Equals("id", StringComparison.CurrentCultureIgnoreCase))
            {
                List<string> dataTypeModifiers = oraclePair.Item2.ToList();
                dataTypeModifiers.Insert(1, "GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY");

                oraclePair.Item2 = dataTypeModifiers;
            }

            return $"{CaseConverter.ToSnakeCase(columnName)} {string.Join(' ', oraclePair.Item2)}";
        }
        else
        {
            throw new InvalidOperationException($"{cSharpType} is not supported as column type.");
        }
    }
}
