using System.Linq.Expressions;
using System.Reflection;


namespace OracleOrm;


public class QueryBinder : ExpressionVisitor
{
    private readonly ColumnProjector _columnProjector;
    private readonly Dictionary<ParameterExpression, Expression> _parameterMap;

    private int _aliasCounter;

    private readonly OracleDbContext _context;


    public QueryBinder(OracleDbContext context)
    {
        _columnProjector = new ColumnProjector(isColumn: expression => expression is ColumnExpression);
        _parameterMap = []; 

        _aliasCounter = 0;

        _context = context;
    }


    public Expression Bind(Expression expression)
    {
        _parameterMap.Clear();
        _aliasCounter = 0;

        return Visit(expression);
    }


    protected override Expression VisitMethodCall(MethodCallExpression callExpression)
    {
        var method = callExpression.Method;
        method = method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;

        if (method.Name == "Select")
        {
            return BindSelect(callExpression.Type, callExpression.Arguments[0], (LambdaExpression)StripQuotes(callExpression.Arguments[1]));
        }
        else if (method.Name == "Where")
        {
            return BindWhere(callExpression.Type, callExpression.Arguments[0], (LambdaExpression)StripQuotes(callExpression.Arguments[1]));
        }
        else if (method.Name == "Join")
        {
            return this.BindJoin(
                callExpression.Type, 
                callExpression.Arguments[0], 
                callExpression.Arguments[1],

                (LambdaExpression)StripQuotes(callExpression.Arguments[2]),
                (LambdaExpression)StripQuotes(callExpression.Arguments[3]),
                (LambdaExpression)StripQuotes(callExpression.Arguments[4])
            );
        }
        else if (method == ParsableMethods.StringIndexing)
        {
            return BindMethodCalling(method, callExpression);
        }
        //else if (method.Name == "Exists")
        //{
        //    var predicate = callExpression.Arguments[0];

        //    var qf = new QueryFormatter(_context, monkeyPatch: true);

        //    string whereClause = qf.Format(predicate);

        //    var table = callExpression.Object.Type.GetGenericArguments();

        //    string tableName = table[0].Name + "s";

        //    string sql = $"""EXISTS (SELECT * FROM {tableName} WHERE {whereClause})""";

        //    return BindSubQuery(method, callExpression, sql);
        //}
        //else if (method.Name == "NotExists")
        //{
        //    var predicate = callExpression.Arguments[0];

        //    var qf = new QueryFormatter(_context, monkeyPatch: true);

        //    string whereClause = qf.Format(predicate);

        //    var table = callExpression.Object.Type.GetGenericArguments();

        //    string tableName = table[0].Name + "s";

        //    string sql = $"""NOT EXISTS (SELECT * FROM {tableName} WHERE {whereClause})""";

        //    return BindSubQuery(method, callExpression, sql);
        //}

        //else
        //{
        //    throw new NotSupportedException(string.Format("The method '{0}' is not supported", method.Name));
        //}

        return base.VisitMethodCall(callExpression);
    }

    protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
    {
        return base.VisitMemberAssignment(node);
    }

    private Expression BindSelect(Type resultType, Expression source, LambdaExpression selector)
    {
        ProjectionExpression projection = (ProjectionExpression)Visit(source);
        _parameterMap[selector.Parameters[0]] = projection.Projector;

        Expression expression = Visit(selector.Body);
      
        string alias = GetNextAlias();
        ProjectedColumns pc = ProjectColumns(expression, alias, GetExistingAlias(projection.Source));

        return new ProjectionExpression(
            new SelectExpression(resultType, alias, pc.Columns, projection.Source, null),
            pc.Projector
        );
    }

    private Expression BindWhere(Type resultType, Expression source, LambdaExpression predicate)
    {
        ProjectionExpression projection = (ProjectionExpression)Visit(source);
        _parameterMap[predicate.Parameters[0]] = projection.Projector;
        Expression where = Visit(predicate.Body);
        string alias = GetNextAlias();

        ProjectedColumns pc = ProjectColumns(projection.Projector, alias, GetExistingAlias(projection.Source));

        return new ProjectionExpression(
            new SelectExpression(resultType, alias, pc.Columns, projection.Source, where),
            pc.Projector
        );
    }

    protected virtual Expression BindJoin(Type resultType, Expression outerSource, Expression innerSource, LambdaExpression outerKey, LambdaExpression innerKey, LambdaExpression resultSelector)
    {
        ProjectionExpression outerProjection = (ProjectionExpression)Visit(outerSource);
        ProjectionExpression innerProjection = (ProjectionExpression)Visit(innerSource);
        _parameterMap[outerKey.Parameters[0]] = outerProjection.Projector;

        Expression outerKeyExpr = Visit(outerKey.Body);
        _parameterMap[innerKey.Parameters[0]] = innerProjection.Projector;
        Expression innerKeyExpr = Visit(innerKey.Body);

        _parameterMap[resultSelector.Parameters[0]] = outerProjection.Projector;
        _parameterMap[resultSelector.Parameters[1]] = innerProjection.Projector;

        Expression resultExpr = Visit(resultSelector.Body);
        JoinExpression join = new JoinExpression(resultType, JoinType.InnerJoin, outerProjection.Source, innerProjection.Source, Expression.Equal(outerKeyExpr, innerKeyExpr));

        string alias = GetNextAlias();
        ProjectedColumns pc = ProjectColumns(resultExpr, alias, outerProjection.Source.Alias);

        return new ProjectionExpression(
            new SelectExpression(resultType, alias, pc.Columns, join, null),
            pc.Projector
        );

    }



    private Expression BindMethodCalling(MethodInfo method, MethodCallExpression source)
    {
        return new FunctionCallingExpression(method, Visit(source.Object), source.Arguments.Select(arg => Visit(arg)));
    }

    private Expression BindSubQuery(MethodInfo method, MethodCallExpression source, string sql)
    {
        return new SubQueryExpression(method, Visit(source.Object), source.Arguments.Select(arg => Visit(arg)), sql);
    }

    private static string GetExistingAlias(Expression source)
    {
        if (source is not SqlExpression sqlExpression)
            throw new InvalidOperationException();


        return sqlExpression switch
        {
            SelectExpression selectExpression => selectExpression.Alias,
            TableExpression tableExpression => tableExpression.Alias,

            _ => throw new InvalidOperationException(string.Format("Invalid source node type '{0}'", source.NodeType)),
        };
    }

    private bool IsTable(object value)
    {
        return value is IQueryable { Expression.NodeType: ExpressionType.Constant };
    }

    private string GetTableName(object table)
    {
        IQueryable tableQuery = (IQueryable)table;
        Type rowType = tableQuery.ElementType;

        return rowType.Name;
    }

    private string GetColumnName(MemberInfo member)
    {
        return member.Name;
    }

    private Type GetColumnType(MemberInfo member)
    {
        PropertyInfo pi = (PropertyInfo)member;

        return pi.PropertyType;
    }

    private IEnumerable<MemberInfo> GetMappedMembers(Type rowType)
    {
        return rowType.GetProperties().Cast<MemberInfo>();
    }

    private ProjectionExpression GetTableProjection(object value)
    {
        IQueryable table = (IQueryable)value;

        string tableAlias = GetNextAlias();
        string selectAlias = GetNextAlias();

        List<MemberBinding> bindings = [];
        List<ColumnDeclaration> columns = [];

        foreach (MemberInfo mi in GetMappedMembers(table.ElementType))
        {
            string columnName = GetColumnName(mi);

            Type columnType = GetColumnType(mi);
            int ordinal = columns.Count;

            // Add to bindings ColumnExpression with selectAlias because in futere we get value from this select,
            // using columnName
            //
            //  SELECT (t0.Id, t0.Name) FROM (...) t1 ==> new Entity() { Id = t1.Id, Name = t1.Name } 
            //
            columns.Add(new ColumnDeclaration(columnName, new ColumnExpression(columnType, tableAlias, columnName, ordinal)));
            bindings.Add(Expression.Bind(mi, new ColumnExpression(columnType, selectAlias, columnName, ordinal)));
        }

        Expression projector = Expression.MemberInit(Expression.New(table.ElementType), bindings);
        Type resultType = typeof(IEnumerable<>).MakeGenericType(table.ElementType);

        return new ProjectionExpression(
            new SelectExpression(
                resultType,
                selectAlias,
                columns,
                new TableExpression(resultType, tableAlias, GetTableName(table)),
                null
            ),

            projector
        );
    }

    protected override Expression VisitConstant(ConstantExpression c)
    {
        if (IsTable(c.Value))
        {
            return GetTableProjection(c.Value);
        }

        return c;
    }

    protected override Expression VisitParameter(ParameterExpression parameterExpression)
    {
        if (_parameterMap.TryGetValue(parameterExpression, out Expression? expression))
        {
            return expression!;
        }

        return parameterExpression;
    }

    protected override Expression VisitMember(MemberExpression memberExpression)
    {
        Expression? source = Visit(memberExpression.Expression)
            ?? throw new InvalidOperationException("");

        switch (source.NodeType)
        {
            case ExpressionType.MemberInit:
                MemberInitExpression min = (MemberInitExpression)source;

                for (int i = 0, n = min.Bindings.Count; i < n; i++)
                {
                    if (min.Bindings[i] is MemberAssignment assign && MembersMatch(assign.Member, memberExpression.Member))
                    {
                        return assign.Expression;
                    }
                }

                break;

            case ExpressionType.New:
                NewExpression nex = (NewExpression)source;

                if (nex.Members != null)
                {
                    for (int i = 0, n = nex.Members.Count; i < n; i++)
                    {
                        if (MembersMatch(nex.Members[i], memberExpression.Member))
                        {
                            return nex.Arguments[i];
                        }
                    }
                }

                break;
        }

        if (source == memberExpression.Expression)
        {
            return memberExpression;
        }

        return MakeMemberAccess(source, memberExpression.Member);
    }

    private bool MembersMatch(MemberInfo left, MemberInfo right)
    {
        if (left == right)
        {
            return true;
        }

        if (left is MethodInfo && right is PropertyInfo rightProperty)
        {
            return left == rightProperty.GetGetMethod();
        }
        else if (left is PropertyInfo leftProperty && right is MethodInfo)
        {
            return leftProperty.GetGetMethod() == right;
        }

        return false;
    }

    private Expression MakeMemberAccess(Expression source, MemberInfo member)
    {
        PropertyInfo pi = (PropertyInfo)member;

        return Expression.Property(source, pi);
    }


    private static Expression StripQuotes(Expression expression)
    {
        while (expression.NodeType == ExpressionType.Quote)
        {
            expression = ((UnaryExpression)expression).Operand;
        }

        return expression;
    }

    private string GetNextAlias()
    {
        return $"t{_aliasCounter++}";
    }

    private ProjectedColumns ProjectColumns(Expression expression, string newAlias, string existingAlias)
    {
        return _columnProjector.ProjectColumns(expression, newAlias, existingAlias);
    }
}

public class TranslateResult
{
    internal string CommandText { get; init; }
    internal LambdaExpression Projector { get; init; }
}
