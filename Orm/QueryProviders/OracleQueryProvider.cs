using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OracleOrm.Core;


namespace OracleOrm;


public class OracleQueryProvider : QueryProvider
{
    //private static readonly MethodInfo s_ChangeTypeMethod
    //    = typeof(Convert).GetMethod(nameof(Convert.ChangeType), [typeof(object), typeof(Type)])!;

    private readonly OracleDbContext _context;


    public OracleQueryProvider(OracleDbContext context)
    {
        _context = context;
    }

    public override object Execute(Expression expression)
    {
        return this.Execute(this.Translate(expression));
    }

    public object Execute(TranslateResult query)
    {
       // TranslateResult result = Translate(expression);
        Delegate projector = query.Projector.Compile();

        var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = query.CommandText;

        if (command.Connection is null)
            throw new InvalidOperationException("Can not create command with valid connection.");

        if (command.Connection.State != ConnectionState.Open)
            command.Connection.Open();

        command.CommandText = query.CommandText;

        DbDataReader reader = command.ExecuteReader();


        Type elementType = TypeSystem.GetElementType(query.Projector.Body.Type);

        return Activator.CreateInstance(
            typeof(ProjectionReader<>).MakeGenericType(elementType),
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            [reader, projector, this],
            null
        )!;
    }


    //private TranslateResult Translate(Expression expression)
    //{
    //    expression = LocalVariablesEvaluater.Evaluate(expression);

    //    ProjectionExpression proj = (ProjectionExpression)new QueryBinder().Bind(expression);
    //    string commandText = new QueryFormatter(_context).Format(proj.Source);
    //    LambdaExpression projector = new ProjectionBuilder().Build(proj.Projector, null);

    //  //  var convertExpression = Expression.Call(instance: null, s_ChangeTypeMethod, projector, Expression.Constant(property.PropertyType));

    // //   return Expression.Convert(convertExpression, property.PropertyType);


    //    return new TranslateResult { CommandText = commandText, Projector = projector };
    //}

    private TranslateResult Translate(Expression expression)
    {
        ProjectionExpression projection = expression as ProjectionExpression;

        if (projection == null)
        {
            expression = Evaluator.PartialEval(expression);
            projection = (ProjectionExpression)new QueryBinder().Bind(expression);
        }

        string commandText = new QueryFormatter(_context).Format(projection.Source);
        LambdaExpression projector = new ProjectionBuilder().Build(projection.Projector, projection.Source.Alias);

        return new TranslateResult { CommandText = commandText, Projector = projector };
    }

    //public override object? Execute(Expression expression)
    //{
    //    BuildResult res = new QueryBinder(_context).Build(expression);

    //    Delegate projector = res.Projector.Compile();

    //    var command = _context.Database.GetDbConnection().CreateCommand();
    //    command.CommandText = res.CommandText;

    //    if (command.Connection is null)
    //        throw new InvalidOperationException("Can not create command with valid connection.");

    //    if (command.Connection.State != ConnectionState.Open)
    //        command.Connection.Open();

    //    command.CommandText = res.CommandText;

    //    DbDataReader reader = command.ExecuteReader();
    //    Type elementType = TypeSystem.GetElementType(expression.Type);

    //    return Activator.CreateInstance(
    //        typeof(ProjectionReader<>).MakeGenericType(elementType),
    //        BindingFlags.Instance | BindingFlags.NonPublic, null,
    //        [reader, projector],
    //        null
    //    );

    //}

    //public override string GetQueryString(Expression expression)
    //{
    //    return new QueryBuilder(_context).Build(expression).CommandText;
    //}
}
