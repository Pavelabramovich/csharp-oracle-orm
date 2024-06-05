using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleOrm.Dev;

internal class DevContext : OracleDbContext
{
    internal override string Protocol { get; } = "TCP";
    internal override string Host { get; } = "localhost";
    internal override long Port { get; } = 1521;
    internal override string ServiceName { get; } = "orcl";
    internal override string SchemaName { get; } = "DEV";
    internal override string SchemaPassword { get; } = "pass1pass";


    public DbSet<Student> Students { get; init; }
    public DbSet<Group> Groups { get; init; }
}
