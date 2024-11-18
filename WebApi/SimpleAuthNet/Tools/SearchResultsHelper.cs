using Microsoft.EntityFrameworkCore;
using SimpleAuthNet.Extensions;
using SimpleAuthNet.Models.Api;

namespace SimpleAuthNet.Tools;
public class SearchResultsHelper
{
    public static ApiSearchResults<T> GetApiSearchResults<T>(IQueryable<T> query, int pageSize,
        int pageIndex, string orderByField, bool orderAscending = true)
    {
        var totalCount = query.Count();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        IList<T> results = query.OrderByField(orderByField, orderAscending)
            .Skip(pageIndex * pageSize)
            .Take(pageSize).ToList();

        var retVal = new ApiSearchResults<T>
        {
            TotalCount = totalCount,
            TotalPages = totalPages,
            Results = results
        };

        return retVal;
    }

    public static async Task<ApiSearchResults<T>> GetApiSearchResultsAsync<T>(IQueryable<T> query, int pageSize,
        int pageIndex, string orderByField, bool orderAscending = true)
    {
        var totalCount = query.Count();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        IList<T> results = await query.OrderByField(orderByField, orderAscending)
            .Skip(pageIndex * pageSize)
            .Take(pageSize).ToListAsync();

        var retVal = new ApiSearchResults<T>
        {
            TotalCount = totalCount,
            TotalPages = totalPages,
            Results = results
        };

        return retVal;
    }
}