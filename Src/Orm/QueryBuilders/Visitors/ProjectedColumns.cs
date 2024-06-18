using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;


namespace OracleOrm;


public class ProjectedColumns
{
    public Expression Projector { get; }
    public ReadOnlyCollection<ColumnDeclaration> Columns { get; }


    public ProjectedColumns(Expression projector, ReadOnlyCollection<ColumnDeclaration> columns)
    {
        Projector = projector;
        Columns = columns;
    }
}


public class ColumnProjector : SqlExpressionVisitor
{
    private readonly Nominator _nominator;
    private readonly Dictionary<ColumnExpression, ColumnExpression> _columnsMap;
    private readonly List<ColumnDeclaration> _columns;
    private readonly HashSet<string> _columnNames;

    private HashSet<Expression> _candidates;

    private string? _existingAlias;
    private string? _newAlias;
    private int _iColumn;


    public ColumnProjector(Func<Expression, bool> isColumn)
    {
        _nominator = new Nominator(isColumn);
        _columnsMap = [];
        _columns = [];
        _columnNames = [];
        _candidates = [];

        _existingAlias = null;
        _newAlias = null;
        _iColumn = 0;
    }

    public ProjectedColumns ProjectColumns(Expression expression, string newAlias, string existingAlias)
    {
        _columnsMap.Clear();
        _columns.Clear();
        _columnNames.Clear();

        _newAlias = newAlias;
        _existingAlias = existingAlias;
        _candidates = _nominator.Nominate(expression);

        return new ProjectedColumns(Visit(expression), _columns.AsReadOnly());
    }


    public override Expression Visit(Expression? expression)
    {
        ArgumentNullException
            .ThrowIfNull(expression, nameof(expression));   

        if (_candidates.Contains(expression))
        {
            if (expression is ColumnExpression column)
            { 
                if (_columnsMap.TryGetValue(column, out ColumnExpression? mapped))
                {
                    return mapped;
                }

                if (_existingAlias == column.Alias)
                {
                    int ordinal = _columns.Count;
                    string columnName = GetUniqueColumnName(column.Name);
                    _columns.Add(new ColumnDeclaration(columnName, column));

                    mapped = new ColumnExpression(column.Type, _newAlias!, columnName, ordinal);
                    _columnsMap[column] = mapped;
                    _columnNames.Add(columnName);

                    return mapped;
                }

                // must be referring to outer scope
                return column;
            }
            else
            {
                string columnName = GetNextColumnName();
                int ordinal = _columns.Count;

                _columns.Add(new ColumnDeclaration(columnName, expression));

                return new ColumnExpression(expression.Type, _newAlias!, columnName, ordinal);
            }
        }
        else
        {
            return base.Visit(expression);
        }
    }


    private bool IsColumnNameInUse(string name)
    {
        return _columnNames.Contains(name);
    }

    private string GetUniqueColumnName(string name)
    {
        string baseName = name;
        int suffix = 1;

        while (IsColumnNameInUse(name))
        {
            name = baseName + (suffix++);
        }

        return name;
    }

    private string GetNextColumnName()
    {
        return GetUniqueColumnName($"c{_iColumn++}");
    }


    private class Nominator : SqlExpressionVisitor
    { 
        private readonly Func<Expression, bool> _isColumn;
        private readonly HashSet<Expression> _candidates;

        private bool _isBlocked;
        

        public Nominator(Func<Expression, bool> isColumn)
        {
            _isColumn = isColumn;
            _candidates = [];

            _isBlocked = false;
        }

        public HashSet<Expression> Nominate(Expression expression)
        {
            _candidates.Clear();
            _isBlocked = false;

            Visit(expression);

            return [.. _candidates];
        }

        [return: NotNullIfNotNull(nameof(expression))]
        public override Expression? Visit(Expression? expression)
        {
            if (expression is null)
                return null;

            if (expression is FunctionCallingExpression funcCalling)
            {
                /// Simplify?
                Visit(funcCalling.Instance);

                foreach (Expression p in funcCalling.Params)
                {
                    Visit(p);
                }

                _candidates.Add(expression);
            }
            else
            {
                bool saveIsBlocked = _isBlocked;
                _isBlocked = false;

                base.Visit(expression);

                if (!_isBlocked)
                {
                    if (_isColumn(expression))
                    {
                        _candidates.Add(expression);
                    }
                    else
                    {
                        _isBlocked = true;
                    }
                }

                _isBlocked |= saveIsBlocked;
            }

            return expression;
        }
    }
}
