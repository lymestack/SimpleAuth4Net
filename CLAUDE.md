# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Development Commands

### Running the Application

- **Quick Start (Backend + Angular)**: Run `Run.bat` in the root directory to start both the .NET API and Angular app
- **Backend API Only**: `cd WebApi\WebApi && dotnet watch run`
- **Angular App**: `cd ng-app && npm install && npm start`
- **React App**: `cd react-app && npm install && npm start`
- **Vue App**: `cd vue-app && npm install && npm run serve`

### Frontend Development

**Angular** (ng-app/):

- `npm start` - Start development server
- `npm run build` - Build for production
- `npm test` - Run tests

**React** (react-app/):

- `npm start` - Start development server
- `npm run build` - Build for production
- `npm test` - Run tests

**Vue** (vue-app/):

- `npm run serve` - Start development server
- `npm run build` - Build for production
- `npm run lint` - Run linting

## Architecture Overview

### Backend (.NET 8 WebAPI)

The backend is a modular authentication/authorization system built on .NET 8:

**Core Components:**

- **SimpleAuthNet Library** (`WebApi/SimpleAuthNet/`): Reusable authentication library containing all auth logic, models, and services
  - `SimpleAuthServiceExtensions.cs`: Extension methods for configuring auth services (JWT, CORS, rate limiting, etc.)
  - `Data/SimpleAuthContext.cs`: Entity Framework database context
  - `Models/`: Domain models (AppUser, AppRole, AppUserRole, etc.)
  - `EmailService.cs` & `SmsService.cs`: Communication services for MFA
  - `Models/Config/SimpleAuthMode.cs`: Enum — Standalone, IdentityProvider, RelyingApp
  - `Models/Config/AuthSettings.cs`: SSO properties — Mode, IdentityProviderUrl, CookieDomain, ReturnUrlParameter
  - `Data/IRoleDbContext.cs`: Interface for local role lookup (AppUserRoles, AppRoles)
  - `LocalRoleClaimsTransformer.cs`: IClaimsTransformation that enriches JWTs with local roles in RelyingApp mode

**WebAPI Project** (`WebApi/WebApi/`):

- `Program.cs`: Minimal API setup using SimpleAuth extension methods
- `Controllers/`:
  - `AuthController.cs`: Public auth endpoints (login, register, password reset, MFA, SSO)
  - `AppUserController.cs` & `AppRoleController.cs`: Admin endpoints for user/role management
  - `SecureController.cs`: Example protected endpoint

**Key Patterns:**

- JWT-based authentication with refresh tokens
- HTTP-only cookies for token storage (configurable)
- Rate limiting on sensitive endpoints
- Audit logging for security events
- Support for multiple SSO providers (Google, Microsoft, Facebook)
- MFA via email, SMS, or OTP authenticator apps

### Operating Modes

SimpleAuthNet supports three operating modes, configured via `AuthSettings:Mode` in appsettings.json. The unified `AddSimpleAuth(configuration)` method handles all mode-specific wiring.

- **Standalone** (default): Full auth endpoints, role claims in JWT, no cross-app SSO
- **IdentityProvider**: Issues identity-only JWTs (sub claim, no roles), cookies scoped to CookieDomain
- **RelyingApp**: Validates tokens, resolves roles locally, redirects unauthenticated browser requests to IdentityProvider

### Frontend Architecture

All three frontend apps follow similar patterns:

**Angular App** (Most Complete):

- Feature modules: `account/`, `auth-admin/`
- Lazy-loaded routes for performance
- Material Design components
- Auth service with interceptors for token management
- Admin UI for user/role management

**React & Vue Apps** (Basic Implementation):

- Auth service for login/logout
- Protected route examples
- Basic registration and login forms

### Database

- SQL Server or SQLite support
- Tables: AppUsers, AppRoles, AppUserRoles, AppUserCredentials, AppRefreshTokens, AppUserPasswordHistory
- Password history tracking for reuse prevention
- Separate credential storage for security

### Connecting to the Database

The shared dev SQL Server is `192.168.50.42\SQLEXPRESS`, database `SimpleAuth`. The
default `appsettings.json` connection string uses Windows Integrated Security against
`.\SQLEXPRESS` (for Windows dev machines); from macOS we connect with SQL auth instead.

**Credentials live in `WebApi/WebApi/appsettings.Development.local.json`** — a
gitignored file that is NEVER committed. It overrides `ConnectionStrings:DefaultConnection`
(loaded last in `Program.cs`). Do NOT put the username/password anywhere else in the repo
(not in this file, not in tracked appsettings). Read the connection details from that file
when you need to connect. If the file is missing or still contains `REPLACE_ME`
placeholders, stop and ask — the dev SQL user must be provisioned first.

**Running SQL scripts via pymssql (macOS):** the Go-based `sqlcmd` can't resolve
`\SQLEXPRESS` named instances from macOS — use `pymssql`. Split on `GO` batch separators
(pymssql doesn't support them natively); strip comment-only lines within a batch but do NOT
drop whole batches that start with `--`. Pull `server` / `user` / `password` / `database`
from `appsettings.Development.local.json`:

```python
import pymssql, re
conn = pymssql.connect(server='192.168.50.42\\SQLEXPRESS', user='<from-dev-local-json>',
                       password='<from-dev-local-json>', database='SimpleAuth')
cursor = conn.cursor()
with open('migrations/2026-07-security-hardening.sql') as f:
    sql = f.read()
for batch in re.split(r'^\s*GO\s*$', sql, flags=re.MULTILINE | re.IGNORECASE):
    if not [l for l in batch.split('\n') if l.strip() and not l.strip().startswith('--')]:
        continue
    try:
        cursor.execute(batch); conn.commit()
    except Exception as e:
        print(f"ERROR: {e}"); conn.rollback()
conn.close()
```

### Configuration

Primary configuration in `WebApi/WebApi/appsettings.json`:

- Database connection strings
- JWT settings and token expiration
- SSO provider configuration
- Password complexity rules
- Rate limiting settings
- CORS allowed origins
- Audit logging options

## Security Considerations

- Passwords are salted and hashed
- Account lockout after failed attempts
- Password complexity enforcement
- Prevention of password reuse
- MFA support (email/SMS/OTP)
- Rate limiting on auth endpoints
- Audit logging of security events

## Git Commits

**CRITICAL** When forming Git commit messages, never mention Claude or Anthropic.

## Custom Command: "update docs"

When the user says **"update docs"** or **"update documentation"**, you should:

1. Review all pending git changes (`git status` and `git diff`) and all uncommitted changes.
1. Update relevant spec files in `/documentation/` folder based on the changes made.
1. Provide a summary of documentation updates made.
1. Update any relevant info on the main `/README.md` file. Only high level information goes into this file so technical details should be omitted.

This helps keep app store submissions streamlined by ensuring documentation stays current with code changes.

### Documentation Reminder

**IMPORTANT**: When completing a feature or making significant changes, proactively remind the user to run the "update docs" command before committing. Use prompts like:

- "The feature looks complete! Would you like me to run 'update docs' before we commit?"
- "Before we wrap up, should I update the documentation with these changes?"
- "Ready to commit? Don't forget we can run 'update docs' first to keep everything in sync."

This ensures documentation stays current without being overly automatic.

### ZOMBIE Comments

Code blocks that are prefaced with a ZOMBIE prefix denotes some commented code that is commented for a reason, maybe because it might be re-implemented in some part. So don't delete these ZOMBIE commented code blocks when editing code. They might actually add some value in the future.

### Error Handling

**IMPORTANT**: Do NOT use try/catch blocks in API controller endpoints. The application has global error handling middleware that catches exceptions. Let exceptions bubble up naturally.

### DateTime Handling

**IMPORTANT**: Always use `DateTime.UtcNow` (not `DateTime.Now`) when saving dates to the database in DATETIME or DATETIME2 fields. DATETIMEOFFSET fields don't need this.

### Database Schema

No EF migrations. Schema is managed via SQL scripts (DbUp or manual).

`CreateDb.sql` builds a fresh database with the current schema. Changes to the schema
must ALSO be delivered as an idempotent, guarded migration script under `migrations/`
(named `YYYY-MM-<slug>.sql`) so **existing** SimpleAuth databases can be upgraded — a
`CreateDb.sql` edit alone only helps new instances. Migration scripts guard every
statement (`IF NOT EXISTS (SELECT 1 FROM sys.columns ...)`) so they are safe to re-run.
See `migrations/2026-07-security-hardening.sql`.

### Shared Models

Prefer shared models between API and client. Use TypeGen to auto-generate TypeScript models from C# DTOs.
