# SimpleAuth Implementation Guide

This guide explains how to integrate SimpleAuth into your own .NET WebApi project by cloning the source code, referencing the core library, and wiring up the necessary configuration and endpoints.

---

## Prerequisites

* .NET 8 SDK or later
* An existing WebApi project or plan to create a new one
* Basic understanding of `Program.cs`, middleware configuration, and controller routing in ASP.NET Core

---

## Step 1: Clone the SimpleAuth Repository

Start by cloning the [SimpleAuth GitHub repository](https://github.com/your-org/simpleauth) locally:

```bash
git clone https://github.com/your-org/simpleauth.git
```

---

## Step 2: Build the Solution

Navigate into the repository and open it with your IDE. Ensure the solution builds successfully:

```bash
cd simpleauth
```

Build the project:

```bash
dotnet build
```

---

## Step 3: Add Reference to `SimpleAuthNet`

In your target project (where you want to integrate authentication), add a project reference to `SimpleAuthNet.csproj`:

```bash
dotnet add reference ../simpleauth/SimpleAuthNet/SimpleAuthNet.csproj
```

---

## Step 4: Copy Required Controllers

From the `WebApi/Controllers` folder in the SimpleAuth repo, copy the following controllers into your own project's `Controllers` folder:

* `AuthController.cs`
* `AppConfigController.cs`
* Any optional CRUD controllers you wish to use (e.g., `AppRoleController.cs`)

Make any necessary namespace adjustments.

---

## Step 5: Modify Your `Program.cs`

In your project's `Program.cs`, add the following using statement:

```csharp
using SimpleAuthNet;
```

Then, modify your service configuration to register the required SimpleAuth services:

```csharp
builder.Services
    .AddSimpleAuthDbContext()
    .AddSimpleAuthControllers()
    .AddSimpleAuthCors(builder.Configuration)
    .AddSimpleAuthRateLimiting(builder.Configuration)
    .AddSimpleAuthLogging(builder.Configuration)
    .AddSimpleAuthJwt(builder.Configuration)
    .AddSimpleAuthDefaultAuthorization();
```

Ensure the following middleware is added in the correct order *before* `app.Run()`:

```csharp
app.UseCors("default");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
```

---

## Step 6: Configure `appsettings.json`

Add the following sections from the SimpleAuth sample `appsettings.json` into your project:

* `AppConfig`
* `AuthSettings`
* `EmailSettings`
* `SmsSettings` *(if applicable)*

Customize values like token secret, issuer name, SMTP server, etc., based on your environment.

---

## Step 7: Set Up the Database

SimpleAuth requires a database to store user, token, and session data.

1. Create your database manually (e.g., in SQL Server)
2. Point the `ConnectionStrings:DefaultConnection` setting to it
3. Run EF Core migrations or use a migration-free manual schema setup *(if you're seeding manually)*

---

## Optional: Customize UI or Client Integration

The `AppConfig` controller exposes public auth config to clients. You can build your Angular or Blazor front-end to respect these config values.

---

## Staying Up To Date

Since this is an open-source repo intended for transparency and modification, it's recommended you fork the repo and track updates via Git. You can diff against new tags or periodically pull changes into your local fork manually.

---

## Support & Contributions

For feature requests, issues, or contributing improvements, open a GitHub Issue or Pull Request on the [main SimpleAuth repository](https://github.com/your-org/simpleauth).

---

Happy coding!
