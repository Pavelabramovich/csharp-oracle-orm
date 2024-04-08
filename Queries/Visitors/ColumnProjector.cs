using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using OracleOrm.Queries.Expressions;


namespace OracleOrm.Queries.Visitors;


internal sealed class ProjectedColumns
{
    Expression projector;
    ReadOnlyCollection<ColumnDeclaration> columns;


    internal ProjectedColumns(Expression projector, ReadOnlyCollection<ColumnDeclaration> columns)
    {
        this.projector = projector;
        this.columns = columns;
    }

    internal Expression Projector => projector;
    internal ReadOnlyCollection<ColumnDeclaration> Columns => columns;
}


internal class ColumnProjector : DbExpressionVisitor
{
    Nominator nominator;
    Dictionary<ColumnExpression, ColumnExpression> map;
    List<ColumnDeclaration> columns;

    HashSet<string> columnNames;
    HashSet<Expression> candidates;

    string existingAlias;
    string newAlias;
    int iColumn;


    internal ColumnProjector(Func<Expression, bool> fnCanBeColumn)
    {
        nominator = new Nominator(fnCanBeColumn);
    }

    internal ProjectedColumns ProjectColumns(Expression expression, string newAlias, string existingAlias)
    {
        map = new Dictionary<ColumnExpression, ColumnExpression>();
        columns = new List<ColumnDeclaration>();
        columnNames = new HashSet<string>();
        this.newAlias = newAlias;
        this.existingAlias = existingAlias;
        candidates = nominator.Nominate(expression);

        return new ProjectedColumns(Visit(expression), columns.AsReadOnly());
    }

    public override Expression Visit(Expression? expression)
    {
        if (candidates.Contains(expression))
        {
            if (expression.NodeType == (ExpressionType)DbExpressionType.Column)
            {
                ColumnExpression column = (ColumnExpression)expression;
                ColumnExpression mapped;

                if (map.TryGetValue(column, out mapped))
                {
                    return mapped;
                }

                if (existingAlias == column.Alias)
                {
                    int ordinal = columns.Count;
                    string columnName = GetUniqueColumnName(column.Name);

                    columns.Add(new ColumnDeclaration(columnName, column));

                    mapped = new ColumnExpression(column.Type, newAlias, columnName, ordinal);

                    map[column] = mapped;
                    columnNames.Add(columnName);

                    return mapped;
                }

                // must be referring to outer scope
                return column;
            }
            else
            {
                string columnName = GetNextColumnName();
                int ordinal = columns.Count;

                columns.Add(new ColumnDeclaration(columnName, expression));

                return new ColumnExpression(expression.Type, newAlias, columnName, ordinal);
            }
        }
        else
        {
            return base.Visit(expression);
        }
    }

    private bool IsColumnNameInUse(string name)
    {
        return columnNames.Contains(name);
    }


    private string GetUniqueColumnName(string name)
    {
        string baseName = name;
        int suffix = 1;

        while (IsColumnNameInUse(name))
        {
            name = baseName + suffix++;
        }

        return name;
    }

    private string GetNextColumnName()
    {
        return GetUniqueColumnName("c" + iColumn++);
    }


    class Nominator : DbExpressionVisitor
    {
        Func<Expression, bool> fnCanBeColumn;
        bool isBlocked;

        HashSet<Expression> candidates;


        internal Nominator(Func<Expression, bool> fnCanBeColumn)
        {
            this.fnCanBeColumn = fnCanBeColumn;
        }

        internal HashSet<Expression> Nominate(Expression expression)
        {
            candidates = new HashSet<Expression>();
            isBlocked = false;

            Visit(expression);

            return candidates;
        }

        public override Expression Visit(Expression expression)
        {
            if (expression != null)
            {
                bool saveIsBlocked = isBlocked;
                isBlocked = false;

                base.Visit(expression);

                if (!isBlocked)
                {
                    if (fnCanBeColumn(expression))
                    {
                        candidates.Add(expression);
                    }
                    else
                    {
                        isBlocked = true;
                    }
                }

                isBlocked |= saveIsBlocked;
            }

            return expression;
        }
    }
}