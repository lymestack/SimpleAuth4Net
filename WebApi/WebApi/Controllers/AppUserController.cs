using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleAuthNet.Data;
using SimpleAuthNet.Models;
using SimpleAuthNet.Models.Api;
using SimpleAuthNet.Models.SearchOptions;

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
            dbItem = new AppUser
            {
                DateEntered = DateTime.UtcNow
            };

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
            dbItem.AppUserCredential = new AppUserCredential { DateCreated = DateTime.UtcNow, VerifyTokenExpires = DateTime.UtcNow };
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

        //var retVal = await SearchResultsHelper.GetApiSearchResultsAsync(query, pageSize, pageIndex, searchOptions.SortField,
        //    searchOptions.SortDirection.ToLower().Contains("asc"));

        var retVal = new ApiSearchResults<AppUser>();
        retVal.TotalCount = query.Count();
        retVal.TotalPages = (int)Math.Ceiling((double)retVal.TotalCount / pageSize);

        var sortedResult = GetOrderedQueryable(query, searchOptions.SortField, searchOptions.SortDirection);
        retVal.Results = await sortedResult.Skip(pageIndex * pageSize).Take(pageSize).ToListAsync();

        // REDACTING PASSWORDS / POST PROCESSING:
        foreach (var result in retVal.Results)
        {
            foreach (var role in result.AppUserRoles)
                result.Roles.Add(role.AppRole.Name);

            result.AppUserRoles = null;
        }

        return Ok(retVal);
    }

    private IOrderedQueryable<AppUser> GetOrderedQueryable(IQueryable<AppUser> query, string sortField, string sortDirection)
    {
        var ascending = sortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);

        return sortField.ToLower() switch
        {
            "name" => ascending
                ? query.OrderBy(x => x.FirstName).ThenBy(x => x.LastName)
                : query.OrderByDescending(x => x.FirstName).ThenByDescending(x => x.LastName),

            "username" => ascending
                ? query.OrderBy(x => x.Username)
                : query.OrderByDescending(x => x.Username),

            "emailaddress" => ascending
                ? query.OrderBy(x => x.EmailAddress)
                : query.OrderByDescending(x => x.EmailAddress),

            "phonenumber" => ascending
                ? query.OrderBy(x => x.PhoneNumber)
                : query.OrderByDescending(x => x.PhoneNumber),

            "dateentered" => ascending
                ? query.OrderBy(x => x.DateEntered)
                : query.OrderByDescending(x => x.DateEntered),

            "lastseen" => ascending
                ? query.OrderBy(x => x.LastSeen)
                : query.OrderByDescending(x => x.LastSeen),

            _ => ascending
                ? query.OrderBy(x => x.Username) // default fallback
                : query.OrderByDescending(x => x.Username),
        };
    }


    private IQueryable<AppUser> GetQuery(AppUserSearchOptions searchOptions)
    {
        var query = db.Set<AppUser>()
            .Include(x => x.AppUserRoles)
            .ThenInclude(x => x.AppRole)
            .AsQueryable();

        if (searchOptions.Id.HasValue) query = query.Where(x => x.Id == searchOptions.Id);
        if (!string.IsNullOrEmpty(searchOptions.Username)) query = query.Where(x => x.Username.Contains(searchOptions.Username));
        if (!string.IsNullOrEmpty(searchOptions.EmailAddress)) query = query.Where(x => x.EmailAddress.Contains(searchOptions.EmailAddress));
        if (!string.IsNullOrEmpty(searchOptions.FirstName)) query = query.Where(x => x.FirstName.Contains(searchOptions.FirstName));
        // if (!string.IsNullOrEmpty(searchOptions.LastName)) query = query.Where(x => x.LastName.Contains(searchOptions.LastName));

        // HACK:
        if (!string.IsNullOrEmpty(searchOptions.LastName))
        {
            query = query.Where(x => x.LastName.Contains(searchOptions.LastName) ||
                                     x.FirstName.Contains(searchOptions.LastName));
        }

        if (searchOptions.DateEnteredLower.HasValue) query = query.Where(x => x.DateEntered >= searchOptions.DateEnteredLower);
        if (searchOptions.DateEnteredUpper.HasValue) query = query.Where(x => x.DateEntered <= searchOptions.DateEnteredUpper);
        if (searchOptions.LastSeenLower.HasValue) query = query.Where(x => x.LastSeen >= searchOptions.LastSeenLower);
        if (searchOptions.LastSeenUpper.HasValue) query = query.Where(x => x.LastSeen <= searchOptions.LastSeenUpper);
        if (searchOptions.Verified.HasValue) query = query.Where(x => x.Verified == searchOptions.Verified);
        if (searchOptions.Active.HasValue) query = query.Where(x => x.Active == searchOptions.Active);
        if (searchOptions.Locked.HasValue) query = query.Where(x => x.Locked == searchOptions.Locked);
        return query;
    }

    #endregion
}