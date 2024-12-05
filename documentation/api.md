# The API

The API is built using ASP.NET 9 WebApi technology reside in the `WebApi/WebApi/Controllers` directory and offers the following endpoints:

## Configuration Endpoint

HTTP GET: `/api/AppConfig`

The [configuration endpoint](configuration-endpoint.md) is an anonymously accessible endpoint that deliver settings that describe API features to the client application.

## Auth Endpoints

 The `AuthController` contains a series of anonymously accessible API endpoints that are used for authentication, registration and password resets.

### Register

 HTTP POST: `/api/Auth/Register`

 The Register endpoints allows users to register themselves with the application. An error response will be given if the `enableRegistration` option is set to `false` in the [App Settings](app-settings.md) file.

### Login

HTTP POST: `/api/Auth/Login`

The login endpoint is used to authenticate user credentials with the local accounts database. An `Ok` 200 HTTP response containing expiration dates for the access and refresh tokens will be returned. Invalid credentials will return a `BadRequest` 400 HTTP error response. Users with inactive accounts or that have not been verified will receive an `Unauthorized` 401 error response. If local accounts are disabled in the [App Settings](app-settings.md) file, a `NotFound` 404 response will be returned.

### LoginWithGoogle

HTTP POST: `/api/LoginWithGoogle`

This endpoint accepts credentials obtained from Google when using the "Sign in with Google" button on the login page of the client application. This endpoint verifies the credentials submitted with Google to make sure they are authentic and if they are An `Ok` 200 HTTP response containing expiration dates for the access and refresh tokens will be returned. Invalid credentials will return a `BadRequest` 400 HTTP error response. Users with inactive accounts or that have not been verified will receive an `Unauthorized` 401 error response. If Google SSO is disabled in the [App Settings](app-settings.md) file, a `NotFound` 404 response will be returned.

### Forgot Password

This endpoint is used to initiate the password verification process by sending the user a code to be used to reset their password. In the local development environment, the code can be seen in the response body so that email is not needed for testing. 

### Reset Password

This email accepts the password reset token that was 
If the password reset has expired or already been redeemed a `BadRequest` 400 HTTP error response will be returned. If local accounts are disabled in the [App Settings](app-settings.md) file, a `NotFound` 404 response will be returned.

## Administrative Endpoints
