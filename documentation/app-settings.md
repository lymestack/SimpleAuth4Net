# App Settings

This page documents the WebApi configuration file located in: `WebApi/WebApi/appsettings.json`

## ConnectionStrings Section

| Name | Default | Description |
| ---- | ------- | ---------- |
| DefaultConnection | See config file | SQL Server connection string. Might support other databases someday.  |

## AuthSettings Section

The `AuthSettings` section of the config file contains configuration variables that are concerned with auth and that should not be shared with clients via the [Configuration Endpoint](./api.md#configuration-endpoint).

| Name | Default | Description |
| ---- | ------- | ---------- |
| TokenSecret | TBD | Unique key used to encrypt access tokens |
| AccessTokenExpirationMinutes | `15` | The number of minutes an access token is valid for. |
| StoreTokensInCookies | `true` | If this value is true, access tokens will be stored in a HTTP only cookie, safe from CQRS attacks. If false, the access token will be in the body of the [Login API Endpoints](api.md#auth-endpoints). The access token is stored in a cookie named `X-Access-Token` and the refresh token in another cookie named `X-Refresh-Token`. |
| UseRefreshTokens | `true` | Indicates whether or not to use refresh tokens. Refresh tokens are stored in the `AppRefresh` token [database table](the-database.md). |
| RefreshTokenExpirationDays | `30` | Number of days a refresh token will remain valid. |
| MaxFailedLoginAttempts | `5`, | The maximum number of failed login attempts before automatic account lockout. |
| AccountLockoutDurationInMinutes | `15` | An account lockout will expire in this number of minutes. A value of 0 indicates no automatic unlock. Unlocks must happen manually by an Admin user. |
| PasswordComplexityOptions | TBD | A series of options defining rules for password complexity requirements. |
| PasswordResetCodeExpiresInMinutes | 30 | The number of minutes that a password reset code can be redeemed after the request for that code. |
| AllowedOrigins | TBD | An array of strings to be allowed for CORS Security. |

## EmailSettings Section

The `EmailSettings` section of the config file controls how emails are sent to users from the WebApi.

| Name | Default | Description |
| ---- | ------- | ---------- |
| NoReplyAddress | `noreply@yourdomain.com` | The "from address" used by default emails sent from the `EmailService`. |
| UseSmtpPickup | `true` | Specifies whether or not to save emails to a directory on the filesystem. If false, the SMTP server is used. This setting is to only simulate emails in development environments. In production environments, this setting would be true. |
| SmtpPickupDirectory | `C:/SmtpPickup/` | If the UseSmtpPickup setting is true, the emails will be saved to this directory. |
| EnableSsl | `true` | Specifies whether or not to use SSL/TLS when sending emails via SMTP. |
| SmtpServer | TBD | The IP Address or domain name of the SMTP Server. |
| Port | `null` | The port to use when sending emails via SMTP. |
| Username | `null` | The username to use when authenticating with the SMTP Server. |
| Password | `null` | The password to use when authenticating with the SMTP Server. |

## AppConfig Section

The `AppConfig` section of the config file contains data that is shared between both the client application and the API. This information is exposed to the client via the [Configuration Endpoint](api.md#configuration-endpoint).

| Name | Default | Description |
| ---- | ------- | ---------- |
| Environment | TBD | This config section contains information about the environment that the application is running in |
|  - Name | `Local` | The name of the environment. One of the environments in the environment list must match the name of the apiSettings:environment app setting above. |
|  - Url | `http://localhost:4200/` | The root URL for the client application. Trailing slash is required. |
|  - Api | `https://localhost:7214/` | The root URL for the API. Trailing slash is required. |
|  - Description | `This is the local instance` | The description of the environment, |
| EnableLocalAccounts | `true` | Whether or not local accounts are permitted. If true, user salted/hashed user passwords will reside in the `AppUserCredential` [database table](the-databse.md). |
| EnableMfaViaEmail | `false` | Enable multi-factor authentication using e-mail verification. |
| AllowRegistration | `true` | Whether or not to allow users to register themselves using the Register button on the login page. |
| EnableGoogle | `false` | Whether or not to allow users to sign into their account using [Google SSO](./google-sso.md) credentials. |
| GoogleClientId | TBD | Unique client ID associated with the application created in the [Google Cloud Console](https://console.cloud.google.com/). |
| RequireUserVerification | `false` | Indicates whether or not email verification is required for a user to be able to login. |

The remaining settings in the file are settings included by default from the Microsoft WebApi template.
