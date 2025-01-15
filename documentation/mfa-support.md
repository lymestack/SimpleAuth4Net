# Multi-Factor Authentication (MFA) Support

**NOTE:** This page is in DRAFT form.

SimpleAuth's Local Accounts support various forms of Multi-Factor Authentication for users signing in with local account credentials. The following mediums are supported for MFA:

| Method | Description |
| -- | -- |
| Email | Send a verification code to a user via SMTP to complete login. |
| SMS / Text | Use a 3rd party provider to sent SMS Text messages to complete login. Currently only Twilio is supported as a provider. |
| One-Time Password (OTP) | Use an authenticator app such as Microsoft Authenticator or Google Authenticator to provide users with a verification code to complete login. |

## Instructions to Enable MFA via Email

1. Open your [API's App Settings](./app-settings.md) file.
2. In the `AppConfig` section, make sure the `EnableMfaViaEmail` setting is set to `true`.
3. Make sure the `EmailSettings` section is configured to use SMTP by setting the `UseSmtpPickup` value to `false` and by correctly entering the domain or IP address of the `SmtpServer` as well as any applicable credentials or settings required to send mail via your SMTP server.

## Instructions to Enable MVA via SMS / Text

1. Open your [API's App Settings](./app-settings.md) file.
2. In the `AppConfig` section, make sure the `EnableMfaViaSms` setting is set to `true`.
3. In the `SmsSettings` section, enter your SMS provider account credentials. Currently only Twilio is supported as a SMS provider. Support for other providers is coming someday. You can use the `SimulateSend` functionality to capture simulated text messages to a local folder for development / demo purposes.

## Instructions to Enable MFA via a One-Time Password (OTP) Authenticator App

1. Open your [API's App Settings](./app-settings.md) file.
2. In the `AppConfig` section, make sure the `EnableMfaViaOtp` setting is set to `true`.
3. In the `AuthSettings` section, specify the `OtpIssuerName`. This is the name of the app that will appear in your authenticator app.
4. To setup the user's authenticator app, the user can scan the QR code generated from the API's Auth Controllers endpoint named `SetupAuthenticator`.
