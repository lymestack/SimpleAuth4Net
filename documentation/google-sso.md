# Google SSO Support

You can use Google SSO to authenticate your apps instead of or in addition to built in local accounts. Currently in order for Google SSO to work, a local account record needs to be explicitly created for each user. This means that a user must register or be created by an administrator prior to being able to gain access to the app. THIS IS BY DESIGN. In the future, an option will be created to allow for the local account record to be automatically generated at first login.

## Step 1: Create a Google App ID

Register your app with Google using the [Google Cloud Console](https://console.cloud.google.com/home/dashboard).

Coming Soon: Instructions to obtain a Google client id with screenshots

## Step 2: Enable Google SSO in the App Settings File

1. Open the [appsettings.json](app-settings.md) file in the `WebApi` directory and enter the `GoogleClientId` under the `AppSettings` section of the file and make sure the value of the `EnableGoogleSso` option is set to `true`.
2. Test the Google SSO functionality by navigating to the test app, logging out if necessary and logging back in using the "Sign in with Google" button on the Login page.
