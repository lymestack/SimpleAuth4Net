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
