using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;


namespace OracleOrm;


public class OracleQueryProvider : QueryProvider
{

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
        Delegate projector = query.Projector.Compile();

        var command = _context.Connection.CreateCommand();
        command.CommandText = query.CommandText;


        if (command.Connection is null)
            throw new InvalidOperationException("Can not create command with valid connection.");

        if (command.Connection.State != ConnectionState.Open)
            command.Connection.Open();

        command.CommandText = query.CommandText;

        DbDataReader reader = command.ExecuteReader();







        Type elementType = TypeSystem.GetElementType(query.Projector.Body.Type);

        var res = Activator.CreateInstance(
            typeof(ProjectionReader<>).MakeGenericType(elementType),
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            [reader, projector, this],
            null
        )!;

        return res;
    }



    private TranslateResult Translate(Expression expression) 
    {
        ProjectionExpression projection = expression as ProjectionExpression;

        if (projection == null)
        {
            expression = PretranslateEvaluator.Evaluate(expression);

            Expression result = new QueryBinder(_context).Bind(expression);

            projection = (ProjectionExpression)result;
        }

        string commandText = new QueryFormatter(_context).Format(projection.Source);


        Console.WriteLine(commandText + "\n\n");

        LambdaExpression projector = new ProjectionBuilder().Build(projection.Projector, projection.Source.Alias);

        return new TranslateResult { CommandText = commandText, Projector = projector };
    }
}
