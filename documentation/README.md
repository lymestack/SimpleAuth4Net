# SimpleAuth for .NET Documentation

Welcome to the SimpleAuth for .NET documentation.

>"Documentation is like sex. When it's good, it's really, really good and when it's bad it's better than nothing." - [Linus Torvalds](https://news.ycombinator.com/item?id=529479)

This documentation is a work in progress. Constructive criticism and contributions are welcome.

- [Background](background.md) - How and why this project came to exist.
- [Getting Started](getting-started.md) - Run the demo app locally in a few minutes.
- [Implementation Guide](implementation-guide.md) - Instructions on installing SimpleAuth into a new .NET WebApi project.
- [Configuration Options](app-settings.md) - Configuration options for your API and configuration endpoint.
- [API](api.md) - An explanation of the API endpoints and the sample apps.
  - [Configuration Endpoint](api.md#configuration-endpoint) - Delivers basic configuration information to your API.
  - [Auth Endpoints](api.md#auth-endpoints) - A series of API endpoints that are used for authentication and registration.
  - [Administrative Endpoints](api.md#administrative-endpoints) - A series of API endpoints used for managing users and roles or manually revoking access tokens.
- SSO Support
  - [Google](./google-sso.md)
  - [Microsoft Entra ID](./microsoft-sso.md)
  - [Facebook](./facebook-sso.md)
- Front End Implementations
  - [Angular App](angular-app.md) - Sample Angular app that authenticates with the API using either local accounts or Google SSO.
  - [React](react-app.md) - How to implement SimpleAuth for .NET into your React app.
  - [Vue](vue-app.md) - How to implement SimpleAuth for .NET into your Vue app.
- [Credits](credits.md) - Much of my hard work is based on the hard work of others.
- [Appendix: Database Schema](the-database.md) - Explanation of database entities.
- [Appendix: Multi-Factor Authentication](mfa-support.md) - How to enable multi-factor authentication (MFA) with local accounts.
