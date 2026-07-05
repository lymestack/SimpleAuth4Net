# E2E Security Test Report — SimpleAuth4Net

**Date:** 2026-07-04 · **Branch:** master (commits d815277 + 5d5ce29)
**Env:** macOS, API on Kestrel `http://localhost:5218` (env `Local`, `AuthSettings.Mode=Standalone`), Angular `ng serve` on `http://localhost:4200`, live DB `SimpleAuth` on `192.168.50.42\SQLEXPRESS`.
**Method:** UI driven with Playwright CLI for happy-path login + registration; short-code / enumeration / lockout mechanics exercised via direct calls to the same `/Auth/*` Kestrel endpoints; every server-side effect verified in SQL Server via pymssql.

**Config observed:** `MaxFailedLoginAttempts=3`, `AccountLockoutDurationInMinutes=15`, rate limit 5/60s, `RequireUserVerification=false`, **all MFA disabled** (`EnableMfaViaEmail/Sms/Otp=false`).

## Environment plumbing note (not a product bug)
The SPA (`ng-app/src/main.ts`) hardcodes `useIIS=true`, so it fetches `AppConfig` from `http://localhost/SimpleAuthNet/api/` (IIS) and the returned `environment.api` points there too. On macOS there is no IIS, so out of the box the SPA can't reach the Kestrel API and shows "Unable to retrieve app configuration data." I worked around it non-invasively by having Playwright mock the `AppConfig` fetch to point `api` at `http://localhost:5218/` — **no source or config files were changed.** Login/deviceId/cookies/CORS all worked against Kestrel once repointed.

---

## Results

### 1. Happy-path login + rehash-on-login (Argon2id migration) — KEY TEST → **PASS (mechanism); mjoseph not exercised (credential mismatch)**
- The provided credential **`mjoseph@iadev.net` / `Password123$` does not authenticate.** Verified two ways: (a) live login returned `INVALID_CREDENTIALS` and locked after 3 tries; (b) offline replication of the exact legacy verifier — `HMACSHA512(salt).ComputeHash(utf8(pw))`, byte-identical to both the original `d815277^` register code and the current `SimpleAuthPasswordHasher` legacy branch — did **not** reproduce mjoseph's stored 64-byte hash for `Password123$` (or common variants). This is a **test-credential mismatch, not a code defect.** mjoseph's stored hash is legacy HMAC-SHA512 (64 bytes) and was left **byte-for-byte unchanged**.
- Because I would not repeatedly lock the real dev account, I proved the migration on a **controlled legacy row**: took a throwaway Argon2id account, overwrote its credential with a legacy HMAC-SHA512 hash of a known password (exact old algorithm), then logged in through the **UI**:
  - **BEFORE:** `PasswordHash` = 64 bytes, binary prefix (legacy HMAC-SHA512).
  - **UI login succeeded** → landed on authed page ("You are logged in as …", Log out + Test Secure Resource).
  - **AFTER:** `PasswordHash` = `$argon2id$v=19$m=19456,t=2,p=1$…` (100 bytes); `FailedLoginAttempts=0`, `FailedVerificationAttempts=0`.
  - The app authenticating a hash I built offline also **positively validates** the replication above (so the mjoseph conclusion is sound).
  - Evidence: `s1-rehash-loggedin.png`, `s1-login-flow.webm`.

### 2. Brute-force lockout → **PASS. Threshold observed = 3.**
Wrong-password attempts on a throwaway account (same endpoint the UI hits):
- Attempt 1 & 2 → **identical** generic `{"error":"INVALID_CREDENTIALS","message":"AppUser or password was invalid."}` (HTTP 400).
- Attempt 3 → `"The account has been locked due to multiple failed login attempts."` (HTTP 401).
- **DB:** `Locked=1`, `FailedLoginAttempts` incremented, `LockoutEndTime` = now + 15 min.
- Correct password while locked → `"The account is locked."` (HTTP 401) — lockout enforced even with valid credentials.
- The same lockout was observed via the **UI** as a browser alert during the mjoseph episode.

### 3. Account enumeration → **PASS on login; PARTIAL on forgot-password (see bug)**
- **(a) Login:** non-existent user and existing-user-wrong-password return the **identical** body `{"error":"INVALID_CREDENTIALS","message":"AppUser or password was invalid."}` (400). No user-found oracle. ✔
- **(b) Forgot-password:** by design returns a uniform `"If an account exists for that email, a password reset code has been sent."` — **fake email → HTTP 200** with that message. **BUT real email → HTTP 500** in this env (see Bugs). The uniform-message design is correct; the exception on the account-exists path leaks existence via status code.
- **Timing (rough):** non-existent `~0.050s` vs existing-wrong-password `~0.038s` — comparable, confirming the anti-enumeration timing equalizer (Argon2 dummy verify on missing users). ✔

### 4. Password-reset code lockout → **PASS**
Seeded a known reset code, submitted wrong codes:
- Wrong 1 → `FailedVerificationAttempts=1`; Wrong 2 → `=2`; Wrong 3 → token **invalidated** (`VerifyTokenUsed=1`, counter reset to 0). Each response the generic `"Invalid or expired verification token."` (400).
- Subsequently submitting the **correct** code → still rejected (token was invalidated). Per-account cap works and is IP-independent (threshold = `MaxFailedLoginAttempts` = 3). ✔

### 5. Registration → Argon2id from birth → **PASS**
Registered a fresh account via the **UI**; stored `PasswordHash` immediately = `$argon2id$v=…` (100 bytes). No legacy hash ever written for new accounts. Evidence: `s5-register-filled.png`. (Minor: a post-register auto-login attempt returned 401; registration itself succeeded and the DB row was correct.)

### 6. OTP-only routing → **NOT EXERCISED (MFA disabled in this environment)**
`EnableMfaViaOtp=false` and no account has a `TotpSecret`, so this couldn't be driven at runtime. **Code-level confirmation only:** the Login gate now enforces OTP when it is the requested or only factor and returns `{ mfaRequired:true, redirectUrl:"/account/verify-mfa-otp" }`; `login.component.ts` routes to `/account/verify-mfa-otp` when the server's `redirectUrl` contains `otp` (honoring the server redirect rather than re-deriving). Fixes are present but not runtime-validated.

### 7. Refresh-token reuse → **PARTIAL PASS (rotation confirmed; full replay-revocation not exercised)**
- **DB confirmed:** after the UI session refreshed, the `AppRefreshToken` row had `PreviousToken` **populated** (prior token recorded on rotation), DeviceId matching the browser. ✔
- **Code confirmed:** presenting a consumed/previous token triggers reuse detection → `RemoveRange` of the **entire token family** for the user + a `RefreshTokenReuseDetected` audit event.
- Full replay was **not** exercised end-to-end: refresh tokens are stored SHA-256-hashed at rest, so the raw prior token isn't recoverable from the DB to replay. Consistent with the audit's "not UI-observable" caveat.

---

## Bugs / findings

1. **Forgot-password enumeration oracle via unhandled email-send exception (real, env-triggered).**
   `ForgotPassword` calls `SendVerificationEmail` **synchronously inside the account-exists branch**. In this env the SMTP pickup dir is a Windows path (`C:/SmtpPickup/`, invalid on macOS) so the send throws → global middleware returns **HTTP 500** for real accounts, while non-existent accounts return **HTTP 200**. That status difference re-introduces the exact account-enumeration oracle the batch set out to close. Even in production, any transient email-delivery failure would leak existence the same way. **Recommend:** wrap/defer the email send (fire-and-forget or catch) so the response is uniform (200 + generic message) regardless of send success. `security-audit-findings.md` M7 remediation should note this exception path.

## Notable / follow-ups
- **Test credential `Password123$` for mjoseph is stale/incorrect** — please re-confirm the real password. The rehash migration itself is proven working (scenario 1).
- Dev-only code disclosure (L6) is active: `ForgotPassword`/MFA responses append `Development ONLY: <code>` because env name contains "Local". Expected in dev.
- SPA `main.ts useIIS=true` + `AppConfig.api` pointing at IIS makes the Angular app unusable against Kestrel on macOS without a repoint — worth a dev-ergonomics note (not a security issue).

## Housekeeping
- **mjoseph restored** to its exact original state (unlocked, all counters 0, legacy hash unchanged). It was locked once during the credential-mismatch discovery and immediately restored.
- Throwaway test account(s) **deleted** (AppUser + child rows).
- No source or tracked config files modified; no commits. DB credentials never printed.
- **Servers:** API and `ng serve` are **user-managed** (you're running them manually) — left running; stop them at your convenience. Playwright browser closed.
- Evidence in scratchpad: `s1-login-filled.png`, `s1-rehash-loggedin.png`, `s5-register-filled.png`, `s1-login-flow.webm`.
