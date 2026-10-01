namespace WebApi.Tests;

// Placeholder so `dotnet test` has something to run. Real auth regression tests replace it.
public class SmokeTests
{
    [Fact]
    public void ApiAssemblyLoads()
    {
        Assert.NotNull(typeof(WebApi.Controllers.AuthController).Assembly);
    }
}
