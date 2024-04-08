using System;
using System.Collections.Concurrent;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Dynamic;
using System.Reflection.Metadata;


namespace OracleOrm;


public static class QueryMapper
{
    public delegate T ConvertDictionary<T>(IDictionary<string, object?> dict);

    private static ConcurrentDictionary<Type, Delegate> _mapperFuncs = new()
    {
        [typeof(int)] = new ConvertDictionary<int>(dict => dict.Values.Single() is var key ? Convert.ToInt32(key) : -1),
        [typeof(string)] = new ConvertDictionary<string>(dict => dict.Values.Single() is var key and not null ? key.ToString()! : "-1"),
        [typeof(bool)] = new ConvertDictionary<bool>(dict => Convert.ToBoolean(dict.Values.Single()))
    };


    private static readonly PropertyInfo s_dictionaryIndexer = typeof(IDictionary<string, object?>)
        .GetProperties()
        .Single(p => p.GetIndexParameters().Any());

    private static readonly MethodInfo s_getTypeMethod = typeof(object).GetMethod(nameof(GetType))!;
    private static readonly MethodInfo s_ChangeTypeMethod = typeof(Convert).GetMethod(nameof(Convert.ChangeType), [typeof(object), typeof(Type)])!;
    private static readonly MethodInfo s_ToPascalCaseMethod = typeof(CaseConverter).GetMethod(nameof(CaseConverter.ToPascalCase))!;

    public static T Map<T>(ExpandoObject obj) 
    {
        var map = (ConvertDictionary<T>)_mapperFuncs.GetOrAdd(typeof(T), t => Build<T>());

        return map(obj);
    }


    /*  public static T Build<T>(IDictionary<string, object?> objectProperties)
        {
            return new T()
            {
                property.Name = buildProperty(dict, property) for property in T.Properties
            }
        }
    */
    public static ConvertDictionary<T> Build<T>()
    {
        var dictParam = Expression.Parameter(typeof(IDictionary<string, object?>));

        var newExpr = Expression.New(typeof(T));

        var memberInit = Expression.MemberInit(newExpr, typeof(T)
            .GetProperties()
            .Select(p => Expression.Bind(p, BuildPropertyExpression(dictParam, p))));

        return Expression.Lambda<ConvertDictionary<T>>(memberInit, dictParam).Compile();
    }


    /*  private static property.PropertyType BuildProperty(IDictionary<string, object?> dict, PropertyInfo property)
        {
            return (property.Type)CaseConverter.ToPascalCase(dict[property.Name]);
        }
    */
    private static Expression BuildPropertyExpression(Expression dictExpression, PropertyInfo property)
    {
        var indexExpression = Expression.MakeIndex(dictExpression, s_dictionaryIndexer, [Expression.Constant(property.Name)]);
        var convertExpression = Expression.Call(instance: null, s_ChangeTypeMethod, indexExpression, Expression.Constant(property.PropertyType));

        return Expression.Convert(convertExpression, property.PropertyType);
    }
}