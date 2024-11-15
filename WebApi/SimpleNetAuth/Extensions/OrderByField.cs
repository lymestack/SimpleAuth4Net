using System.Linq.Expressions;

namespace SimpleNetAuth.Extensions;

/// <summary>
/// Lifted from: https://ronniediaz.com/2011/05/24/orderby-string-in-linq-c-net-dynamic-sorting-of-anonymous-types/
/// </summary>
public static class OrderByFieldExtension
{
    public static IQueryable<T> OrderByField<T>(this IQueryable<T> q, string SortField, bool Ascending)
    {
        var param = Expression.Parameter(typeof(T), "p");
        var prop = Expression.Property(param, SortField);
        var exp = Expression.Lambda(prop, param);
        var method = Ascending ? "OrderBy" : "OrderByDescending";
        var types = new Type[] { q.ElementType, exp.Body.Type };
        var mce = Expression.Call(typeof(Queryable), method, types, q.Expression, exp);
        return q.Provider.CreateQuery<T>(mce);
    }
}