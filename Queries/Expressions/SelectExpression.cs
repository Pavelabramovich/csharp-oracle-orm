using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


namespace OracleOrm.Queries.Expressions;


internal class SelectExpression : Expression
{
    string alias;
    ReadOnlyCollection<ColumnDeclaration> columns;
    Expression from;
    Expression where;


    internal SelectExpression(Type type, string alias, IEnumerable<ColumnDeclaration> columns, Expression from, Expression where)
        : base((ExpressionType)DbExpressionType.Select, type)
    {
        this.alias = alias;
        this.columns = columns as ReadOnlyCollection<ColumnDeclaration>;

        if (this.columns == null)
        {
            this.columns = new List<ColumnDeclaration>(columns).AsReadOnly();
        }

        this.from = from;
        this.where = where;
    }


    internal string Alias => alias;
    internal ReadOnlyCollection<ColumnDeclaration> Columns => columns;
    internal Expression From => from;
    internal Expression Where => where;
}
