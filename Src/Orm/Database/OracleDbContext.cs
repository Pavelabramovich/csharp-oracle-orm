using System.Data;
using System.Dynamic;
using System.Reflection;
//using OracleOrm.Queries.Core;
using System.Data.OracleClient;
using System.Data.Common;


namespace OracleOrm;


public record OracleConnectionSettings(
    string Protocol, 
    string Host, 
    long Port, 
    string ServiceName, 
    string SchemaName, 
    string SchemaPassword)
{
    public string ConnectionString => $"""
        Data Source=
        (
            DESCRIPTION=
            (
                ADDRESS=(PROTOCOL={Protocol})
                (HOST={Host})
                (PORT={Port})
            )
            (CONNECT_DATA=(SERVICE_NAME={ServiceName}))
        );

        User Id={SchemaName};
        Password={SchemaPassword};
        """;
}


public abstract class OracleDbContext : IDisposable
{
    private bool _isDisposed;

    protected abstract OracleConnectionSettings ConnectionSettings { get; }

    public OracleConnection Connection { get; init; }
    public string SchemaName { get; init; }

    public OracleDbContext()
    {
        Connection = new OracleConnection(ConnectionSettings.ConnectionString);
        SchemaName = ConnectionSettings.SchemaName;

        InitSets();
    }

    private void InitSets()
    {
        var sets = GetType()
            .GetProperties()
            .Where(x => x.PropertyType is { IsConstructedGenericType: true } genProp && genProp.GetGenericTypeDefinition() == typeof(DbSet<>));

        foreach (var set in sets)
        {
            set.SetValue(this, CreateSet(set.PropertyType.GetGenericArguments()[0]));
        }
    }


    public IEnumerable<ExpandoObject> ExecuteQuery(string query)
    {
        using var command = Connection.CreateCommand();
        command.CommandText = query;

        if (command.Connection is null)
            throw new InvalidOperationException("Can not create command with valid connection.");

        if (command.Connection.State != ConnectionState.Open)
            command.Connection.Open();

        IEnumerable<ExpandoObject> dataRows;

        using var dataReader = command.ExecuteReader();


        dataRows = ReadData(dataReader);

        if (command.Connection.State == ConnectionState.Open)
            command.Connection.Close();

        return dataRows.Select(obj =>
        {
            var dict = (obj as IDictionary<string, object?>);

            IDictionary<string, object?> pascalCaseObject = new ExpandoObject();

            foreach (var (key, value) in dict)
            {
                string pascalCaseKey = CaseConverter.ToPascalCase(key);
                pascalCaseObject[pascalCaseKey] = value;
            }

            return (ExpandoObject)pascalCaseObject;
        });
    }
    public IEnumerable<T> ExecuteQuery<T>(string query)
    {
        var t = ExecuteQuery(query).ToList();

        return ExecuteQuery(query).Select(obj => QueryMapper.Map<T>(obj));
    }


    public async Task<IEnumerable<ExpandoObject>> ExecuteQueryAsync(string query)
    {
        await using var command = Connection.CreateCommand();
        command.CommandText = query;

        if (command.Connection is null)
            throw new InvalidOperationException("Can not create command with valid connection.");

        if (command.Connection.State != ConnectionState.Open)
            command.Connection.Open();

        using var dataReader = await command.ExecuteReaderAsync();
        var dataRow = ReadData(dataReader);

        if (command.Connection.State == ConnectionState.Open)
            command.Connection.Close();

        return dataRow;
    }
    public async Task<IEnumerable<T>> ExecuteQueryAsync<T>(string query)
    {
        var objects = await ExecuteQueryAsync(query);

        return objects.Select(obj => QueryMapper.Map<T>(obj));
    }


    public static IEnumerable<ExpandoObject> ReadData(DbDataReader reader)
    {
        if (!reader.HasRows)
            return [];

        var dataList = new List<ExpandoObject>();

        while (reader.Read())
        {
            ExpandoObject dbRow = new();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string colName = reader.GetName(i);
                object value = reader.GetValue(i);

                (dbRow as IDictionary<string, object?>)[colName] = value;
            }

            dataList.Add(dbRow);
        }

        return dataList;
    }


    private object? CreateSet(Type setPropertyType)
    {
        return typeof(OracleDbContext)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(m => m.Name == nameof(CreateSet) && m.GetParameters().Length == 0)
            .MakeGenericMethod(setPropertyType)
            .Invoke(this, []);
    }

    private DbSet<T> CreateSet<T>()
    {
        return new DbSet<T>(this);
    }

    public void Dispose()
    {
        Connection.Dispose();
    }
}
