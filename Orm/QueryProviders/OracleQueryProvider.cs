using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace OracleOrm;


public class OracleQueryProvider : QueryProvider
{
    private readonly OracleDbContext _context;


    public OracleQueryProvider(OracleDbContext context)
    {
        _context = context;
    }

    public override object? Execute(Expression expression)
    {
        var sql = GetQueryString(expression);
        return _context.ExecuteQuery(sql);
    }

    public override string GetQueryString(Expression expression)
    {
        return new QueryBuilder(_context).Compile(expression);
    }
}
