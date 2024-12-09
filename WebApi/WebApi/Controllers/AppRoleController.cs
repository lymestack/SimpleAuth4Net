using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleAuthNet.Data;
using SimpleAuthNet.Models;
using SimpleAuthNet.Models.SearchOptions;
using SimpleAuthNet.Tools;

namespace WebApi.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Roles = "Admin")]
public class AppRoleController(SimpleAuthContext db) : ControllerBase
{
    #region GET

    [HttpGet]
    public async Task<ActionResult<AppRole>> Get()
    {
        var retVal = await db.AppRoles.OrderBy(x => x.Name).ToListAsync();
        return Ok(retVal);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppRole>> Get(int id)
    {
        var role = await db.AppRoles.SingleOrDefaultAsync(x => x.Id == id);
        return Ok(role);
    }

    #endregion

    #region DELETE

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var item = await db.AppRoles.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();
        db.AppRoles.Remove(item);
        await db.SaveChangesAsync();
        return Ok();
    }

    #endregion

    #region POST

    [HttpPost]
    public async Task<ActionResult<AppRole>> Post([FromBody] AppRole value)
    {
        var dbItem = await db.AppRoles.SingleOrDefaultAsync(x => x.Id == value.Id);

        if (dbItem == null)
        {
            dbItem = new AppRole();
            await db.AppRoles.AddAsync(dbItem);
        }

        PropertyCopy.Copy(value, dbItem);
        await db.SaveChangesAsync();
        return Ok(dbItem);
    }

    #endregion

    #region SEARCH

    [HttpPost("Search")]
    public async Task<ActionResult<AppRole>> Post([FromBody] AppRoleSearchOptions searchOptions, int pageIndex = 0, int pageSize = 10)
    {
        var query = GetQuery(searchOptions);
        if (string.IsNullOrEmpty(searchOptions.SortField)) searchOptions.SortField = "Name";
        if (string.IsNullOrEmpty(searchOptions.SortDirection)) searchOptions.SortDirection = "ASC";

        var retVal = await SearchResultsHelper.GetApiSearchResultsAsync(query, pageSize, pageIndex, searchOptions.SortField,
            searchOptions.SortDirection.ToLower().Contains("asc"));

        return Ok(retVal);
    }

    private IQueryable<AppRole> GetQuery(AppRoleSearchOptions searchOptions)
    {
        var query = from q in db.AppRoles select q;
        if (searchOptions.Id.HasValue) query = query.Where(x => x.Id == searchOptions.Id);
        if (!string.IsNullOrEmpty(searchOptions.Name)) query = query.Where(x => x.Name.Contains(searchOptions.Name));
        return query;
    }

    #endregion
}