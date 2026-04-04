using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using SimpleAuthNet.Data;
using System.Security.Claims;

namespace SimpleAuthNet;

public class LocalRoleClaimsTransformer : IClaimsTransformation
{
    private readonly IRoleDbContext _roleDb;

    public LocalRoleClaimsTransformer(IRoleDbContext roleDb)
    {
        _roleDb = roleDb;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity == null || !identity.IsAuthenticated)
            return principal;

        // Get the user ID from the sub claim
        var subClaim = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(subClaim) || !int.TryParse(subClaim, out var userId))
            return principal;

        // Skip if role claims already exist (e.g., Standalone mode where roles are in the JWT)
        if (principal.FindFirst(ClaimTypes.Role) != null)
            return principal;

        // Look up local roles for this user
        var roles = await _roleDb.AppUserRoles
            .Where(ur => ur.AppUserId == userId)
            .Include(ur => ur.AppRole)
            .Select(ur => ur.AppRole.Name)
            .ToListAsync();

        foreach (var role in roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
        }

        return principal;
    }
}
