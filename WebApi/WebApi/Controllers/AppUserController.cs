using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleNetAuth.Data;
using SimpleNetAuth.Models;
using SimpleNetAuth.Models.SearchOptions;
using SimpleNetAuth.Tools;

namespace WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class AppUserController(SimpleNetAuthDataContext db, IConfiguration config) : ControllerBase
{
    #region GET

    //[HttpGet]
    //public async Task<ActionResult<IList<AppUser>>> Get()
    //{
    //    var retVal = await db.AppUsers.OrderBy(x => x.Name).ToListAsync();
    //    return Ok(retVal);
    //}

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppUser>> Get(int id)
    {
        var sampleItem = await db.AppUsers.SingleOrDefaultAsync(x => x.Id == id);
        return Ok(sampleItem);
    }

    #endregion

    #region DELETE

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id)
    {
        var item = await db.AppUsers.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();
        db.AppUsers.Remove(item);
        await db.SaveChangesAsync();
        return Ok();
    }

    #endregion

    #region POST

    [HttpPost]
    public async Task<ActionResult<AppUser>> Post([FromBody] AppUser value)
    {
        var dbItem = await db.AppUsers.SingleOrDefaultAsync(x => x.Id == value.Id);

        if (dbItem == null)
        {
            dbItem = new AppUser();
            db.AppUsers.Add(dbItem);
        }

        PropertyCopy.Copy(value, dbItem);
        await db.SaveChangesAsync();
        return Ok(dbItem);
    }

    #endregion

    #region SEARCH


    [HttpPost("Search")]
    public async Task<ActionResult<AppUser>> Post([FromBody] AppUserSearchOptions searchOptions, int pageIndex = 0, int pageSize = 10)
    {
        var query = GetQuery(searchOptions);
        if (string.IsNullOrEmpty(searchOptions.SortField)) searchOptions.SortField = "Name";
        if (string.IsNullOrEmpty(searchOptions.SortDirection)) searchOptions.SortDirection = "ASC";

        var retVal = await SearchResultsHelper.GetApiSearchResultsAsync(query, pageSize, pageIndex, searchOptions.SortField,
            searchOptions.SortDirection.ToLower().Contains("asc"));

        return Ok(retVal);
    }

    private IQueryable<AppUser> GetQuery(AppUserSearchOptions searchOptions)
    {
        var query = from q in db.AppUsers select q;
        if (searchOptions.Id.HasValue) query = query.Where(x => x.Id == searchOptions.Id);
        //if (!string.IsNullOrEmpty(searchOptions.Name)) query = query.Where(x => x.Name.Contains(searchOptions.Name));
        return query;
    }

    #endregion
}