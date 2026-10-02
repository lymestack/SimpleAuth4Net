# Core drift audit (M8, part b)

Read-only audit, 2026-10-01. Base: `lymestarter` `main` at `b8ec996` (one commit ahead of `origin/main`: item K, unpushed). Apps at HEAD: `lymetimer` `main` `27d6499`, `ping` `develop` `d64f4d1`, `lymecrm` `develop` `4dd7811`, `lymedeploy` `main` `124244c`. Compared blob by blob with `git ls-tree` / `git show`. Excluded: `lymestack-admin/**`, global SCSS, `AuthController.cs`.

Areas: `WebApi/LymeStackCore`, `WebApi/SimpleAuthNet`, `ng-app/src/app/{lymestack-core,lymestack-shared,shell,account}`. No file exists only in the template in any app.

## Security and behaviour first

1. **Template logs the user out on any 401 and discards their work.** `ping` (`a083410`) and `lymetimer` (`67d5bf7`) each fixed it independently with refresh-and-replay. The server rotates refresh tokens and revokes every session on replay, so concurrent refreshes must be single-flight. Ping's version (shared `refreshTokenShared()` in `AuthService`, plus specs) is the one to upstream. Both apps still let the foreground timer call `refreshToken()` directly, outside the single-flight guard, so a timer tick racing a 401 refresh can still look like a replay. Route the timer through the shared refresh when upstreaming.
2. **`AuthService.log()` / `error()` recurse infinitely** (`this.log(...args)` calls itself) in the template **and in `SimpleAuth4Net` `origin/master`**. It is harmless only while `debug = false`. Ping fixed it (`console.log`).
3. **LymeSend returns HTTP 200 on a failed send.** Every app except `ping` logs those failures as sent. Ping (`16a46dd`) reads `success` from the body.
4. **`lymetimer` lacks item B** (`DefaultAuthLogger` drops `eventType`, so `{Label}` gets the username and `{Username}` gets the data). M4's fan-out already covers it.
5. **`lymedeploy` login detects OTP by substring** (`redirectUrl.includes('otp')`). The template's exact-match constant (`1eb7d24`) supersedes it. Low risk, but it is auth routing.
6. **Upstream trap:** lymetimer's `date-range-picker` `menuDirection` defaults to `'right'`, which flips every other app's menu. Upstream it with the default `'left'`.

## Summary

| Repo | Files | upstream | template newer | intentional | revert | needs Mike |
|---|---|---|---|---|---|---|
| `lymetimer` | 20 | 12 | 4 | 4 | 0 | 0 |
| `ping` | 38 (33 differ + 5 app-only) | 8 | 0 | 28 | 0 | 2 |
| `lymecrm` | 3 | 1 | 0 | 2 | 0 | 0 |
| `lymedeploy` | 8 | 2 | 3 | 3 | 0 | 0 |

Re-measured against the plan's 1.7 counts: `lymecrm` LymeStackCore 1 → 0 (it already has K). `lymetimer`, `ping` and `lymedeploy` each gain a `LymeSendEmailTransport.cs` row because local `main` carries K; `origin/main` does not. Every app's `account` count includes the template's broken spec import (item U8).

### Proposed upstream items

L1 items go to `SimpleAuth4Net` first, then the template.

| # | Item | Layer | Source |
|---|---|---|---|
| U1 | Refresh-and-replay on 401 with single-flight `refreshTokenShared()`, plus its two specs. Replaces the `ctr` logout. Also send the timer through it | **L1** (`core/error-interceptor.ts`, `auth.service.ts` exist in SA4N) | `ping` `a083410` (supersedes `lymetimer` `67d5bf7`) |
| U2 | `AuthService.log()`/`error()` recursion fix | **L1** | `ping` `a083410` |
| U3 | Forgot password shows the server's `message` (surfaces the Local-only code) | **L1** | `ping` `6eb68a5` |
| U4 | Login `busy` state and inline `loginError` (needs matching markup in the template's `login.component.html`) | **L1** | `ping` `b42a9d0` |
| U5 | MFA method dialog hides SMS when `simpleAuth.enableMfaViaSms` is false | **L1** | `lymetimer` `66cbd05` |
| U6 | LymeSend transport checks `success` in the response body (merge with K's attachments) | L2 | `ping` `16a46dd` |
| U7 | Shell resets `asyncLoadCount` on `NavigationEnd/Cancel/Error` (stuck loading pill) | L2 | `lymetimer` `a2c2e29`; also in LymeBooks (M8a intake overlap) |
| U8 | `register-confirmation.component.spec.ts` imports `@angular/lymestack-core/testing` (rename accident). That load error stops Karma before any spec runs. All four apps fixed it | L2 (template only; SA4N is correct) | `lymedeploy` `404199a`, `ping` `d621f52` |
| U9 | `theme.service` sets `data-bs-theme` (Bootstrap native dark mode) | L2 | `lymetimer` `852c035` |
| U10 | `sortable-column-label` icon `color: inherit` (white is invisible on light headers) | L2 | `lymetimer` `852c035`; also in LymeBooks |
| U11 | `date-range-picker` `menuDirection` input (default `'left'`) and `--ls-*` tokens for hover and empty colours | L2 | `lymetimer` `fc8e7db`, `491ba3a`, `852c035` |
| U12 | `DevController.SendTestEmail` (parameterize the "LymeTimer" strings; the UI half is in `lymestack-admin`, M9 territory) | L2 | `lymetimer` `a21deba` |
| U13 | `TableStat` `[Precision(18, 2)]` on the three MB decimals (EF truncation warning) | L2 | `lymetimer` `fc8e7db` |
| U14 | `RestService.deleteResourceWithBody` | L2 | `lymedeploy` `3bcea37` |

### Needs Mike

1. **Chrome-less route mode for the shell** (`ping` `ecc5557`: `ShellComponent.bare` input + `:host(.shell-bare)` SCSS, driven by `data.fullPage` in `AppComponent`). It is generic: any app with a public, magic-link or kiosk page needs it. **Recommendation:** upstream it to the template as an L2 feature, but after M8's LymeBooks intake, and leave ping's copy in place until then. The 2 needs-Mike rows are `shell.component.ts` and `.scss`.
2. **Shell extension slot under the brand row** (not counted as a row). `lymecrm` portals its tenant switcher into `<div id="crm-brand-outlet">`. `lymetimer` imports `TeamSwitcherComponent` from app code into `LymeStackCoreModule`, so core depends on app code. **Recommendation:** add a named slot under the brand row in the template so apps stop editing `shell.component.html` and the core module. Keep both apps' current edits as intentional until the slot exists.

## lymetimer

| File | Verdict | What differs | Evidence | Proposed action |
|---|---|---|---|---|
| `WebApi/LymeStackCore/Controllers/DevController.cs` | upstream | Adds `POST Dev/SendTestEmail` (Admin-only) | `a21deba` | U12 |
| `WebApi/LymeStackCore/LymeStackCore.csproj` | template newer | Lacks the template's nullable `<NoWarn>` list | tpl `176f819`, `40cfb93` | Routine port |
| `WebApi/LymeStackCore/Models/AppHealth/TableStat.cs` | upstream | `[Precision(18, 2)]` on 3 decimals | `fc8e7db` | U13 |
| `WebApi/LymeStackCore/Services/LymeSendEmailTransport.cs` | template newer | No attachments (item K) | tpl `b8ec996` (unpushed) | M10 K |
| `WebApi/SimpleAuthNet/Logging/DefaultAuthLogger.cs` | template newer | Item B: `eventType` missing from log args | tpl `8c201de` | M4 (already planned) |
| `WebApi/SimpleAuthNet/SimpleAuthNet.csproj` | template newer | Lacks `<NoWarn>CS8618;CS8766` | tpl `176f819` | Routine port |
| `lymestack-core/_services/theme.service.ts` | upstream | Sets `data-bs-theme` alongside `data-mode` | `852c035` | U9 |
| `lymestack-core/error-interceptor.ts` | upstream | Refresh on 401 with `isRefreshing` flag, then logout | `67d5bf7`, `66f43b7` | U1 (take ping's version, then replace this one) |
| `lymestack-core/lymestack-core.module.ts` | intentional | Imports app `TeamSwitcherComponent` | `65f8827` | Keep; see Needs Mike 2 |
| `lymestack-shared/date-range-picker/…component.html` | upstream | `menuDirection` classes and caret | `491ba3a` | U11 |
| `lymestack-shared/date-range-picker/…component.scss` | upstream | Direction classes; `--ls-*` tokens; `max-width` 175→110px (app choice; keep template's) | `fc8e7db`, `852c035` | U11 |
| `lymestack-shared/date-range-picker/…component.ts` | upstream | `@Input() menuDirection` (default `'right'`); comment arrow character only | `fc8e7db`, `d9146f0` | U11 with default `'left'` |
| `lymestack-shared/sortable-column-label/…component.scss` | upstream | Icon `color: inherit` | `852c035` | U10 |
| `shell/footer/footer.component.html` | intentional | Company name "LymeStack" | `44189f4` | Keep |
| `shell/shell.component.html` | intentional | Logo image, "LymeTimer", team switcher | `abafa58`, `65f8827` | Keep |
| `shell/shell.component.scss` | intentional | Brand icon 28px image; content padding 1rem; Prettier wrapping | `627c8d7`, `abafa58` | Keep |
| `shell/shell.component.ts` | upstream | Reset `asyncLoadCount` on nav end/cancel/error | `a2c2e29` | U7 |
| `account/register-confirmation/…spec.ts` | upstream | Correct `@angular/core/testing` import | `272af0f` | U8 |
| `account/select-mfa-method-dialog/…component.html` | upstream | SMS radio behind `*ngIf="enableMfaViaSms"` | `66cbd05` | U5 (L1) |
| `account/select-mfa-method-dialog/…component.ts` | upstream | Reads `config.simpleAuth.enableMfaViaSms` | `66cbd05` | U5 (L1) |

## ping

All 23 restyled `account/` templates and SCSS come from one deliberate PinG design pass (`b42a9d0` "Put PinG's app frame around the login page and every account screen", plus the `ping-auth-*` classes). I compared Angular bindings file by file; the only non-cosmetic changes are the inline login error and busy state, a stricter confirm-password mismatch check, and `setup-authenticator` dropping `[disabled]="!qrCodeBase64"` on its button.

| File | Verdict | What differs | Evidence | Proposed action |
|---|---|---|---|---|
| `WebApi/LymeStackCore/Services/LymeSendEmailTransport.cs` | upstream | Body `success` check (adds); attachments K (lacks) | `16a46dd`; tpl `b8ec996` | U6, then port K back |
| `lymestack-core/error-interceptor.ts` | upstream | Refresh and replay via `refreshTokenShared()`; logout only on a second 401 | `a083410` | U1 (L1) |
| `lymestack-core/simple-auth/auth.service.ts` | upstream | `refreshTokenShared()` single-flight; `log`/`error` recursion fix | `a083410` | U1, U2 (L1) |
| `lymestack-core/error-interceptor.spec.ts` (app-only) | upstream | 6 specs for refresh and replay | `a083410` | U1 |
| `lymestack-core/simple-auth/auth-refresh-shared.spec.ts` (app-only) | upstream | 3 specs for single-flight | `a083410` | U1 |
| `shell/footer/footer.component.html` | intentional | "Partners in Giving" | `4ca096d` | Keep |
| `shell/shell.component.html` | intentional | Icon and "PinG" brand | `4ca096d` | Keep |
| `shell/shell.component.scss` | needs Mike | `:host(.shell-bare)` hides all chrome | `ecc5557` | Needs Mike 1 |
| `shell/shell.component.ts` | needs Mike | `@Input() bare` + `HostBinding` | `ecc5557` | Needs Mike 1 |
| `account/account.routes.ts` | intentional | Routes nested under `AccountFrameComponent`, `fullPage: true`; same paths | `b42a9d0` | Keep |
| `account/account-frame/account-frame.component.{html,scss,ts}` (3, app-only) | intentional | PinG frame wrapper | `b42a9d0` | Keep |
| `account/forgot-password/forgot-password.component.ts` | upstream | Toast shows server `message` | `6eb68a5` | U3 (L1) |
| `account/login/login.component.ts` | upstream | `busy`, `loginError`, error handler on `userVerified` | `b42a9d0` | U4 (L1) |
| `account/register-confirmation/…spec.ts` | upstream | Correct testing import (with explanatory comment) | `d621f52` | U8 |
| `account/account.component.html` | intentional | PinG restyle | `b42a9d0` | Keep |
| `account/facebook-login-button/…{html,scss}` (2) | intentional | PinG restyle; wrapper div removed | `b42a9d0` | Keep |
| `account/forgot-password/…component.html` | intentional | PinG restyle and copy | `b42a9d0` | Keep |
| `account/login/…{html,scss}` (2) | intentional | PinG restyle; renders `loginError`, `busy` | `b42a9d0` | Keep (template markup goes with U4) |
| `account/logout/…html`, `microsoft-login-button/…html`, `microsoft-login-callback/…html` (3) | intentional | Copy and classes | `b42a9d0` | Keep |
| `account/register-confirmation/…html` | intentional | PinG restyle | `b42a9d0` | Keep |
| `account/register/…{html,scss}` (2) | intentional | PinG restyle; mismatch message only once confirm is typed | `b42a9d0` | Keep |
| `account/reset-password/…html` | intentional | PinG restyle; per-field "required" messages dropped | `b42a9d0` | Keep |
| `account/select-mfa-method-dialog/…{html,scss}` (2) | intentional | PinG restyle | `b42a9d0` | Keep |
| `account/setup-authenticator/…{html,scss}` (2) | intentional | PinG restyle; `[disabled]="!qrCodeBase64"` dropped | `b42a9d0` | Keep; consider restoring the disable |
| `account/user-settings/…html` | intentional | PinG restyle; loading state | `b42a9d0` | Keep |
| `account/username-email-input/…{html,scss}` (2) | intentional | `[hidden]` → `*ngIf`, restyle | `b42a9d0` | Keep |
| `account/verification-pending/…html`, `verify-account/…html` (2) | intentional | PinG restyle | `b42a9d0` | Keep |

## lymecrm

| File | Verdict | What differs | Evidence | Proposed action |
|---|---|---|---|---|
| `shell/footer/footer.component.html` | intentional | "Interapp Development, Inc." | `c99aa88` | Keep |
| `shell/shell.component.html` | intentional | `fa-address-book` and "Lyme CRM"; `#crm-brand-outlet` portal anchor | `defa658` | Keep; see Needs Mike 2 |
| `account/register-confirmation/…spec.ts` | upstream | Correct testing import | `ba1b8e7` | U8 |

## lymedeploy

| File | Verdict | What differs | Evidence | Proposed action |
|---|---|---|---|---|
| `WebApi/LymeStackCore/Services/LymeSendEmailTransport.cs` | template newer | No attachments (K) | tpl `b8ec996` | M10 K |
| `WebApi/SimpleAuthNet/SimpleAuthServiceExtensions.cs` | template newer | `KnownNetworks` (obsolete on .NET 10) vs `KnownIPNetworks` | tpl `1eb7d24`; app `ef71f98` ported an earlier cut | Routine port |
| `lymestack-core/_services/rest.service.ts` | upstream | `deleteResourceWithBody()` | `3bcea37` | U14 |
| `shell/footer/footer.component.html` | intentional | "IADev" | `410ad52` | Keep |
| `shell/shell.component.html` | intentional | `lymedeploy-mark.svg` and "LymeDeploy" | `728eac7` | Keep |
| `shell/shell.component.scss` | intentional | Image-sized brand icon | `728eac7` | Keep |
| `account/login/login.component.ts` | template newer | OTP detected by substring, not exact `OTP_VERIFY_REDIRECT` | tpl `1eb7d24`; app `ef71f98` | Routine port (L1 code, template already has it) |
| `account/register-confirmation/…spec.ts` | upstream | Correct testing import | `404199a` | U8 |
