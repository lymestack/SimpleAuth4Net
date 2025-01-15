# The API

The API is built using ASP.NET 9 WebApi technology and resides in the `WebApi/WebApi/Controllers` directory. It offers the following endpoints:

## Configuration Endpoint

| Route | HTTP Verb | Example | Description |
| ----- | --------- | ------- | ----------- |
| AppConfig | POST | `api/AppConfig` | The configuration endpoint is an anonymously accessible endpoint that deliver settings that describe API features to the client application. |

Here's an example of the response given from the API:

``` json
{
    "environment": {
        "name": "Local",
        "url": "http://localhost:4200/",
        "api": "http://localhost:5218/",
        "description": "This is the local instance."
    },
    "googleClientId": "REDACTED",
    "sessionId": "ec6d9062-df5d-4ec9-a20f-9b2fb1e8c64a",
    "enableLocalAccounts": true,
    "enableGoogleSso": false,
    "enableFacebookSso": false,
    "facebookAppId": "REDACTED",
    "allowRegistration": true,
    "requireUserVerification": false,
    "enableMfaViaEmail": true,
    "enableMfaViaSms": true,
    "enableMfaViaOtp": false,
    "resendCodeDelaySeconds": 30,
    "enableMicrosoftSso": false,
    "microsoftTenantId": "REDACTED",
    "microsoftClientId": "REDACTED",
    "microsoftRedirectUri": "http://localhost:4200/account/microsoft-login-callback"
}
```

Here's a breakdown on the meaning of the information in this response can be found on the [App Config Section](app-settings.md#appconfig-section) of the appsettings.json file.

## Auth Endpoints

 The `AuthController` contains a series of anonymously accessible API endpoints that are used for authentication, registration and password resets.

| Route | HTTP Verb | Example | Description |
| ----- | --------- | ------- | ----------- |
| Register | POST | `/Auth/Register` | The Register endpoints allows users to register themselves with the application. An error response will be given if the `enableRegistration` option is set to `false` in the [App Settings](app-settings.md) file. |
| Login | POST | `/Auth/Login` | The login endpoint is used to authenticate user credentials with the local accounts database. An `Ok` 200 HTTP response containing expiration dates for the access and refresh tokens will be returned. Invalid credentials will return a `BadRequest` 400 HTTP error response. Users with inactive accounts or that have not been verified will receive an `Unauthorized` 401 error response. If local accounts are disabled in the [App Settings](app-settings.md) file, a `NotFound` 404 response will be returned. |
| LoginWithFacebook | POST | `/Auth/LoginWithFacebook` | This endpoint accepts credentials obtained from Facebook when using the "Sign in with Facebook" button on the login page of the client application. This endpoint verifies the credentials submitted with Facebook to make sure they are authentic and if they are An `Ok` 200 HTTP response containing expiration dates for the access and refresh tokens will be returned. Invalid credentials will return a `BadRequest` 400 HTTP error response. Users with inactive accounts or that have not been verified will receive an `Unauthorized` 401 error response. If Facebook SSO is disabled in the [App Settings](app-settings.md) file, a `NotFound` 404 response will be returned. |
| LoginWithGoogle | POST | `/Auth/LoginWithGoogle` | This endpoint accepts credentials obtained from Google when using the "Sign in with Google" button on the login page of the client application. This endpoint verifies the credentials submitted with Google to make sure they are authentic and if they are An `Ok` 200 HTTP response containing expiration dates for the access and refresh tokens will be returned. Invalid credentials will return a `BadRequest` 400 HTTP error response. Users with inactive accounts or that have not been verified will receive an `Unauthorized` 401 error response. If Google SSO is disabled in the [App Settings](app-settings.md) file, a `NotFound` 404 response will be returned. |
| LoginWithMicrosoft | POST | `/Auth/LoginWithMicrosoft` | This endpoint accepts credentials obtained from Microsoft when using the "Sign in with Microsoft" button on the login page of the client application. This endpoint verifies the credentials submitted with Microsoft to make sure they are authentic and if they are An `Ok` 200 HTTP response containing expiration dates for the access and refresh tokens will be returned. Invalid credentials will return a `BadRequest` 400 HTTP error response. Users with inactive accounts or that have not been verified will receive an `Unauthorized` 401 error response. If Microsoft SSO is disabled in the [App Settings](app-settings.md) file, a `NotFound` 404 response will be returned. |
| Logout | GET | `/Auth/Logout` | Logs the user out by setting the auth cookie values to empty string values. |
| ForgotPassword | POST | `/Auth/ForgotPassword` | This endpoint is used to initiate the password verification process by sending the user a code to be used to reset their password. In the local development environment, the code can be seen in the response body so that email is not needed for testing. Emails are also saved to the `C:\SmtpPickup` (configurable via the [App Settings](app-settings.md)) directory in the local environment as well. |
| ResetPassword | POST | `/Auth/ResetPassword` | This endpoint accepts the password reset token that was emailed to the user from the ForgotPassword endpoint. If the password reset has expired or already been redeemed a `BadRequest` 400 HTTP error response will be returned. If local accounts are disabled in the [App Settings](app-settings.md) file, a `NotFound` 404 response will be returned. |
| UserVerified | GET | `/Auth/UserVerified?username=email@domain.com` | Returns a true or false value as to whether a user has been verified. |
| UserExists | GET | `/Auth/UserExists?username=email@domain.com` | Returns a true or false value as to whether a user username is available when registering. |
| RefreshToken | GET | `/Auth/RefreshToken?deviceId=12345` | Refreshes the JWT Access and Refresh token cookie values |
| CheckPasswordComplexity | GET | `/Auth/CheckPasswordComplexity?password=MySecretP@$$word` | Checks a password to verify that it meets complexity requirements dicated by rules set in the [Auth Settings](./app-settings.md#authsettings-section) section of the API's App Settings file. |
| VerifyAccount | POST | `/Auth/VerifyAccount` | The endpoint to use when verifying the account email address. |
| VerifyMfa | POST | `/Auth/VerifyMfa` | The endpoint to use when entering a verification code for multi-factor authentication using email or SMS. |
| SendNewCode | POST | `/Auth/SendNewCode` | The endpoint to use to send a new MFA code in case initial delivery failed when using email or SMS multi-factor authentication. |
| SetupAuthenticator | POST | `/Auth/SetupAuthenticator?username=user@domain.com` | This endpoint generates a OTP secret for a user and returns a Base-64 encoded QR code graphic to use when registering it with a OTP Authenticator app. |
| VerifyAuthenticatorCode | POST | `/Auth/VerifyMfa` | The endpoint to use when entering a verification code for multi-factor authentication using OTP. |
| WhoAmI | GET | `/Auth/WhoAmI` | This endpoint returns the name of the authenticated user. |

## Administrative Endpoints

Administrative endpoints are all locked down to be accessible by users with the `Admin` role only (unless otherwise noted).

### AppUser Controller

| Route | HTTP Verb | Example | Description |
| ----- | --------- | ------- | ----------- |
| {id} | GET | `/AppUser/123` | Gets a single record from the database |
| {id} | POST | `/AppUser/123` | Saves a single record from the database. Note that this endpoint will both Create (Insert) and Update (Modify) a record depending on the value of the incoming entity's `Id` key field value. Submit the AppUser object as the body of the HTTP request. |
| {id} | DELETE | `/AppUser/123` | Deletes a record from the database. |
| Search | POST | `/AppUser/Search?pageSize=10&pageIndex=0` | Search the AppUser database table. Submit the AppUserSearchOptions object to indicate what fields are to be searched. The `pageSize` and `pageIndex` query parameters handle pagination |
| Me | GET | `/AppUser/Me` | Anonymous endpoint - Allows the current user to get their own AppUser record. If the user is not authenticated, this endpoint will return a `null` value. |

### AppRole Controller

| Route | HTTP Verb | Example | Description |
| ----- | --------- | ------- | ----------- |
| {id} | GET | `/AppRole/123` | Gets a single record from the database |
| {id} | POST | `/AppRole/123` | Saves a single record from the database. Note that this endpoint will both Create (Insert) and Update (Modify) a record depending on the value of the incoming entity's `Id` key field value. Submit the AppRole object as the body of the HTTP request. |
| {id} | DELETE | `/AppRole/123` | Deletes a record from the database. |
| Search | POST | `/AppRole/Search?pageSize=10&pageIndex=0` | Search the AppRole database table. Submit the AppRoleSearchOptions object to indicate what fields are to be searched. The `pageSize` and `pageIndex` query parameters handle pagination |
