# SimpleAuth for .NET

Making auth suck less since 2024.

## Introduction

**Note:** This project is currently in early stages and has not yet been widely tested in production environments. Documentation is evolving, and contributions and feedback are highly encouraged to help improve stability and functionality. [Version 1 Sucks, But Ship It Anyway](https://blog.codinghorror.com/version-1-sucks-but-ship-it-anyway/)

SimpleAuth for .NET is a **free and open-source** solution designed to **simplify** the implementation of user and role-based authentication and authorization in .NET WebApi and client applications.

The goal of this project is to provide small to medium-sized businesses and organizations with a straightforward, self-hosted, and cost-effective infrastructure for identity management. Built for a .NET 9 WebAPI backend, SimpleAuth serves as an alternative to expensive commercial products and Microsoft's [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity) framework, which can be complex.

- [Get Started](./documentation/getting-started.md) - Setup Auth on your local computer in 10 minutes!
- [Why Choose SimpleAuth](#why-choose-simpleauth) - Hear us out...
- [Using SimpleAuth](#using-simpleauth) - Common programming use-cases in the API.
- SimpleAuth Features
  - [General Features](#general-features) - Features applicable to both local account and SSO authentication methods.
  - ["Local Accounts" Security Features](#local-accounts-security-features) - Security features built into local accounts.
- [Screenshots](#screenshots) - Look at some screenshots from one of the client apps.
- [Frontend Support](#frontend-support) - Sample apps provided for Angular, React and VueJS


To get a sample app with authentication / authorization up and running on your local computer in less than 10 minutes, follow the **[Getting Started Guide](./documentation/getting-started.md)**. Just curious? Have a look at [some screenshots](./documentation/angular-app.md) or check out the [the documentation](./documentation/README.md) to read more.

---

## Why Choose SimpleAuth?

- **Simplifies Auth Implementation:** Reduces the complexity of setting up user authentication and authorization.
- **Free and Open-Source:** A budget-friendly alternative to expensive identity management frameworks.
- **Lightweight and Flexible:** Designed to integrate quickly without the steep learning curve of ASP.NET Core Identity.
- **SSO Support:** Use Google, Microsoft Entra ID or Facebook account credentials alongside or instead of local accounts.
- **Local Accounts:** Allows you to enable or disable local accounts and rely solely on external providers.

---

## Using SimpleAuth

Once you have SimpleAuth set up and your users / roles input, using authorization for your endpoints is easy. Simply use the control decorators that you've likely been using for years. To require a user be logged in to access an endpoint, simply use the `[Authorize]` decorator on your controllers. For example, the built in test endpoint looks like this:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// This is a test secure endpoint.
/// </summary>
[Authorize]
[Route("[controller]")]
[ApiController]
public class SecureController : ControllerBase
{
    [HttpGet("GetColorList")]
    public ActionResult<List<string>> GetColorList()
    {
        try
        {
            List<string> retVal = ["Red", "Green", "Blue"];
            return Ok(retVal);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
```

You can also restrict an endpoint to an entire role:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// This is a test secure endpoint restricted to users assigned to the Admin role.
/// </summary>
[Authorize(Role = "Admin")]
[Route("[controller]")]
[ApiController]
public class AdminController : ControllerBase
{
    [HttpGet("GetRoleList")]
    public ActionResult<List<string>> GetRoleList()
    {
        try
        {
            List<string> retVal = ["Admin", "Manager", "Staff"];
            return Ok(retVal);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}
```

You can also use the built in `User` object which is an [`IPrincipal` interface](https://learn.microsoft.com/en-us/dotnet/standard/security/principal-and-identity-objects) built into ASP.NET WebApi Controllers.

For example, to output the current username to the console, you can use:

```csharp
Console.WriteLine(User.Identity.Name)
```

To test to see if the user is logged in on an anonymous endpoint:

```csharp
if (User.Identity.IsAuthenticated)
{
    Console.WriteLine("Logged in!");
}
else
{
    Console.WriteLine("NOT logged in!");
}
```

Or to test to see if a user is in a particular role (in this case the `Admin` role):

```csharp
if (User.IsInRole("Admin"))
{
    Console.WriteLine("Logged in!");
}
else
{
    Console.WriteLine("NOT logged in!");
}
```

### Alternate Use: Security-First Approach

We recommend that you put this into your `Program.cs` file:

```csharp
// Enable [Authorize] attribute by default on all controllers:
builder.Services.AddMvc(o =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    o.Filters.Add(new AuthorizeFilter(policy));
});
```

This code will enable the "Authorize" decorator on all WebApi controllers **by default**. This security-first approach requires any endpoint that is anonymously accessible is explicitly set to do so by using the `[AllowAnonymous]` decorator on your WebApi controller instead of using the `[Authorize] decorator to restrict access. For example:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

/// <summary>
/// This is an anonymous non-secure endpoint.
/// </summary>
[AllowAnonymous]
[Route("[controller]")]
[ApiController]
public class NotSecureController : ControllerBase
{
    [HttpGet("IsUserAuthenticated")]
    public ActionResult<bool> IsUserAuthenticated()
    {
        if (User.Identity == null) return NotFound();
        return Ok(User.Identity.IsAuthenticated);
    }
}
```

---

## SimpleAuth Features

SimpleAuth is built to be simple and functional while supporting core identity management features and workflows.

### General Features

| Feature | Description |
| --- | --- |
| Quick Integration | Integrate into an existing WebAPI project with minimal effort. The API serves authenticated users with JWT access tokens for use when using authorization credentials. |
| Core Workflows Included | Out-of-the-box support for login, logout, user registration and password recovery. |
| User and Role Management | Manage users and roles without the need to build extensive UI components from scratch. |
| SSO Support | Single Sign-On support for [Google](./documentation/google-sso.md), [Microsoft Entra ID](./documentation/microsoft-sso.md) and [Facebook](./documentation/facebook-sso.md) OAuth providers with plans to add more providers in the future. |
| HTTP-only Access Tokens | By default, JWT access tokens are stored in HTTP-only cookies, which are inaccessible to Javascript, making it secure against cross-site scripting (XSS) that attempt to intercept access tokens. |
| Refresh Tokens | A refresh token improves security by limiting how often sensitive login details are sent over the internet, reducing the risk of them being exposed if an attacker intercepts a session.  |

### Local Accounts Security Features

SimpleAuth's "Local Accounts" support several features to make them more secure. Passwords are stored as salted / hashed values in a separate [database table](./documentation/the-database.md) apart from general user information.

| Feature | Description |
| --- | --- |
| Enable / Disable | Local accounts are enabled by default, but can be disabled if you want to rely solely on SSO providers for password management and authentication. |
| [Multi-Factor Authentication (MFA) Support](./documentation/mfa-support.md) | User accounts can be protected using MFA via e-mail, SMS or a OTP (Authenticator) App like Microsoft Authenticator or Google Authenticator. |
| Public Registration with Email Verification | Allow for users to create an account using a standard registration page. This option can be easily disabled if you want to control the creation of new user accounts.  |
| Configurable Password Complexity | Specify complexity options for users when they are creating their passwords. These options include `RequiredLength`, `RequiredUniqueChars`, `RequireDigit`, `RequireNonAlphanumeric` and so on. |
| Automatic Account Lock / Unlock | Automatically lock a user account after a configurable number of attempts. You can configure your API to automatically unlock an account after a certain number of minutes or require administrative intervention. |
| Prevent Password Reuse | Optionally disallow users from re-using a previously used password. |

---

### API Overview

[The API](./documentation/api.md) is organized into two primary categories:

- **[Auth (Public) Endpoints](./documentation/api.md#auth-endpoints):** Support workflows like authentication, registration, and password recovery.
- **[Administrative (Private) Endpoints](./documentation/api.md#administrative-endpoints):** Administrative endpoints for user and role management, protected by the "Admin" role.

---

### Frontend Support

SimpleAuth currently supports three client frameworks:

- [**Angular v18**](./documentation/angular-app.md) – The most polished implementation, ready to integrate.
- [**React v18**](./documentation/react-app.md) – Functional, but less refined.
- [**Vue v3**](./documentation/vue-app.md) – Functional, but early-stage.

Contributions to improve the existing front-ends or add support for other frameworks are welcome and encouraged.

#### Screenshots

The screenshots below are screenshots from the [Angular client app](./documentation/angular-app.md).

![Screenshot 1](./documentation/images/login.png)  
*Login Screen*

![Screenshot 2](./documentation/images/logged-in.png)  
*Logged-In Dashboard*

---

## Getting Started

To begin using SimpleAuth for .NET on your local machine, follow the [Getting Started Guide](./documentation/getting-started.md).

---

## Contributing

SimpleAuth is a community-driven project, and we welcome your contributions to make it even better. Here’s how you can help:

- Improve the documentation
- Refine or enhance the existing front-end implementations (Angular, React, Vue)
- Add support for other front-end frameworks or third-party SSO providers
- Report issues or suggest features to improve the project

If you’re using SimpleAuth and find it helpful, we’d also love to hear your feedback.

---

**SimpleAuth for .NET** – Simplifying identity management for .NET developers.

> **Future Vision:** SimpleAuth is intended to be a core part of [LymeStack](https://www.lymestack.com), a full-stack web application template that simplifies application development. Once fully integrated, SimpleAuth will provide seamless authentication and authorization out-of-the-box within LymeStack.
