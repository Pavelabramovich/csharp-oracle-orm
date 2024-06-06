using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleOrm.Dev;

internal class DevContext : OracleDbContext
{
    public DbSet<Student> Students { get; init; }
    public DbSet<Group> Groups { get; init; }


    protected override OracleConnectionSettings ConnectionSettings
    {
        get => new(Protocol: "TCP", Host: "localhost", Port: 1521, ServiceName: "orcl", SchemaName: "DEV", SchemaPassword: "pass1pass");
    }
}
