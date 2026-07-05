# Security Audit — SimpleAuth4Net

**Scope:** `ng-app/` (Angular) and `WebApi/WebApi/` + `WebApi/SimpleAuthNet/` (.NET 8 auth library). React/Vue apps excluded.
**Date:** 2026-07-04
**Method:** Manual code review of the auth core + `npm audit` on `ng-app/`. Every finding cites `file:line` and was verified against source.

## Summary

| Severity | Count |
|----------|-------|
| Critical | 0 |
| High     | 5 |
| Medium   | 8 |
| Low      | 6 |
| Info     | 3 |

**Top issues:** (1) passwords stored with a single-round HMAC-SHA512 (no KDF/work factor); (2) TOTP verification endpoint has no rate limiting *and* skips account-state checks; (3) password-reset relies on a 6-digit numeric code with no per-account attempt lockout; (4) open-redirect bypass in SSO return-URL validation; (5) `npm audit` reports 43 vulnerabilities (1 critical / 21 high).

A "Controls verified correct" section at the end lists auth protections that were checked and found sound (so implementers don't re-flag them).

---

## HIGH

### H1. Passwords stored with fast single-round HMAC-SHA512 (no password KDF)
- **Location:** `WebApi/WebApi/Controllers/AuthController.cs:78-80` (register), `:527-529` (reset), `:1192-1199` (`CheckPassword`)
- **Description:** Password hashing uses `new HMACSHA512()` with a random key as the "salt" and a single hash pass. This is a fast cryptographic hash, not a password-hardening function. There is no work factor / iteration count / memory cost. If the `AppUserCredential` table is ever exfiltrated, an attacker can brute-force passwords offline at billions of guesses/sec. For an authentication *library* this is a core weakness.
- **Recommendation:** Switch to a purpose-built password hash: ASP.NET Core `PasswordHasher<T>` (PBKDF2, tunable iterations) or, preferably, `Argon2id`/`bcrypt` via a maintained library. Migrate existing hashes lazily on next successful login. Keep the per-user random salt.
- **Confidence:** Confirmed

### H2. TOTP endpoint `VerifyAuthenticatorCode` has no rate limiting and skips account-state checks
- **Location:** `WebApi/WebApi/Controllers/AuthController.cs:714-742`
- **Description:** Two problems on the same anonymous endpoint that issues a full JWT:
  1. **No rate limit** — unlike every other auth endpoint it has no `[EnableRateLimiting("fixed")]`, and there is no global default limiter (policies are opt-in; `Program.cs:28`). A 6-digit TOTP (1,000,000 space, with a ±1 step verification window) can be brute-forced with unlimited attempts.
  2. **No account-state gate** — `Login` enforces `!Active` (`:161`), `HandleLockedAccounts` (`:163`), and `RequireUserVerification && !Verified` (`:166`). This endpoint checks none of them before calling `JwtGenerator`. A locked, inactive, or unverified account that has a `TotpSecret` set can still authenticate. Unlike `VerifyMfa` (which is gated by a prior status-checked `Login` that sets `PendingMfaLogin`), the TOTP path has no such gate.
- **Recommendation:** Add `[EnableRateLimiting("fixed")]`, enforce a per-account failed-attempt counter/lockout on TOTP failures, and replicate the `Active`/`Locked`/`Verified` checks (ideally via a shared helper) before issuing a token.
- **Confidence:** Confirmed

### H3. Password-reset code is a 6-digit number with no per-account attempt lockout
- **Location:** `WebApi/WebApi/Controllers/AuthController.cs:1011-1025` (`SetupVerifyToken`), consumed at `:449-471` (`ResetPassword`)
- **Description:** `SetupVerifyToken` generates `rng % 1_000_000` — a 6-digit numeric code — used for password reset, and it is valid for `VerifyTokenExpiresInMinutes` (default 30). `ResetPassword` compares the code but never increments a failure counter or locks the account on wrong codes; the only throttle is the per-IP fixed-window limiter (5/60s per `RateLimit`). A distributed / multi-IP attacker can guess the code within the 30-minute window and take over any account by email. Account-takeover impact makes this High even though the mechanism is shared with lower-impact flows.
- **Recommendation:** Use a high-entropy reset token (e.g. 256-bit URL-safe random) delivered as a link, not a 6-digit code; or, if a short code must stay, add a strict per-account attempt counter that invalidates the token after 3–5 failures, shorten expiry, and bind attempts to the account rather than the IP.
- **Confidence:** Confirmed

### H4. Open redirect / SSO return-URL host bypass in `IsAllowedReturnUrl`
- **Location:** `WebApi/WebApi/Controllers/AuthController.cs:1239-1261` (esp. `:1248`)
- **Description:** The `CookieDomain` check uses `uri.Host.EndsWith(cookieDomain)` with no dot boundary. With `CookieDomain=".lymestack.com"` (trimmed to `lymestack.com`), an attacker host like `evil-lymestack.com` or `lymestack.com.attacker.io`... actually `evillymestack.com` satisfies `EndsWith("lymestack.com")` and is accepted, so the post-login `returnUrl` (`:220-227`) can redirect users to an attacker origin. This is the SSO open-redirect surface called out in scope. (The `AllowedOrigins` branch at `:1253-1257` uses exact host equality and is fine.)
- **Recommendation:** Require a dot boundary: `host.Equals(domain, OrdinalIgnoreCase) || host.EndsWith("." + domain, OrdinalIgnoreCase)`. Also confirm the URL scheme is http/https.
- **Confidence:** Confirmed

### H5. Vulnerable npm dependencies (`ws` critical/high chain + 43 total)
- **Location:** `ng-app/package-lock.json` (`npm audit`)
- **Description:** `npm audit` reports **43 vulnerabilities: 1 critical, 21 high, 20 moderate, 1 low.** The `ws` (8.0.0–8.20.1) chain feeding `engine.io`/`socket.io-adapter` includes high-severity uninitialized-memory-disclosure and memory-exhaustion DoS advisories. Most of the 1,040 dependencies are dev-only (prod: 10), so runtime exposure is smaller than the raw count, but the counts are high enough to warrant remediation and a review of which advisories touch shipped code.
- **Recommendation:** Run `npm audit fix`; for the critical/high items requiring majors, evaluate `npm audit fix --force` in a branch and test. Add `npm audit` (or Dependabot) to CI.
- **Confidence:** Confirmed

---

## MEDIUM

### M1. OTP MFA is not enforced on the password login path
- **Location:** `WebApi/WebApi/Controllers/AuthController.cs:196`
- **Description:** After a correct password, the MFA branch fires only when `EnableMfaViaEmail || EnableMfaViaSms`. `EnableMfaViaOtp` is absent from the condition. If OTP is the *only* MFA method enabled, `Login` returns a full JWT with no second factor — MFA is silently bypassed on the primary login path. (The separate `VerifyAuthenticatorCode` endpoint exists but nothing forces the client through it.)
- **Recommendation:** Include `EnableMfaViaOtp` in the gate and route OTP users into a pending-MFA state that must be satisfied before a token is issued. Confirm the intended OTP login flow.
- **Confidence:** Confirmed (backend gap; intended UX flow needs confirmation)

### M2. MFA email/SMS codes brute-forceable — 6 digits, no per-account lockout
- **Location:** `WebApi/WebApi/Controllers/AuthController.cs:576-602` (`VerifyMfa`), token from `:1011-1025`
- **Description:** Same 6-digit numeric token as H3, here for login MFA. `VerifyMfa` does not increment failed attempts or lock the account on wrong codes; the account lockout logic (`:176-185`) applies only to password attempts. Throttling is per-IP only, so a distributed attacker can brute the second factor within the 30-min window.
- **Recommendation:** Add a per-account MFA failure counter that invalidates the code after a few attempts, shorten expiry (e.g. 5 min), and increase code length/entropy.
- **Confidence:** Confirmed

### M3. Cookie `Secure` flag derived from `Request.IsHttps` — fails behind TLS-terminating proxy
- **Location:** `WebApi/WebApi/Controllers/AuthController.cs:1103` and `:1127`
- **Description:** `Secure` is set to `HttpContext.Request.IsHttps`. Behind a reverse proxy / load balancer that terminates TLS and forwards HTTP to Kestrel, `IsHttps` is `false` unless forwarded headers are configured, so auth cookies would be issued **without** the `Secure` flag in production and could leak over plaintext. No `UseForwardedHeaders` is configured in `Program.cs`.
- **Recommendation:** Configure `ForwardedHeadersMiddleware` (respect `X-Forwarded-Proto`) or force `Secure = true` in non-Local environments regardless of `IsHttps`.
- **Confidence:** Confirmed

### M4. JWT bearer configured with `RequireHttpsMetadata = false`
- **Location:** `WebApi/SimpleAuthNet/SimpleAuthServiceExtensions.cs:135`
- **Description:** Set unconditionally (all environments). Combined with M3 this widens the window for tokens to traverse plaintext.
- **Recommendation:** Set to `true` outside Local/Development, or gate on environment.
- **Confidence:** Confirmed

### M5. Symmetric `TokenSecret` shared across SSO roles + no issuer/audience validation
- **Location:** `WebApi/SimpleAuthNet/SimpleAuthServiceExtensions.cs:137-146`; token creation `AuthController.cs:1037,1070`
- **Description:** Tokens are signed/validated with a single symmetric `TokenSecret`, and `ValidateIssuer`/`ValidateAudience` are both `false`. In IdentityProvider/RelyingApp SSO, every relying app must hold this same secret to validate tokens — which means any relying app (or anyone who obtains the secret) can *mint* valid IdP tokens for any user/role. There is no cryptographic separation between issuer and validators. Also note a minor mismatch: signing uses `Encoding.ASCII` (`:1037`) while validation uses `Encoding.UTF8` (`Extensions:140`) — equivalent for ASCII secrets only.
- **Recommendation:** For multi-app SSO use asymmetric signing (RSA/ECDSA): IdP holds the private key, relying apps validate with the public key. Enable issuer/audience validation. Align the encoding.
- **Confidence:** Confirmed (design-level; Likely-High impact in SSO deployments)

### M6. Weak-by-default / in-config signing secret pattern
- **Location:** `WebApi/WebApi/appsettings.json:32`
- **Description:** `TokenSecret` lives in `appsettings.json` (here a `REDACTED…` placeholder; the comment says to move it out for prod). Shipping the secret in a checked-in config invites deployers to leave a weak/default value, which would make all JWTs forgeable. (No live secret is leaked in this repo — the connection string uses Integrated Security with no password, and provider secrets are `REDACTED` — so this is a *pattern/weak-default* risk, not an exposed credential.)
- **Recommendation:** Load `TokenSecret` from environment variables / a secret manager; fail startup if it is missing, short, or equal to a known placeholder.
- **Confidence:** Confirmed

### M7. Username / account enumeration across multiple endpoints
- **Location:** `AuthController.cs:139-147` (`UserExists`), `:761-769` (`UserVerified`), `:160-166` login messages ("inactive"/"locked"/"not verified" vs "invalid"), `:438` (`ForgotPassword` "No user found with that email"), `:52-58` register (`USERNAME_EXISTS`/`EMAIL_EXISTS`)
- **Description:** Several endpoints let an unauthenticated caller distinguish existing from non-existing accounts and even their state (verified/locked/inactive). `UserExists` is explicitly an oracle. This aids targeted password/MFA attacks and phishing.
- **Recommendation:** Return generic responses (e.g. `ForgotPassword` always "if the account exists, an email was sent"; uniform login error). If `UserExists` is needed for UX, rate-limit hard and consider requiring auth.
- **Confidence:** Confirmed

### M8. Refresh tokens rotate without reuse detection
- **Location:** `WebApi/WebApi/Controllers/AuthController.cs:351-399`, `:1147-1176`
- **Description:** On refresh the stored token is overwritten with a new one (rotation), but a *replayed* old token is simply "not found" and rejected without any signal that a stolen token was used. There is no token-family invalidation, so a stolen refresh token used in parallel with the legitimate user goes undetected until it happens to lose the race. 30-day lifetime widens the window.
- **Recommendation:** Implement rotation with reuse detection: if a previously-rotated (now-invalid) token is presented, revoke the entire token family for that device/user and log a security event.
- **Confidence:** Likely

---

## LOW

### L1. First registrant automatically becomes Admin (deployment race)
- **Location:** `WebApi/WebApi/Controllers/AuthController.cs:119-124`
- **Description:** With `AllowRegistration: true` (default) and an empty user table, the first person to register is granted the Admin role. Between deployment and the intended admin registering, any anonymous visitor can claim admin.
- **Recommendation:** Seed the initial admin out-of-band, or gate first-admin bootstrap behind a one-time setup token / disable public registration until an admin exists.
- **Confidence:** Confirmed

### L2. CSRF: cookie-based auth with `SameSite=None` and no anti-CSRF token
- **Location:** `AuthController.cs:1110,1117,1134,1141`; auth via cookie `OnMessageReceived` (`Extensions:150-153`)
- **Description:** Access token is read from the `X-Access-Token` cookie and CORS uses `AllowCredentials`. Cross-site CSRF is *largely* mitigated in practice because `[ApiController]` + `[FromBody]` require `application/json`, which forces a CORS preflight against the explicit origin allowlist (a plain form POST hits 415). So this is defense-in-depth rather than a confirmed live vector, hence Low — but there is no anti-CSRF token as a backstop, and `SameSite=None` is used whenever `CookieDomain` is set or HTTPS is on.
- **Recommendation:** Add the double-submit / `XSRF-TOKEN` cookie + header pattern for state-changing endpoints, or set `SameSite=Strict/Lax` where cross-subdomain flows don't require `None`.
- **Confidence:** Likely (low exploitability given JSON/CORS constraints)

### L3. HTML injection into verification emails via unescaped `FirstName`
- **Location:** `WebApi/WebApi/Controllers/AuthController.cs:887-991`
- **Description:** Email bodies interpolate `userName` (= `user.FirstName`, user-controlled at registration) into HTML with `IsBodyHtml = true` and no encoding. A registrant can inject markup into the email they receive; limited impact (email is sent to the same user) but it is unsanitized stored input rendered as HTML.
- **Recommendation:** HTML-encode all user-supplied values interpolated into email templates.
- **Confidence:** Confirmed

### L4. `[innerHTML]` binding in shared card menu
- **Location:** `ng-app/src/app/shared/card-menu/card-menu.component.html:12,26,41`
- **Description:** `[innerHTML]="item.description"` bypasses Angular's text interpolation. Currently the menu items appear to be static/config-driven (low risk), but if `description` ever sources server/user data it becomes a stored-XSS sink. Angular sanitizes `innerHTML` by default, which limits (not eliminates) impact.
- **Recommendation:** Prefer text interpolation; if HTML is required, ensure the source is trusted/sanitized and never user-derived.
- **Confidence:** Needs-review

### L5. `AllowedHosts: "*"`
- **Location:** `WebApi/WebApi/appsettings.json:124`
- **Description:** Wildcard host binding permits Host-header spoofing scenarios in some setups.
- **Recommendation:** Restrict to expected hostnames in production.
- **Confidence:** Confirmed

### L6. Dev-only disclosure of verification tokens in API responses
- **Location:** `AuthController.cs:115, 200, 444, 653, 705`
- **Description:** When `AppConfig:Environment:Name` contains "Local", the actual verify/MFA/reset code (and TOTP secret) is appended to API responses. Gated to Local, but the gate is a substring match on a config string — a misconfigured non-prod-looking name would leak codes.
- **Recommendation:** Gate on `IWebHostEnvironment.IsDevelopment()` (framework-managed) rather than a config substring; ensure it can never be true in production.
- **Confidence:** Confirmed

---

## INFO

### I1. Client-side role checks are cosmetic; no Angular route guards
- **Location:** `ng-app/src/app/core/_services/current-user.service.ts:62-65` (`isInRole` reads `AppUser` from localStorage); no `*.guard.ts` files exist.
- **Description:** Role gating in the SPA is UX-only and trivially editable in the browser. This is acceptable **because** the server enforces authorization (`[Authorize(Roles="Admin")]` on `AppUserController`/`AppRoleController`, global fail-closed `AuthorizeFilter`). Noted so implementers don't rely on the client for access control.
- **Confidence:** Confirmed

### I2. `AccessTokenExpirationMinutes` 15 / refresh 30 days
- **Location:** `appsettings.json:33,36`
- **Description:** Short access-token lifetime is good; the 30-day refresh lifetime (see M8) is the main residual exposure window. Informational — tune to risk appetite.

### I3. `ClockSkew = TimeSpan.Zero`
- **Location:** `SimpleAuthServiceExtensions.cs:144`
- **Description:** Strict expiry (no skew) is a hardening positive; noted so it isn't mistaken for a bug when tokens expire exactly on time.

---

## Controls verified correct (no action needed)

These scoped items were checked and found sound — listed to prevent re-flagging:

- **`alg:none` / algorithm confusion:** mitigated — `ValidAlgorithms = { HmacSha512 }` is pinned (`Extensions:145`) and `ValidateIssuerSigningKey = true`.
- **Token storage (Angular):** access/refresh tokens are in **HttpOnly cookies**, not `localStorage`/`sessionStorage`. The SPA stores only expiry timestamps, `deviceId`, and `verifyUsername` — no tokens or secrets. (`auth.service.ts`)
- **SQL injection:** the only raw SQL (`SimpleAuthContext.cs:37-48`) is fully parameterized with `SqlParameter`; everything else is LINQ/EF.
- **Admin IDOR / broken access control:** `AppUserController` and `AppRoleController` are `[Authorize(Roles="Admin")]` at the class level, plus a global fail-closed `AuthorizeFilter` (`Extensions:185-196`). `AppUser/Me` is intentionally `[AllowAnonymous]` and returns only the caller's own record. No credentials/password hashes are serialized to clients (credentials not `Include`d; `AppUserRoles` nulled).
- **Refresh tokens at rest:** stored SHA-256-hashed (`HashToken`), generated from 64 bytes of CSPRNG (`GenerateRefreshToken`).
- **Verify/TOTP token generation:** uses `RandomNumberGenerator` (CSPRNG), not `Random`. (Entropy of the 6-digit *output* is the issue in H3/M2, not the RNG.)
- **SSO token validation:** Google uses `GoogleJsonWebSignature.ValidateAsync` with audience pinned to the configured client ID; Facebook uses `debug_token` with the app secret; Microsoft exchanges the auth code server-side. Provider secrets are not returned to the client — `AppConfigController` exposes only public client IDs (`AppId`/`TenantId`/`RedirectUri`).
- **Password reset invalidates sessions:** `ResetPassword` removes all refresh tokens for the user (`AuthController.cs:521-524`).

---

## Follow-ups / out-of-scope observations

- **React (`react-app/`) and Vue (`vue-app/`) apps were not audited** per scope. If they consume the same API, re-check their token storage (localStorage vs cookie) and MFA handling — the Angular app's HttpOnly-cookie approach is the secure reference.
- **DbUp / SQL schema scripts** were not located in this pass; confirm migration scripts don't seed default/admin credentials or weak passwords.
- **`SmsSettings` / `EmailSettings`** ship with placeholder credentials (`xxxx`, `SimulateSend: true`) — fine for the repo, but ensure real deployments source these from a secret store, not config.
