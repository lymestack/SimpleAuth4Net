# SimpleAuth for .NET

**Note:** This project is currently in early alpha stages and has not yet been widely tested in production environments. Documentation is evolving, and contributions and feedback are highly encouraged to help improve stability and functionality. [Version 1 Sucks, But Ship It Anyway](https://blog.codinghorror.com/version-1-sucks-but-ship-it-anyway/)

SimpleAuth for .NET is a free and open-source solution designed to simplify user and role-based authentication and authorization in .NET WebAPI and client applications.

The goal of this project is to provide small to medium-sized businesses and organizations with a straightforward, cost-effective infrastructure for identity management. Built for a .NET 9 WebAPI backend, SimpleAuth serves as an alternative to expensive commercial products and Microsoft's [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity) framework, which can be complex.

> **Future Vision:** SimpleAuth is intended to be a core part of [LymeStack](https://www.lymestack.com), a full-stack web application template that simplifies application development. Once fully integrated, SimpleAuth will provide seamless authentication and authorization out-of-the-box within LymeStack.

---

## Why Choose SimpleAuth?

- **Simplifies Auth Implementation:** Reduces the complexity of setting up user authentication and authorization.
- **Free and Open-Source:** A budget-friendly alternative to expensive identity management tools.
- **Lightweight and Flexible:** Designed to integrate quickly without the steep learning curve of ASP.NET Core Identity.
- **SSO Support:** Use Google account credentials alongside or instead of local accounts.
- **Local Accounts Optional:** Allows you to disable local accounts and rely solely on external providers for added security.

## Features of SimpleAuth

SimpleAuth is built to be simple and functional while supporting core identity management features:

1. **Quick Integration:** Seamlessly integrate into an existing WebAPI project with minimal configuration.
2. **Core Workflows Included:** Out-of-the-box support for:
   - Login and Logout
   - User Registration
   - Password Recovery with email verification codes
3. **User and Role Management:** Manage users and roles without the need to build extensive UI components from scratch.
4. **SSO Support:** Single Sign-On support for [Google](./documentation/google-sso.md), [Microsoft Entra ID](./documentation/microsoft-sso.md) and [Facebook](./documentation/facebook-sso.md) OAuth providers with plans to add more providers in the future.

---

### API Overview

[The API](./documentation/api.md) is organized into two primary categories:

- **Public Endpoints:** Support workflows like authentication, registration, and password recovery.
- **Private Endpoints:** Administrative endpoints for user and role management, protected by the "Admin" role.

---

### Frontend Support

SimpleAuth currently supports three client frameworks:

- [**Angular v18**](./documentation/angular-app.md) – The most polished implementation, ready to integrate.
- [**React v18**](./documentation/react-app.md) – Functional, but less refined.
- [**Vue v3**](./documentation/vue-app.md) – Functional, but early-stage.

Contributions to improve the existing front-ends or add support for other frameworks are welcome and encouraged.

#### Screenshots

![Screenshot 1](./documentation/images/login.png)  
*Login Screen*

![Screenshot 2](./documentation/images/logged-in.png)  
*Logged-In Dashboard*

---

## Getting Started

To begin using SimpleAuth for .NET on your local machine, follow the [Getting Started Guide](./documentation/getting-started.md). The guide provides a step-by-step walkthrough to help you set up SimpleAuth and start building secure applications.

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

[Visit LymeStack for more solutions and templates](https://www.lymestack.com).
