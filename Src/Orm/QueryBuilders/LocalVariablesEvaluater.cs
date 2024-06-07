using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;


namespace OracleOrm;


public static class PretranslateEvaluator
{
    private static readonly Func<Expression, bool> s_defaultIsEvaluatable = expression => expression.NodeType != ExpressionType.Parameter;


    public static Expression Evaluate(Expression expression)
    {
        return Evaluate(expression, s_defaultIsEvaluatable);
    }

    public static Expression Evaluate(Expression expression, Func<Expression, bool> isEvaluatable)
    {
        var evaluatableNodes = new EvaluatableNodesFilter(isEvaluatable).GetEvaluatableNodes(expression);
        var evaluatedExpression = new SubtreeEvaluator(evaluatableNodes).ReplaceEvaluatableNodes(expression);

        return evaluatedExpression;
    }


    private class SubtreeEvaluator : ExpressionVisitor
    {
        private readonly HashSet<Expression> _evaluatableNodes;


        public SubtreeEvaluator(HashSet<Expression> evaluatableNodes)
        {
            _evaluatableNodes = evaluatableNodes;
        }

        public Expression ReplaceEvaluatableNodes(Expression expression)
        {
            return Visit(expression);
        }


        [return: NotNullIfNotNull(nameof(expression))]
        public override Expression? Visit(Expression? expression)
        {
            if (expression is null)
                return null;

            if (_evaluatableNodes.Contains(expression))
                return Evaluate(expression);

            return base.Visit(expression);
        }

        private static Expression Evaluate(Expression evaluation)
        {
            if (evaluation.NodeType == ExpressionType.Constant)
                return evaluation;

            LambdaExpression evaluationLambda = Expression.Lambda(body: evaluation);
            object? evaluationValue = evaluationLambda.Compile().DynamicInvoke([]);

            return Expression.Constant(evaluationValue, evaluation.Type);
        }
    }


    // Main goal of this class is to find nodes that can be calculated in before a SQL query
    // creating (various operations with expressions captured by closure). This allows
    // for simplifying expressions for subsequent processing.
    private class EvaluatableNodesFilter : ExpressionVisitor
    {
        private readonly Func<Expression, bool> _isEvaluatable;
        private readonly HashSet<Expression> _evaluatableNodes;

        private bool _isNodeEvaluatable;

        public EvaluatableNodesFilter(Func<Expression, bool> isEvaluatable)
        {
            _isEvaluatable = isEvaluatable;
            _evaluatableNodes = [];

            _isNodeEvaluatable = false;
        }

        public HashSet<Expression> GetEvaluatableNodes(Expression expression)
        {
            Visit(expression);
            return _evaluatableNodes;
        }
        
        public override Expression? Visit(Expression? expression)
        {
            if (expression is null)
                return expression;

            bool tempIsNodeEvaluatable = _isNodeEvaluatable;
            _isNodeEvaluatable = true;

            base.Visit(expression);

            if (_isNodeEvaluatable)
            {
                if (_isEvaluatable(expression))
                {
                    _evaluatableNodes.Add(expression);
                }
                else
                {
                    _isNodeEvaluatable = false;
                }
            }

            _isNodeEvaluatable &= tempIsNodeEvaluatable;

            return expression;
        }
    }
}