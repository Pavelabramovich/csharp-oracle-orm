using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OracleOrm.Queries.Expressions;


namespace OracleOrm.Queries.Visitors;


public class DbQueryProvider : QueryProvider
{

    DbConnection connection;

    public DbQueryProvider(DbConnection connection)
    {
        this.connection = connection;
    }

    public override string GetQueryText(Expression expression)
    {
        return Translate(expression).CommandText;
    }

    public override object Execute(Expression expression)
    {
        TranslateResult result = Translate(expression);
        Delegate projector = result.Projector.Compile();

        DbCommand cmd = connection.CreateCommand();
        cmd.CommandText = result.CommandText;
        DbDataReader reader = cmd.ExecuteReader();

        Type elementType = expression.Type.GetElementType(); //TypeSystem.GetElementType(expression.Type);

        return Activator.CreateInstance(
            typeof(ProjectionReader<>).MakeGenericType(elementType),
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            new object[] { reader, projector },
            null
        )!;


    }

    internal class TranslateResult
    {
        internal string CommandText;
        internal LambdaExpression Projector;
    }


    private TranslateResult Translate(Expression expression)
    {
        expression = Evaluator.PartialEval(expression);


        ProjectionExpression proj = (ProjectionExpression)new QueryBinder().Bind(expression);


        string commandText = new QueryFormatter().Format(proj.Source);


        LambdaExpression projector = new ProjectionBuilder().Build(proj.Projector);


        return new TranslateResult { CommandText = commandText, Projector = projector };


    }
}


public static class Evaluator
{
    public static Expression PartialEval(Expression expression, Func<Expression, bool> fnCanBeEvaluated)
    {
        return new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression)).Eval(expression);
    }

    public static Expression PartialEval(Expression expression)
    {
        return PartialEval(expression, CanBeEvaluatedLocally);
    }

    private static bool CanBeEvaluatedLocally(Expression expression)
    {
        return expression.NodeType != ExpressionType.Parameter;
    }

    class SubtreeEvaluator : ExpressionVisitor
    {
        HashSet<Expression> candidates;

        internal SubtreeEvaluator(HashSet<Expression> candidates)
        {
            this.candidates = candidates;
        }

        internal Expression Eval(Expression exp)
        {
            return Visit(exp);
        }

        public override Expression Visit(Expression exp)
        {


            if (exp == null)
            {


                return null;


            }


            if (candidates.Contains(exp))
            {


                return Evaluate(exp);


            }


            return base.Visit(exp);


        }





        private Expression Evaluate(Expression e)
        {


            if (e.NodeType == ExpressionType.Constant)
            {


                return e;


            }


            LambdaExpression lambda = Expression.Lambda(e);


            Delegate fn = lambda.Compile();


            return Expression.Constant(fn.DynamicInvoke(null), e.Type);


        }


    }


    class Nominator : ExpressionVisitor
    {


        Func<Expression, bool> fnCanBeEvaluated;


        HashSet<Expression> candidates;


        bool cannotBeEvaluated;





        internal Nominator(Func<Expression, bool> fnCanBeEvaluated)
        {


            this.fnCanBeEvaluated = fnCanBeEvaluated;


        }





        internal HashSet<Expression> Nominate(Expression expression)
        {


            candidates = new HashSet<Expression>();


            Visit(expression);


            return candidates;


        }





        public override Expression Visit(Expression expression)
        {


            if (expression != null)
            {


                bool saveCannotBeEvaluated = cannotBeEvaluated;


                cannotBeEvaluated = false;


                base.Visit(expression);


                if (!cannotBeEvaluated)
                {


                    if (fnCanBeEvaluated(expression))
                    {


                        candidates.Add(expression);


                    }


                    else
                    {


                        cannotBeEvaluated = true;


                    }


                }


                cannotBeEvaluated |= saveCannotBeEvaluated;


            }


            return expression;


        }


    }
}