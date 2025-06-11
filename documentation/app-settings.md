# App Settings

This page documents the WebApi configuration file located in: `WebApi/WebApi/appsettings.json`

## ConnectionStrings Section

| Name | Default | Description |
| ---- | ------- | ---------- |
| DefaultConnection | See config file | SQL Server connection string. Might support other databases someday.  |

## AppConfig Section

The `AppConfig` section of the config file contains data that is shared between both the client application and the API. This information is exposed to the client via the [Configuration Endpoint](api.md#configuration-endpoint).

| Name | Default | Description |
| ---- | ------- | ---------- |
| Environment | See below | This config section contains information about the environment that the application is running in. |
|  - Name | `Local` | The name of the environment. One of the environments in the environment list must match the name of the apiSettings:environment app setting above. |
|  - Url | `http://localhost:4200/` | The root URL for the client application. Trailing slash is required. |
|  - Api | `https://localhost:7214/` | The root URL for the API. Trailing slash is required. |
|  - Description | `This is the local instance` | The description of the environment, |
| SimpleAuth | See Below | Contains SimpleAuth data that is shared between both the client application and the API. |

## AppConfig:SimpleAuth Section

These settings are publicly accessible by client applications and provide data needed to enable/disable certain UI as well as how to behave with SSO redirects.

| EnableLocalAccounts | `true` | Whether or not local accounts are permitted. If true, user salted/hashed user passwords will reside in the `AppUserCredential` [database table](the-databse.md). |
| EnableMfaViaEmail | `false` | Enable [multi-factor authentication](./mfa-support.md#instructions-to-enable-mfa-via-email) using e-mail verification. |
| EnableMfaViaSms | `false` | Enable  [multi-factor authentication](./mfa-support.md#instructions-to-enable-mva-via-sms--text) using SMS text message. |
| EnableMfaViaOtp | `false` | Enable  [multi-factor authentication](./mfa-support.md#instructions-to-enable-mfa-via-a-one-time-password-otp-authenticator-app) using OTP Authenticator apps. |
| AllowRegistration | `true` | Whether or not to allow users to register themselves using the Register button on the login page. |
| RequireUserVerification | `false` | Indicates whether or not email verification is required for a user to be able to login. |
| ResendCodeDelaySeconds | `60` | The number of seconds that must pass before allowing the user to send themselves a new MFA code (email and SMS MFA methods). |
| EnableGoogleSso | `false` | Whether or not to allow users to sign into their account using [Google SSO](./google-sso.md) credentials. |
| GoogleClientId | TBD | Unique client ID associated with the application created in the [Google Cloud Console](https://console.cloud.google.com/). |
| EnableFacebookSso | `false` | Whether or not to allow users to sign into their account using [Facebook SSO](./facebook-sso.md) credentials. |
| FacebookAppId | TBD | Unique app ID associated with the application created in the [Facebook Developers](https://developers.facebook.com/) page. |
| EnableMicrosoftSso | `false` | Whether or not to allow users to sign into their account using [Microsoft Entra ID SSO](./microsoft-sso.md) credentials. |
| MicrosoftTenantId | TBD | The Microsoft Tenant ID associated with the registered application in Azure. |
| MicrosoftClientId | TBD | The Client ID associated with the registered application in Azure. |
| MicrosoftRedirectUri | `http://localhost:4200/account/microsoft-login-callback` | The redirect URL that will be used after a successful sign using valid Entra ID credentials. |

## AuthSettings Section

The `AuthSettings` section of the config file contains configuration variables that are concerned with auth and that should not be shared with clients via the [Configuration Endpoint](./api.md#configuration-endpoint).

| Name | Default | Description |
| ---- | ------- | ---------- |
| TokenSecret | TBD | Unique key used to encrypt access tokens. |
| AccessTokenExpirationMinutes | `15` | The number of minutes an access token is valid. |
| StoreTokensInCookies | `true` | If this value is true, access tokens will be stored in a HTTP only cookie, safe from CQRS attacks. If false, the access token will be in the body of the [Login API Endpoints](api.md#auth-endpoints) response. The access token is stored in a cookie named `X-Access-Token` and the refresh token in another cookie named `X-Refresh-Token`. |
| UseRefreshTokens | `true` | Indicates whether or not to use refresh tokens. Refresh tokens are stored in the `AppRefresh` token [database table](the-database.md). |
| RefreshTokenExpirationDays | `30` | Number of days a refresh token will remain valid. |
| MaxFailedLoginAttempts | `5`, | The maximum number of failed login attempts before automatic account lockout. |
| AccountLockoutDurationInMinutes | `15` | An account lockout will expire in this number of minutes. A value of 0 indicates no automatic unlock; Unlocks must happen manually by an Admin user. |
| PasswordComplexityOptions | TBD | A series of options defining rules for password complexity requirements. |
| PasswordResetCodeExpiresInMinutes | 30 | The number of minutes that a password reset code can be redeemed after the request for that code. |
| AllowedOrigins | TBD | An array of strings to be allowed for CORS Security. |
| SmsProviderApiKey | TBD | A string value containing the API key for SMS MFA Authentication. |
| OtpIssuerName | `MyAppName` | Replace this value with the name of your application that will appear in an Authenticator app. |
| PreventReuseOfPreviousPasswords | `true` | Enable or disable the allowance for the reuse of previously used passwords when changing a password. |
| FacebookAppSecret | TBD | The app secret for your Facebook integration. |
| MicrosoftClientSecret | TBD | The app secret for your Microsoft Entra ID integration. |

## RateLimit Section

The `RateLimit` section of the config file defines how rate limiting is applied to protected WebApi endpoints. This helps guard against brute-force attacks and abusive clients by throttling requests based on a fixed window policy.

| Name | Default | Description |
| ---- | ------- | ----------- |
| PermitLimit | `5` | The maximum number of requests allowed during each time window. If this limit is exceeded, further requests will be rejected or queued. |
| WindowInSeconds | `60` | The length of the time window (in seconds) used for rate limiting. The permit count resets after this window expires. |
| QueueLimit | `2` | The number of additional requests that may be queued after the limit is reached. If the queue is full, requests will be rejected with an HTTP 429 (Too Many Requests) status. |
| EnableRateLimitRejectionLogging | `false` | If set to true, a warning will be logged each time a client exceeds the rate limit. This can help with early detection of abuse patterns or misconfigured clients. |

**Example Usage**:
If `PermitLimit` is set to `5`, `WindowInSeconds` is `60`, and `QueueLimit` is `2`, a client can make up to 5 requests per minute. Two additional requests may be queued, after which further requests are rejected.

To enable rate limiting on specific endpoints, use the `[EnableRateLimiting("fixed")]` attribute.

## EmailSettings Section

The `EmailSettings` section of the config file controls how emails are sent to users from the WebApi.

| Name | Default | Description |
| ---- | ------- | ---------- |
| NoReplyAddress | `noreply@yourdomain.com` | The "from address" used by default emails sent from the `EmailService`. |
| UseSmtpPickup | `true` | Specifies whether or not to save emails to a directory on the filesystem. If false, the SMTP server is used. This setting is to only simulate emails in development environments. In production environments, this setting would be false. |
| SmtpPickupDirectory | `C:/SmtpPickup/` | If the UseSmtpPickup setting is true, the emails will be saved to this directory. |
| EnableSsl | `true` | Specifies whether or not to use SSL/TLS when sending emails via SMTP. |
| SmtpServer | TBD | The IP Address or domain name of the SMTP Server. |
| Port | `null` | The port to use when sending emails via SMTP. If null, the default port 25 will be used. |
| Username | `null` | The username to use when authenticating with the SMTP Server. |
| Password | `null` | The password to use when authenticating with the SMTP Server. |

## SMS Settings Section

The `SmsSettings` section of the config file controls how emails are sent to users from the WebApi.

| Name | Default | Description |
| ---- | ------- | ---------- |
| Provider | `Twilio` | The provider used for SMS messaging. Currently only Twilio is supported. |
| AccountSid | TBD | Your account ID with the SMS provider. |
| AuthToken | TBD | The secret token used to authenticate with the SMS provider. |
| SenderNumber | TBD | The phone number that will be used to send SMS messages. |
| LogDirectory | `.\Logs\Sms` | The folder where time-stamped JSON log files will be stored. |
| SimulateSend | `true` | If you don't have a Twilio account, you can set this value to `true` to only save messages to the log folder and skip the API call. |

The remaining settings in the file are settings included by default from the Microsoft WebApi template.
