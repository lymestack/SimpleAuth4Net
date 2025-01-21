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
public class AppUserController(SimpleAuthContext db) : ControllerBase
{
    #region GET

    [HttpGet("Me")]
    [AllowAnonymous]
    public async Task<ActionResult<AppUser?>> Get()
    {
        if (User.Identity is not { IsAuthenticated: true }) return Ok(null);
        var appUser = await db.AppUsers
            .Include(x => x.AppUserRoles)
            .ThenInclude(x => x.AppRole)
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Username == User.Identity.Name);

        if (appUser == null) return Ok(null);

        foreach (var userRole in appUser.AppUserRoles)
        {
            appUser.Roles.Add(userRole.AppRole.Name);
        }

        appUser.AppUserRoles = null;
        return Ok(appUser);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AppUser>> Get(int id)
    {
        var appUser = await db.AppUsers
            .Include(x => x.AppUserRoles)
            .ThenInclude(x => x.AppRole)
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id);

        if (appUser == null) return Ok(null);

        foreach (var userRole in appUser.AppUserRoles)
        {
            appUser.Roles.Add(userRole.AppRole.Name);
        }

        appUser.AppUserRoles = null;
        return Ok(appUser);
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
        var inserting = false;

        if (dbItem == null)
        {
            dbItem = new AppUser();
            db.AppUsers.Add(dbItem);
            inserting = true;
        }

        dbItem.Username = value.Username;
        dbItem.FirstName = value.FirstName;
        dbItem.LastName = value.LastName;
        dbItem.EmailAddress = value.EmailAddress;
        dbItem.PhoneNumber = value.PhoneNumber;
        dbItem.Active = value.Active;

        if (inserting)
        {
            dbItem.AppUserCredential = new AppUserCredential { DateCreated = DateTime.UtcNow };
            dbItem.DateEntered = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        SaveRoles(value, dbItem);
        return Ok(dbItem);
    }

    private void SaveRoles(AppUser value, AppUser? dbItem)
    {
        db.DeleteRolesForUser(dbItem.Id);

        foreach (var role in value.Roles)
        {
            db.AddRoleForUser(dbItem.Id, role);
        }
    }

    #endregion

    #region SEARCH


    [HttpPost("Search")]
    public async Task<ActionResult<AppUser>> Post([FromBody] AppUserSearchOptions searchOptions, int pageIndex = 0, int pageSize = 10)
    {
        var query = GetQuery(searchOptions);
        if (string.IsNullOrEmpty(searchOptions.SortField)) searchOptions.SortField = "Username";
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