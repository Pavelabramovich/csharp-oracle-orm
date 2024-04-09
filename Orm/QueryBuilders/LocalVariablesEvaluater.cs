using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


namespace OracleOrm;


public class LocalVariablesEvaluater
{
    public static Expression Evaluate(Expression expression, Func<Expression, bool> fnCanBeEvaluated)
    {
        return new SubtreeEvaluator(new Nominator(fnCanBeEvaluated).Nominate(expression)).Eval(expression);
    }

    public static Expression Evaluate(Expression expression)
    {
        return Evaluate(expression, CanBeEvaluatedLocally);
    }

    private static bool CanBeEvaluatedLocally(Expression expression)
    {
        return expression.NodeType != ExpressionType.Parameter;
    }

    private class SubtreeEvaluator : ExpressionVisitor
    {
        private readonly HashSet<Expression> _candidates;


        internal SubtreeEvaluator(HashSet<Expression> candidates)
        {
            _candidates = candidates;
        }

        internal Expression Eval(Expression exp)
        {
            return Visit(exp);
        }

        public override Expression Visit(Expression? exp)
        {
            if (exp == null)
            {
                return null;
            }

            if (_candidates.Contains(exp))
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

    private class Nominator : ExpressionVisitor
    {
        private Func<Expression, bool> _fnCanBeEvaluated;
        private HashSet<Expression> _candidates;

        bool _cannotBeEvaluated;

        internal Nominator(Func<Expression, bool> fnCanBeEvaluated)
        {
            _fnCanBeEvaluated = fnCanBeEvaluated;
            _candidates = [];
        }

        internal HashSet<Expression> Nominate(Expression expression)
        {
            Visit(expression);
            return _candidates;
        }

        public override Expression Visit(Expression? expression)
        {
            if (expression != null)
            {
                bool saveCannotBeEvaluated = _cannotBeEvaluated;
                _cannotBeEvaluated = false;

                base.Visit(expression);

                if (!_cannotBeEvaluated)
                {
                    if (_fnCanBeEvaluated(expression))
                    {
                        _candidates.Add(expression);
                    }
                    else
                    {
                        _cannotBeEvaluated = true;
                    }
                }

                _cannotBeEvaluated |= saveCannotBeEvaluated;
            }

            return expression;
        }
    }
}