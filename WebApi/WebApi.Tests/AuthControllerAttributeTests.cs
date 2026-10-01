using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using WebApi.Controllers;

namespace WebApi.Tests;

// The API applies a global "authenticated user" filter, so these attributes are the whole public
// surface of the auth endpoints. A class-level [AllowAnonymous] would silently expose every new action.
public class AuthControllerAttributeTests
{
    private static readonly Type Controller = typeof(AuthController);

    private static IEnumerable<MethodInfo> Actions() =>
        Controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any());

    [Fact]
    public void HasNoClassLevelAllowAnonymous()
    {
        Assert.Empty(Controller.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true));
    }

    [Fact]
    public void EveryActionHasExactlyOneOfAuthorizeOrAllowAnonymous()
    {
        var actions = Actions().ToList();
        Assert.NotEmpty(actions);

        var offenders = actions
            .Where(m => m.GetCustomAttributes<AuthorizeAttribute>().Count()
                      + m.GetCustomAttributes<AllowAnonymousAttribute>().Count() != 1)
            .Select(m => m.Name)
            .ToList();

        Assert.True(offenders.Count == 0, "Actions without exactly one auth attribute: " + string.Join(", ", offenders));
    }

    [Theory]
    [InlineData(nameof(AuthController.UnlockUser))]
    [InlineData(nameof(AuthController.RevokeAllSessionsForUser))]
    [InlineData(nameof(AuthController.RevokeAllSessions))]
    [InlineData(nameof(AuthController.EmailExists))]
    public void AdminActionsRequireAdminRole(string actionName)
    {
        var authorize = Controller.GetMethod(actionName)!.GetCustomAttributes<AuthorizeAttribute>().Single();
        Assert.Equal("Admin", authorize.Roles);
    }
}
