//using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using OracleOrm.Dev;
using System.Text;
using OracleOrm.Queries.Core;
//using Oracle.ManagedDataAccess.Client;
using System.Data.OracleClient;
using System.Data.Common;


namespace OracleOrm;


public abstract class OracleDbContext //: DbContext
{
    internal abstract string Protocol { get; } 
    internal abstract string Host { get; } 
    internal abstract long Port { get; } 
    internal abstract string ServiceName { get; } 
    internal abstract string SchemaName { get; } 
    internal abstract string SchemaPassword { get; } 

    private string ConnectionString => $"""
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

    public OracleConnection Connection { get; }


    public OracleDbContext()
    {
        Connection = new OracleConnection(ConnectionString);

        var sets = GetType()
            .GetProperties()
            .Where(x => x.PropertyType is { IsConstructedGenericType: true } genProp && genProp.GetGenericTypeDefinition() == typeof(DbSet<>));

        foreach (var set in sets)
        {
            set.SetValue(this, CreateSet(set.PropertyType.GetGenericArguments()[0]));
        }

        
    }


    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //{
    //    optionsBuilder.UseOracle(this.ConnectionString);
    //}

    public IEnumerable<ExpandoObject> ExecuteQuery(string query)
    {
        //using (OracleConnection connection = new OracleConnection(connectionString))
        //{
        //    connection.Open();

        //    string sqlQuery = "SELECT * FROM your_table";
        //    using (OracleCommand command = new OracleCommand(sqlQuery, connection))
        //    {
        //        using (OracleDataReader reader = command.ExecuteReader())
        //        {
        //            while (reader.Read())
        //            {
        //                // Обработка результатов запроса
        //            }
        //        }
        //    }
        //}







      //  using var command = this.Database.GetDbConnection().CreateCommand();

     //   using var connection = new OracleConnection(ConnectionString);

        using var command = new OracleCommand(query, Connection);
        command.CommandText = query;

        if (command.Connection is null)
            throw new InvalidOperationException("Can not create command with valid connection.");

        if (command.Connection.State != ConnectionState.Open)
            command.Connection.Open();

        IEnumerable<ExpandoObject> dataRows;

        using var dataReader = command.ExecuteReader();

    //    List<object> data = new List<object>();







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
       // using var connection = new OracleConnection(ConnectionString);

        await using var command = new OracleCommand(query, Connection);
        command.CommandText = query;

        //await using var command = this.Database.GetDbConnection().CreateCommand();
        //command.CommandText = query;

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

            //foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(reader))
            //{
            //    object? obj = propertyDescriptor.GetValue(reader);
            //    string colName = propertyDescriptor.Name;

                
            //}

            dataList.Add(dbRow);
        }


        //foreach (var item in reader)
        //{
        //    ExpandoObject dbRow = new();

        //    foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(item))
        //    {
        //        object? obj = propertyDescriptor.GetValue(item);
        //        string colName = propertyDescriptor.Name;

        //        (dbRow as IDictionary<string, object?>)[colName] = obj;
        //    }

        //    dataList.Add(dbRow);
        //}

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

    ~OracleDbContext()
    {
        Connection.Dispose();
    }
}
