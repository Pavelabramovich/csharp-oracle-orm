using System.Data;
using System.Dynamic;
using System.Reflection;
using System.Data.OracleClient;


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
