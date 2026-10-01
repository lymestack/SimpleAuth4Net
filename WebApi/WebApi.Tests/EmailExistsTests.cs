using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using SimpleAuthNet.Models;

namespace WebApi.Tests;

// One factory (and database) per class; emails are unique per test so tests don't interfere.
public class EmailExistsTests(AuthApiFactory factory) : IClassFixture<AuthApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static AppUser Payload(int id, string username, string email) => new()
    {
        Id = id, Username = username, EmailAddress = email, FirstName = "Test", LastName = "User", Active = true
    };

    private static async Task<string?> ErrorCode(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.TryGetProperty("error", out var e) ? e.GetString() : null;
    }

    [Fact]
    public async Task Create_WithExistingEmail_DifferentCase_ReturnsEmailExists()
    {
        await factory.AddUserAsync("create-existing", "create.dup@example.com");
        var client = factory.CreateClientFor("Admin");

        var response = await client.PostAsJsonAsync("/AppUser", Payload(0, "create-new", "CREATE.Dup@Example.com"), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("EMAIL_EXISTS", await ErrorCode(response));
    }

    [Fact]
    public async Task Create_WithNewEmail_Succeeds()
    {
        var client = factory.CreateClientFor("Admin");

        var response = await client.PostAsJsonAsync("/AppUser", Payload(0, "create-fresh", "create.fresh@example.com"), Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Edit_ToAnotherUsersEmail_ReturnsEmailExists()
    {
        await factory.AddUserAsync("edit-other", "edit.other@example.com");
        var editing = await factory.AddUserAsync("edit-me", "edit.me@example.com");
        var client = factory.CreateClientFor("Admin");

        var response = await client.PostAsJsonAsync("/AppUser", Payload(editing.Id, "edit-me", "Edit.Other@example.com"), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("EMAIL_EXISTS", await ErrorCode(response));
    }

    [Theory]
    [InlineData("exact", "edit.same.exact@example.com", "edit.same.exact@example.com")]
    [InlineData("recased", "edit.same.recased@example.com", "EDIT.Same.ReCased@Example.com")]
    public async Task Edit_KeepingOwnEmail_IsNotReportedAsTaken(string name, string stored, string submitted)
    {
        var editing = await factory.AddUserAsync("edit-same-" + name, stored);
        var client = factory.CreateClientFor("Admin");

        var response = await client.PostAsJsonAsync("/AppUser", Payload(editing.Id, editing.Username, submitted), Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task EmailExists_ReportsCaseInsensitiveMatch_AndExcludesOwnUser()
    {
        var user = await factory.AddUserAsync("probe-user", "probe@example.com");
        var client = factory.CreateClientFor("Admin");

        var taken = await client.GetFromJsonAsync<JsonElement>("/Auth/EmailExists?email=PROBE@example.com");
        var own = await client.GetFromJsonAsync<JsonElement>($"/Auth/EmailExists?email=probe@example.com&userId={user.Id}");
        var free = await client.GetFromJsonAsync<JsonElement>("/Auth/EmailExists?email=nobody-here@example.com");

        Assert.True(taken.GetProperty("exists").GetBoolean());
        Assert.False(own.GetProperty("exists").GetBoolean());
        Assert.False(free.GetProperty("exists").GetBoolean());
    }
}
