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
        "api": "http://localhost/SimpleAuthNet/api/",
        "description": "This is the local instance."
    },
    "sessionId": "103efe17-bc1f-4348-bc97-5ca4cdd8b4eb",
    "enableLocalAccounts": true,
    "enableGoogle": true,
    "googleClientId": "753678842669-fthjsfcjhp45sas8f5tir7rsb2kt6nu6.apps.googleusercontent.com",
    "allowRegistration": true,
    "passwordResetCodeExpiresInMinutes": 0,
    "requireUserVerification": true
}
```

Here's a breakdown on the meaning of the information in this response can be found on the [App Config Section](app-settings.md#appconfig-section) of the appsettings.json file.

## Auth Endpoints

 The `AuthController` contains a series of anonymously accessible API endpoints that are used for authentication, registration and password resets.

| Route | HTTP Verb | Example | Description |
| ----- | --------- | ------- | ----------- |
| Register | POST | `/Auth/Register` | The Register endpoints allows users to register themselves with the application. An error response will be given if the `enableRegistration` option is set to `false` in the [App Settings](app-settings.md) file. |
| Login | POST | `/Auth/Login` | The login endpoint is used to authenticate user credentials with the local accounts database. An `Ok` 200 HTTP response containing expiration dates for the access and refresh tokens will be returned. Invalid credentials will return a `BadRequest` 400 HTTP error response. Users with inactive accounts or that have not been verified will receive an `Unauthorized` 401 error response. If local accounts are disabled in the [App Settings](app-settings.md) file, a `NotFound` 404 response will be returned. |
| LoginWithGoogle | POST | `/Auth/LoginWithGoogle` | This endpoint accepts credentials obtained from Google when using the "Sign in with Google" button on the login page of the client application. This endpoint verifies the credentials submitted with Google to make sure they are authentic and if they are An `Ok` 200 HTTP response containing expiration dates for the access and refresh tokens will be returned. Invalid credentials will return a `BadRequest` 400 HTTP error response. Users with inactive accounts or that have not been verified will receive an `Unauthorized` 401 error response. If Google SSO is disabled in the [App Settings](app-settings.md) file, a `NotFound` 404 response will be returned. |
| ForgotPassword | POST | `/Auth/ForgotPassword` | This endpoint is used to initiate the password verification process by sending the user a code to be used to reset their password. In the local development environment, the code can be seen in the response body so that email is not needed for testing. Emails are also saved to the `C:\SmtpPickup` (configurable via the [App Settings](app-settings.md)) directory in the local environment as well. |
| ResetPassword | POST | `/Auth/ResetPassword` | This endpoint accepts the password reset token that was emailed to the user from the ForgotPassword endpoint. If the password reset has expired or already been redeemed a `BadRequest` 400 HTTP error response will be returned. If local accounts are disabled in the [App Settings](app-settings.md) file, a `NotFound` 404 response will be returned. |
| UserVerified | GET | `/Auth/UserVerified?username=email@domain.com` | Returns a true or false value as to whether a user has been verified. |
| UserExists | GET | `/Auth/UserExists?username=email@domain.com` | Returns a true or false value as to whether a user username is available when registering. |

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
