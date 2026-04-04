# SimpleAuth4Net — Smoke Test Plan

Before pushing the 2 unpushed commits on `master`, verify Standalone mode still works after the SSO changes.

## Prerequisites

- Database: Determine which SQL Server DB to use (`.\\SQLEXPRESS` localhost won't work from macOS — need `192.168.50.42\\SQLEXPRESS` or similar)
- Update `appsettings.Development.json` with the correct connection string
- Seed data: At least one user account (e.g., mjoseph@iadev.net / Password123\$)

## Steps

1. **Build backend**: `cd WebApi && dotnet build WebApi.sln` — must be 0 errors
2. **Run API**: `cd WebApi/WebApi && dotnet run` — should start on localhost:5218 (or configured port)
3. **Verify config endpoint**: `GET /api/AppConfig` — should return `SimpleAuth.Mode: Standalone` (or no Mode field, defaulting to Standalone)
4. **Install Angular deps**: `cd ng-app && npm install`
5. **Run Angular app**: `npm start` — should serve on localhost:4200
6. **Login flow**: Navigate to localhost:4200, log in with test credentials
   - Verify JWT is issued with `sub` claim (new) AND role claims (Standalone includes both)
   - Verify cookie has NO Domain set (Standalone doesn't scope cookies)
   - Verify `[Authorize(Roles)]` protected endpoints work
7. **Registration flow** (if AllowRegistration is true): Register a new user, verify account creation
8. **Admin endpoints**: Hit `/api/AppUser` and `/api/AppRole` with an admin token — should return data

## Expected Results

- All existing Standalone behavior unchanged
- The only additive change is the `sub` claim in JWTs
- No SSO-related config (IdentityProviderUrl, CookieDomain) should affect Standalone mode
- `AuthSettings.Mode` defaults to `Standalone` if not specified

## After Smoke Test Passes

Push to origin: `git push origin master`
