# SimpleAuth for .NET

**Note:** This project is an alpha version and this documentation is in draft form.

SimpleAuth for .NET is a free and open-source project intended to solve the headache of getting user and role based authorization / authentication up and running in your .NET WebApi and client application.

The mission of this project is to provide small to medium sized businesses and organizations with a free, simple and fast-tracked infrastructure to support identity in an application powered by a .NET 9 WebApi backend to serve as a viable alternative to [expensive commercial products](./documentation/background.md#other-commercial-providers) and the complicated Microsoft's [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity) framework.

## Why SimpleAuth?

- Implementing Auth generally sucks
- Alternative to Microsoft's ASP.NET Core Identity framework, which has a high learning curve
- Free alternative to expensive Identity Management for small to medium sized entities
- Get started with the real work in your app and stop fumbling around with auth implementations

## What does SimpleAuth offer?

SimpleAuth is designed to be just that... simple...

### The API

[The API](./documentation/api.md) offers two classifications of endpoints:

- Authentication / Registration / Password Reset - Public / Anonymous Endpoints
- Administration Endpoints (Private) - Protected by an "Admin" role

### Client Frontend

So far, there are three client applications that I have authenticating with the API - Angular v18, React v18, and Vue v3. The Angular version of the front-end is more polished whereas the React and Vue apps aren't quiet as refined just yet. Contributions to support other front-end tech stacks are encouraged as well as improvements to the existing front-ends, especially in the React and Vue spaces.

## How do I get started with SimpleAuth?

Find out more by checking out the docs. To get started with SimpleAuth for .NET on your local computer visit the  [Getting Started Guide](./documentation/index.md) for a guided walkthrough.
