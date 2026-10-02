# T5 checklist: hands-on checks, decisions and push approvals

Prepared 2026-10-01 by the orchestrator for the downstream sync plan (`downstream-sync-plan.md`). Everything below is local and unpushed. Nothing has been deployed. Work top to bottom; each section says what you are deciding.

Before any `dotnet` command on this Mac:

```bash
export DOTNET_ROOT=/usr/local/share/dotnet PATH="/usr/local/share/dotnet:$PATH"
```

## 1. Decisions that change what gets pushed

Answer these first; each can add or remove commits.

| # | Question | Recommendation |
|---|---|---|
| 1 | **`pmo-app` scope.** The run policy says authorization fix only; M7 lists it for the email check, publish environment, older L1 features and the upstream tag. Only the authorization fix is in. | Leave it at the authorization fix until the rebuild. |
| 2 | **`ping` brand pages.** PinG's own pages and account screens were left untouched; only tokens, admin pages, About and the email check were ported. | Confirm. They are a client-approved design that deliberately does not inherit the framework theme. |
| 3 | **`lymecrm` kept-by-design surfaces:** POS register, `/crm/pos-count`, customer portal (tokens and defects only). | Confirm. |
| 4 | **Audit logger fix ahead of your intake review.** The always-registered `IAuthLogger` fix (intake group G1, first half) is now in `SimpleAuth4Net`, the template and all six LymeStarter-lineage apps. | Keep. No behaviour change when audit logging is on. |
| 5 | **`lymestarter` commit `ff2df33`** (the lymetools samples commit, not this plan's) names the instructions file in its body. Rewording it rewrites this plan's merge and later commits. | Leave it: a file reference, not attribution. Your call before the push. |
| 6 | **Registration audit log.** After the older-L1-features port, `open-mic-night`, `sawgrass-v2`, `qc-sod-ordering` and the template log every registration; `paymentz` still logs only the first user. | Make `paymentz` match (one small commit). |
| 7 | **`SimpleAuth4Net` production bundle budget.** `npm run build` fails: 1.42 MB against a 1.00 MB error limit, same before today. | Raise the error budget to 1.5 MB now; trim later. |
| 8 | **`try/catch (DbUpdateException)`** in `AppUserController.Post` (all repos) and `AuthController.Register` (template). It maps unique-index races to `EMAIL_EXISTS` / `USERNAME_EXISTS`. `SimpleAuth4Net` has no global error middleware. | Keep as is. |
| 9 | **`Auth/UserExists` is anonymous** everywhere (the register form uses it), so usernames can be probed, rate limited only. | Accept for now; add to the backlog. |
| 10 | **Anonymous `GET /AppConfig`** still returns settings whose names do not look secret. Angular does not read them from there. | Stop sending settings anonymously in round two. |
| 11 | **Decisions D6 to D9** (history rewrite for old trailers, packages instead of vendoring, .NET 10, Angular 21): where do you want them recorded as backlog items? | `BACKLOG.md` in `lymestarter`; .NET 10 needs its own plan before 2026-11-10. |

**Decided 2026-10-01 (Mike): all 11 as recommended.** Done since: `paymentz` logs every registration (`827e1ed`, 271 tests pass); bundle error budget raised to 1.5 MB (`e2a1f66`, production build passes); `~/git/lymestarter/BACKLOG.md` created with D6 to D9 and decision 9 (`59962b2`). Decision 10 is a round-two item. Push table counts below are one higher for `paymentz`, `SimpleAuth4Net` and `lymestarter`.

## 2. Reviews that gate round two

Nothing from these is ported until you answer. Neither blocks today's push.

- `/Users/michaeljosephwork/git/lymestarter/lymebooks-intake.md` (uncommitted): approve the 8 port groups in order and answer the 6 questions at the top.
- `/Users/michaeljosephwork/git/SimpleAuth4Net/core-drift-audit.md` (uncommitted): approve upstream items U1 to U14 and the two template design questions.

## 3. Hands-on: the template (`lymestarter`)

Another session has uncommitted sample-component edits in this tree; they do not affect these checks.

Start: `cd ~/git/lymestarter/WebApi/WebApi && dotnet watch run`, then `cd ~/git/lymestarter/ng-app && npm start`. Open `http://localhost:4200`. Admin account: use your current password (the one in older notes is stale and locks after 3 tries).

- [x] Signed out, the app loads past "Waiting for server…" (the `ConfigSetting` lockdown did not break bootstrap).
- [x] Admin → Security → Users → Add User: type an existing address in Email Address and tab out. Expect "Checking", then "The email address is already in use." in red, and Save disabled.
- [x] Edit an existing user and tab through their own address: no message.
- [x] Admin → Nav Menus → open a menu → the sort icon on an item with children opens the sort modal.
- [x] Admin home and the Users list render in the `.ls-*` style in light and dark mode.

**Run headlessly by the orchestrator on 2026-10-01 (Playwright, the `e2e/.env` account): all five pass.** Screenshots: `.orchestrator/downstream-sync-plan/t5-review/lymestarter/`. The "Checking" spinner was too quick to observe. Sign-in first failed with a 500: the dev database `LymeTemplate` had never received `0022-security-hardening.sql`, and `0001a-simpleauth-tables.sql` was unjournaled. With Mike's go-ahead, `0001a` was journaled as already run and DbUp applied `0022`; `--check` now reports the schema current.

## 4. Hands-on: visual sign-off, one repo at a time

For each: sign in as an Admin, walk app home → `/admin` → Security → Users → open a user, and toggle dark mode (sun/moon in the top bar) on every page. Look for one bold title and one primary button per view, small-caps labels and table headers, soft-shadow cards, aligned figures, and no light surfaces left in dark mode. The Add User email check from section 3 applies in every repo.

Full click paths are in each worker's summary under "Review Materials":

- `/Users/michaeljosephwork/git/SimpleAuth4Net/.orchestrator/downstream-sync-plan/processed/worker-summary-m9-lymecrm.md`
- `/Users/michaeljosephwork/git/SimpleAuth4Net/.orchestrator/downstream-sync-plan/processed/worker-summary-m9-lymetimer.md`
- `/Users/michaeljosephwork/git/SimpleAuth4Net/.orchestrator/downstream-sync-plan/processed/worker-summary-m9-lymedeploy.md`
- `/Users/michaeljosephwork/git/SimpleAuth4Net/.orchestrator/downstream-sync-plan/processed/worker-summary-m9-ping.md`
- `/Users/michaeljosephwork/git/SimpleAuth4Net/.orchestrator/downstream-sync-plan/processed/worker-summary-m9-lymestats.md`

### `lymecrm`

Start: `cd ~/git/lymecrm/WebApi/WebApi && ASPNETCORE_URLS='http://localhost:5218' dotnet watch run`; `cd ~/git/lymecrm/ng-app && npm start`. Account: `mjoseph@iadev.net` (local dev Admin, tenant 1). Confirm `appsettings.Development.json` points at `LymeCrm` on `192.168.50.42\SQLEXPRESS`, not UAT or Production.

- [ ] `/` home dashboard: queue cards, POS stat tiles, pipeline bar.
- [ ] `/crm/customers` list and a customer (name is the title; tabs General, Assets, Jobs, Estimates, Files, Portal access).
- [ ] `/crm/leads` list and board (drag a card; Won/Lost confirm), `/crm/jobs`, service agreements, communications, `/crm/search`, portal requests.
- [ ] `/admin/config`: every tab (the POS tab has seven sub-pages). `/admin/tenants`.
- [ ] `/crm/pos`: home, sessions, sales history and a sale, returns, payments, reports, catalog, counts, orders, purchase orders, receiving, vendors, invoices.
- [ ] Kept by design, should still feel as before: POS register, `/crm/pos-count` at phone width, `/portal`.
- [ ] Re-run the e2e specs the port touched: `crm-contrast-probe.spec.ts` (chip and banner colours changed), `crm-customer-tabs.spec.ts`, `crm-lead-inline-customer.spec.ts`.

### `lymetimer`

Start: `cd ~/git/lymetimer/WebApi/WebApi && dotnet watch run`; `cd ~/git/lymetimer/ng-app && npm start`. Account: an Admin who owns a team. Dev database `LymeTimer` on `192.168.50.42\SQLEXPRESS`.

- [ ] `/timer` dashboard: one blue "Add a Timer"; filter card; entry view and Adjust modals.
- [ ] `/timer/projects`, `/timer/tags`, `/timer/preferences`, `/timer/team` (4 tabs, 3 modals), `/timer/admin-teams`, both reports.
- [ ] Admin → App Health: the Send Test Email card is still there.
- [ ] Judge these visible changes: tags are uppercase pills; deleted projects are muted, not red, and the duplicate Archived badge is gone; Save and Return to Timer sit in the page header; team member row actions are always visible; team modals are no longer vertically centred.

### `lymedeploy`

Start: the local sandbox only (`~/git/lymedeploy/sandbox/up.sh`; login in `sandbox/README.md`). Never the production database.

- [ ] `/deploy` dashboard, a Promote/Redeploy confirm dialog, `/deploy/projects` and a project (5 tabs), a deployment (terminal stays dark in both modes), `/deploy/infrastructure`, `/deploy/backups`, `/deploy/activity`.
- [ ] Status colours: emerald, amber and red now come from the shared tokens and shift slightly. Acceptable?
- [ ] As a **non-admin**, `/deploy/settings` → Deployment shows "The current deployment mode is unavailable…" and no radio options.

### `ping`

Start: `cd ~/git/ping/WebApi/WebApi && dotnet watch run`; `cd ~/git/ping/ng-app && npm start`. Needs a local `appsettings.Development.json` pointing at a dev database.

- [ ] Admin pages under `/admin` and `/auth-admin`, and `/about`, in light and dark.
- [ ] `/` home: the module menu's plain card now picks up the soft shadow (the one possible change on a PinG page).
- [ ] `/coordinator`, the treasurer and shopper pages look unchanged.

### `lymestats`

**Before starting:** `WebApi/WebApi/appsettings.json` points at the shared `LymeStats` database on `192.168.50.42\SQLEXPRESS` and there is no Development override. Point `DefaultConnection` at a scratch database first.

Start: `cd ~/git/lymestats/WebApi/WebApi && dotnet watch run`; `cd ~/git/lymestats/ng-app && npm start`.

- [ ] `/analytics` (KPI tiles, panels, View all dialog), `/analytics/search`, `/analytics/live`, `/analytics-admin` (three tabs). Chart colours in dark mode.

### Email check in the SimpleAuth-lineage apps (optional spot check)

The same Add User check exists in `lymesend` (Bootstrap), `paymentz` and `open-mic-night` (Material), `qc-sod-ordering` (PrimeNG), `sawgrass-v2` (form reviewed but never compiled: its Angular build was already broken) and `lymebooks`. Route: `/auth-admin/users` → Add User. Each repo's worker summary (`worker-summary-m7-<repo>.md` in the folder above) has the start commands.

## 5. `lymecrm` production spot check

- [ ] Sign in to Production (release `2.0.0.22`, promoted 2026-10-01 without a sign-in check).
- [ ] Check which environment the deployed UAT and Production sites run as. LymeDeploy does not set it and this repo has no pipeline, so they have probably been running as `Development`. The local commit `119e6a1` flips both `web.config` files to Production.

## 6. Push approvals

Read with `git -C ~/git/<repo> log --oneline @{u}..HEAD`. Say yes or no per row.

| Repo | Branch | Ahead | What the commits contain | Before pushing |
|---|---|---|---|---|
| `SimpleAuth4Net` | `master` | 47 | Admin unlock `ea622c8` (first push) together with the authorization fix; logger event type; publish as Production; email check; test project and 28 tests; triage upstreams; logger registration fix; docs. About 30 of the 47 are commits to the plan document. | Decide whether `core-drift-audit.md` and this checklist get committed. |
| `lymestarter` | `main` | 21 | LymeSend attachments `b8ec996`; N, O, email check, Register handler, Secure cookies, logger fix, nav-menu fix, 51 new tests; tracker updates. **Also 4 commits from the lymetools plan's session** (samples and markers) and its uncommitted edits. | Coordinate with the lymetools session; decision 5. |
| `lymecrm` | `develop` | 20 | N, O and tests; design system (14); email check; publish as Production; upstream tag; logger and cookie fixes. | Sections 4 and 5. `main` untouched. |
| `lymebooks` | `develop` | 7 (rebased onto origin 2026-10-01, 0 behind, tip `009e14b0`) | Todos tidy; attachments; bootstrap; transport consolidation; email check; cookie fix. | Rebase done, no conflicts; build clean, `WebApi.Tests` 836 pass, `LymeStack.Tests` 7 SQL-backed baseline failures, Angular build passes. **Do not deploy** until LymeDeploy's email variables are renamed (section 7). |
| `lymetimer` | `main` | 21 | Authorization fix; attachments; bootstrap; N, O; design system (11); email check; publish as Production; logger and cookie fixes; one RESUME commit. | Section 4. |
| `lymedeploy` | `main` | 19 | Attachments; DbUp `--check`; N, O; design system (12) with the email check and logger fix; publish as Production; Register handler fix. | Section 4. |
| `ping` | `develop` | 16 | Attachments; G19 merge from `main`; N, O; design system for the framework parts; email check; logger, cookie and Register fixes. | Decision 2. `main` untouched. |
| `lymestats` | `develop` (local `main` level) | 17 | Attachments; G19; N, O; design system (8); email check; logger, cookie and Register fixes; one RESUME commit. | Push `main` too? Local `main` has no upstream configured. |
| `lymesend` | `main` | 5 | Authorization fix; email check; project-instructions correction; one doc commit. | Production: deploy order in section 7. |
| `sawgrass-v2` | `main` | 10 | Admin unlock and Argon2id (rebased onto origin, first push); authorization fix; G19 on the hasher; email check; publish as Production; TypeGen path; older L1 features; project instructions. | Migration unapplied (Azure firewall). |
| `paymentz` | `main` | 6 | Authorization fix; email check; TypeGen path; older L1 features; project instructions. | Decision 6. Production: section 7. |
| `open-mic-night` | `main` | 7 | Authorization fix; TypeGen path and 18 backslash files removed; publish as Production; email check; older L1 features; project instructions. | None. |
| `qc-sod-ordering` | `develop` and `main` (level locally) | 12 | Authorization fix; G19 on the hasher; upstream tag; email check; publish as Production; older L1 features; the `develop` into `main` merge. | Client app: it deploys through TeamCity and Octopus; `web.config` now says Production. Push both branches. |
| `playmusiconline` | `develop` | 2 | Authorization fix and logger event type in `pmo-app`. | Decision 1. `main` untouched. |

Not touched and not to be pushed: `md-ccrs-dev` (its 29 local commits stay as they are).

## 7. Carry into M11 (production), in this order

1. Deploy the authorization fix first: `lymesend`, `lymetimer`, `paymentz`, then the rest. UAT before production.
2. **`lymebooks`:** before deploying, rename LymeDeploy's variables from `EmailSettings:Transport`, `EmailSettings:LymeSend:ApiUrl`, `EmailSettings:LymeSend:SubjectPrefix`, `EmailSettings:LymeSend:ApiKey` to the same names under `LymeStackCore:Email:`. Otherwise mail silently falls back to SMTP pickup and nothing is delivered. Confirm against the live variable table; the worker could only read the onboarding doc.
3. `lymetimer` and `lymebooks`: after deploy, confirm `/api/AppConfig` returns a JSON content type (the new bootstrap guard retries otherwise).
4. `sawgrass-v2`: Azure firewall rule, then apply `2026-08-26 - Security hardening.sql`. `lymesend` and `paymentz`: confirm the same migration on production before deploying.
5. `lymecrm`: the environment check in section 5.

## 8. Smaller findings, no action needed today

- Karma suites do not compile in `lymetimer` (`team-context.service.spec.ts`) and `lymedeploy` (`dashboard.component.spec.ts:240`); both pre-existing.
- `sawgrass-v2`: TypeGen post-build fails and the Angular build is broken; a stray tracked `WebApi/WebApi/ng-app/` directory.
- `qc-sod-ordering`: `WebApi.IntegrationTests` fails 19 of 19; DbUp journal out of sync; `MockQcApiController` is anonymous with its own service token (confirm it is unreachable in production).
- `lymesend`: `SendController` relies on the API-key middleware alone; its project instructions carry a plaintext database credential.
- `pmo-app`: `RoomHub` has `[Authorize]` commented out; password hashes are still inline HMAC (Argon2id deferred).
- `lymedeploy`: `--check` works on the standalone DbUp exe only, not `LymeDeploy.Tools dbup`.
- Template: no global `.tnum` rule (each app added its own); RelyingApp mode fails at startup because `UseRateLimiter` runs without a registered limiter (intake group G4).
- `open-mic-night`: TypeGen's `clearOutputDirectory` and `createIndexFile` are off because `_api/` holds hand-added types.
- `lymebooks`: a stale `bin/Release` build made TypeGen generate an incomplete `_api`; the folder was moved aside. Local `main` is 499 commits behind local `develop`.
