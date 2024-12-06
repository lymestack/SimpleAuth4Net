# Configuration Endpoint

The configuration endpoint is an anonymously accessible endpoint that deliver settings that describe API features to the client application.

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
    "passwordResetCodeExpiresInMinutes": 0
}
```

Here's a breakdown on the meaning of the information in this response can be found on the [App Settings](app-settings.md) page.
