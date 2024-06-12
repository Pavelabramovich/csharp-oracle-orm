﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace OracleOrm;







internal sealed class ProjectedColumns
{
    Expression projector;
    ReadOnlyCollection<ColumnDeclaration> columns;

    internal ProjectedColumns(Expression projector, ReadOnlyCollection<ColumnDeclaration> columns)
    {
        this.projector = projector;
        this.columns = columns;
    }

    internal Expression Projector
    {
        get { return this.projector; }
    }

    internal ReadOnlyCollection<ColumnDeclaration> Columns
    {
        get { return this.columns; }
    }
}


internal class ColumnProjector : SqlExpressionVisitor
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
        this.nominator = new Nominator(fnCanBeColumn);
    }

    internal ProjectedColumns ProjectColumns(Expression expression, string newAlias, string existingAlias)
    {
        this.map = new Dictionary<ColumnExpression, ColumnExpression>();
        this.columns = new List<ColumnDeclaration>();
        this.columnNames = new HashSet<string>();
        this.newAlias = newAlias;
        this.existingAlias = existingAlias;
        this.candidates = this.nominator.Nominate(expression);

        return new ProjectedColumns(this.Visit(expression), this.columns.AsReadOnly());
    }


    public override Expression Visit(Expression? expression)
    {
        ArgumentNullException
            .ThrowIfNull(expression, nameof(expression));   

        if (this.candidates.Contains(expression))
        {
            if (expression is ColumnExpression column)
            { 
                ColumnExpression mapped;

                if (this.map.TryGetValue(column, out mapped))
                {
                    return mapped;
                }

                if (this.existingAlias == column.Alias)
                {
                    int ordinal = this.columns.Count;
                    string columnName = this.GetUniqueColumnName(column.Name);
                    this.columns.Add(new ColumnDeclaration(columnName, column));

                    mapped = new ColumnExpression(column.Type, this.newAlias, columnName, ordinal);
                    this.map[column] = mapped;
                    this.columnNames.Add(columnName);

                    return mapped;
                }

                // must be referring to outer scope
                return column;
            }
            else
            {
                throw null;

                //string columnName = this.GetNextColumnName();
                //int ordinal = this.columns.Count;

                //this.columns.Add(new ColumnDeclaration(columnName, expression));

                //return new ColumnExpression(expression.Type, this.newAlias, columnName, ordinal);
            }
        }
        else
        {
            return base.Visit(expression);
        }
    }


    private bool IsColumnNameInUse(string name)
    {
        return this.columnNames.Contains(name);
    }

    private string GetUniqueColumnName(string name)
    {
        string baseName = name;
        int suffix = 1;

        while (this.IsColumnNameInUse(name))
        {
            name = baseName + (suffix++);
        }

        return name;
    }

    private string GetNextColumnName()
    {
        return this.GetUniqueColumnName("c" + (iColumn++));
    }


    class Nominator : SqlExpressionVisitor
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
            this.candidates = new HashSet<Expression>();
            this.isBlocked = false;

            this.Visit(expression);

            return this.candidates;
        }

        public override Expression Visit(Expression? expression)
        {
            if (expression is FunctionCallingExpression funcCalling)
            {
                Visit(funcCalling.Instance);
                funcCalling.Params.Select(p => Visit(p)).ToList();

                candidates.Add(expression);
            }
            else if (expression != null)
            { 
                bool saveIsBlocked = this.isBlocked;
                this.isBlocked = false;

                base.Visit(expression);

                if (!this.isBlocked)
                {
                    if (this.fnCanBeColumn(expression))
                    {
                        this.candidates.Add(expression);
                    }
                    else
                    {
                        this.isBlocked = true;
                    }
                }

                this.isBlocked |= saveIsBlocked;
            }

            return expression;
        }
    }
}
