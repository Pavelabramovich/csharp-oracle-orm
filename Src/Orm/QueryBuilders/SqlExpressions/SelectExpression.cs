using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


namespace OracleOrm;


public class SelectExpression : SqlExpression
{
    public string Alias { get; }

    public ReadOnlyCollection<ColumnDeclaration> Columns { get; }
    public Expression From { get; }
    public Expression? Where { get; }


    internal SelectExpression(Type type, string alias, IEnumerable<ColumnDeclaration> columns, Expression from, Expression where)
        : base(type)
    {
        Alias = alias;
        Columns = columns.ToArray().AsReadOnly();
        From = from;
        Where = where;
    }

    protected internal override Expression Accept(SqlExpressionVisitor sqlVisitor)
    {
        return sqlVisitor.VisitSelect(this);
    }

    public override string ToString()
    {
        return $"select ({string.Join(", ", Columns)}) from ({From})" + (Where is null ? "" : $" where ({Where})");
    }
}
