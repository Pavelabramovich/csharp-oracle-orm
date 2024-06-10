//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Query;
using OracleOrm.Dev;
//using OracleOrm.Queries.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Data.OracleClient;

namespace OracleOrm;

internal class QueryBinder : ExpressionVisitor
{
    ColumnProjector columnProjector;
    Dictionary<ParameterExpression, Expression> map;
    int aliasCount;

    OracleDbContext _context;

    internal QueryBinder(OracleDbContext context)
    {
        this.columnProjector = new ColumnProjector(this.CanBeColumn);
        _context = context;
    }

    private bool CanBeColumn(Expression expression)
    {
        return expression is ColumnExpression;
    }

    internal Expression Bind(Expression expression)
    {
        this.map = new Dictionary<ParameterExpression, Expression>();

        return this.Visit(expression);
    }

    private static Expression StripQuotes(Expression e)
    {
        while (e.NodeType == ExpressionType.Quote)
        {
            e = ((UnaryExpression)e).Operand;
        }

        return e;
    }

    private string GetNextAlias()
    {
        return "t" + (aliasCount++);
    }

    private ProjectedColumns ProjectColumns(Expression expression, string newAlias, string existingAlias)
    {
        return this.columnProjector.ProjectColumns(expression, newAlias, existingAlias);
    }


    public Expression VisitMc(LambdaExpression LambdaExpr)
    {
        var qb = new QueryBinder(_context);
        qb.map = [];
        

        ProjectionExpression projection = (ProjectionExpression)qb.Visit(LambdaExpr.Body);
        qb.map[LambdaExpr.Parameters[0]] = projection.Projector;

        var res = qb.Visit(LambdaExpr);

        return res;
    }

    protected override Expression VisitMethodCall(MethodCallExpression methodCallExpr)
    {
        var method = methodCallExpr.Method;
        method = method.IsGenericMethod ? method.GetGenericMethodDefinition() : method;

        if (method.Name == "Select")
        {
            return BindSelect(methodCallExpr.Type, methodCallExpr.Arguments[0], (LambdaExpression)StripQuotes(methodCallExpr.Arguments[1]));
        }
        else if (method.Name == "Where")
        {
            return BindWhere(methodCallExpr.Type, methodCallExpr.Arguments[0], (LambdaExpression)StripQuotes(methodCallExpr.Arguments[1]));
        }
        else if (method.Name == "Join")
        {
            return this.BindJoin(
                methodCallExpr.Type, methodCallExpr.Arguments[0], methodCallExpr.Arguments[1],

                (LambdaExpression)StripQuotes(methodCallExpr.Arguments[2]),
                (LambdaExpression)StripQuotes(methodCallExpr.Arguments[3]),
                (LambdaExpression)StripQuotes(methodCallExpr.Arguments[4])
            );
        }

        else if (method == ParsableMethods.StringIndexing)
        {
            return BindMethodCalling(method, methodCallExpr);
        }
        else if (method.Name == "Exists")
        {
            var predicate = methodCallExpr.Arguments[0];

            var qf = new QueryFormatter(_context, monkeyPatch: true);

            string whereClause = qf.Format(predicate);

            var table = methodCallExpr.Object.Type.GetGenericArguments();

            string tableName = table[0].Name + "s";

            string sql = $"""EXISTS (SELECT * FROM {tableName} WHERE {whereClause})""";

            return BindSubQuery(method, methodCallExpr, sql);
        }
        else if (method.Name == "NotExists")
        {
            var predicate = methodCallExpr.Arguments[0];

            var qf = new QueryFormatter(_context, monkeyPatch: true);

            string whereClause = qf.Format(predicate);

            var table = methodCallExpr.Object.Type.GetGenericArguments();

            string tableName = table[0].Name + "s";

            string sql = $"""NOT EXISTS (SELECT * FROM {tableName} WHERE {whereClause})""";

            return BindSubQuery(method, methodCallExpr, sql);
        }

        //else
        //{
        //    throw new NotSupportedException(string.Format("The method '{0}' is not supported", method.Name));
        //}

        return base.VisitMethodCall(methodCallExpr);
    }

    protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
    {
        return base.VisitMemberAssignment(node);
    }

    private Expression BindSelect(Type resultType, Expression source, LambdaExpression selector)
    {
        ProjectionExpression projection = (ProjectionExpression)this.Visit(source);
        this.map[selector.Parameters[0]] = projection.Projector;

        Expression expression;
        try
        {
             expression = this.Visit(selector.Body);
        }
        catch (Exception exception)
        {
           // Console.WriteLine("CATCH!!!");
            throw;
        }

        string alias = this.GetNextAlias();
        ProjectedColumns pc = this.ProjectColumns(expression, alias, GetExistingAlias(projection.Source));

        return new ProjectionExpression(
            new SelectExpression(resultType, alias, pc.Columns, projection.Source, null),
            pc.Projector
        );
    }

    private Expression BindWhere(Type resultType, Expression source, LambdaExpression predicate)
    {
        ProjectionExpression projection = (ProjectionExpression)this.Visit(source);
        this.map[predicate.Parameters[0]] = projection.Projector;
        Expression where = this.Visit(predicate.Body);
        string alias = this.GetNextAlias();

        ProjectedColumns pc = this.ProjectColumns(projection.Projector, alias, GetExistingAlias(projection.Source));

        return new ProjectionExpression(
            new SelectExpression(resultType, alias, pc.Columns, projection.Source, where),
            pc.Projector
        );
    }

    protected virtual Expression BindJoin(Type resultType, Expression outerSource, Expression innerSource, LambdaExpression outerKey, LambdaExpression innerKey, LambdaExpression resultSelector)
    {
        ProjectionExpression outerProjection = (ProjectionExpression)this.Visit(outerSource);
        ProjectionExpression innerProjection = (ProjectionExpression)this.Visit(innerSource);
        this.map[outerKey.Parameters[0]] = outerProjection.Projector;

        Expression outerKeyExpr = this.Visit(outerKey.Body);
        this.map[innerKey.Parameters[0]] = innerProjection.Projector;
        Expression innerKeyExpr = this.Visit(innerKey.Body);

        this.map[resultSelector.Parameters[0]] = outerProjection.Projector;
        this.map[resultSelector.Parameters[1]] = innerProjection.Projector;

        Expression resultExpr = this.Visit(resultSelector.Body);
        JoinExpression join = new JoinExpression(resultType, JoinType.InnerJoin, outerProjection.Source, innerProjection.Source, Expression.Equal(outerKeyExpr, innerKeyExpr));

        string alias = this.GetNextAlias();
        ProjectedColumns pc = this.ProjectColumns(resultExpr, alias, outerProjection.Source.Alias);

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
        IQueryable q = value as IQueryable;

        return q != null && q.Expression.NodeType == ExpressionType.Constant;
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
        PropertyInfo fi = member as PropertyInfo;

        if (fi != null)
        {
            return fi.PropertyType;
        }

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

        string tableAlias = this.GetNextAlias();
        string selectAlias = this.GetNextAlias();

        List<MemberBinding> bindings = [];
        List<ColumnDeclaration> columns = [];

        foreach (MemberInfo mi in this.GetMappedMembers(table.ElementType))
        {
            string columnName = this.GetColumnName(mi);

            Type columnType = this.GetColumnType(mi);
            int ordinal = columns.Count;

            bindings.Add(Expression.Bind(mi, new ColumnExpression(columnType, selectAlias, columnName, ordinal)));
            columns.Add(new ColumnDeclaration(columnName, new ColumnExpression(columnType, tableAlias, columnName, ordinal)));
        }


        Expression projector = Expression.MemberInit(Expression.New(table.ElementType), bindings);
        Type resultType = typeof(IEnumerable<>).MakeGenericType(table.ElementType);

        return new ProjectionExpression(
            new SelectExpression(
                resultType,
                selectAlias,
                columns,
                new TableExpression(resultType, tableAlias, this.GetTableName(table)),
                null
            ),

            projector
        );
    }

    protected override Expression VisitConstant(ConstantExpression c)
    {
        if (this.IsTable(c.Value))
        {
            return GetTableProjection(c.Value);
        }

        return c;
    }

    protected override Expression VisitParameter(ParameterExpression p)
    {
        Expression e;

        if (this.map.TryGetValue(p, out e))
        {
            return e;
        }

        return p;
    }

    protected override Expression VisitMember(MemberExpression m)
    {
        Expression source = this.Visit(m.Expression);

        switch (source.NodeType)
        {
            case ExpressionType.MemberInit:
                MemberInitExpression min = (MemberInitExpression)source;

                for (int i = 0, n = min.Bindings.Count; i < n; i++)
                {
                    MemberAssignment assign = min.Bindings[i] as MemberAssignment;

                    if (assign != null && MembersMatch(assign.Member, m.Member))
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
                        if (MembersMatch(nex.Members[i], m.Member))
                        {
                            return nex.Arguments[i];
                        }
                    }
                }

                break;
        }

        if (source == m.Expression)
        {
            return m;
        }

        return MakeMemberAccess(source, m.Member);
    }

    private bool MembersMatch(MemberInfo a, MemberInfo b)
    {
        if (a == b)
        {
            return true;
        }

        if (a is MethodInfo && b is PropertyInfo)
        {
            return a == ((PropertyInfo)b).GetGetMethod();
        }
        else if (a is PropertyInfo && b is MethodInfo)
        {
            return ((PropertyInfo)a).GetGetMethod() == b;
        }

        return false;
    }

    private Expression MakeMemberAccess(Expression source, MemberInfo mi)
    {
        PropertyInfo fi = mi as PropertyInfo;

        if (fi != null)
        {
            return Expression.Property(source, fi);
        }

        PropertyInfo pi = (PropertyInfo)mi;

        return Expression.Property(source, pi);
    }
}

public class TranslateResult
{
    internal string CommandText;
    internal LambdaExpression Projector;
}


public class DdlTranslationResult : TranslateResult
{
    public string Ddl { get; set; }
}