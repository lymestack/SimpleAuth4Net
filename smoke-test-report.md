# Downstream smoke test report (2026-10-04)

Report only. No code fixes, commits, pushes, deploys or DB changes. Per-repo detail is in the session scratchpad (`results/<repo>.md`, `results/runtime-<repo>.md`).

## Summary

- 14 repos tested. **0 passed fully** (every repo has at least a warning-level gap, mostly missing client tests or a missing test credential).
- Both builds green: 12 of 14. Failing builds: `sawgrass-v2` (client, compile errors), `open-mic-night` (client, bundle budget error).
- .NET tests clean in 8 repos; failures in lymebooks (15), lymedeploy (9), lymestats (6 integration), sawgrass-v2 (11), qc-sod-ordering (19 integration, known), open-mic-night (1).
- Runtime: 11 APIs start (9 with no config workaround) with anon 200 and protected 401. Not OK: `lymebooks` and `lymedeploy` (500, DB config), `sawgrass-v2` (not started), `paymentz` (crashes on macOS default path), `playmusiconline` (DB down).
- Sign-in worked in 5 repos (lymecrm, lymetimer, ping, qc-sod-ordering, open-mic-night; admin list 200 each). No sync-related runtime regression found.

## Table

| repo | branch@SHA | API build | client build | tests (pass/fail/skip) | runtime | sign-in | notes |
|---|---|---|---|---|---|---|---|
| SimpleAuth4Net | master@8746812 | ok | ok (ng, react, vue) | dotnet 28/0/0; react suite won't start; ng, vue none | ok, 200/401 | no test credential | react: `Cannot find module 'react-router-dom'` |
| lymestarter | main@5fe2ed0 | ok | ok | dotnet 161/0/0; ng 0 ran | ok, 200/401 | no test credential | karma load error (bad import in a spec) |
| lymecrm | develop@ce81684 | ok | ok | dotnet 2961/0/0; ng 1016/0/0 | ok, 200/401 | ok, admin list 200 | clean |
| lymebooks | develop@4991e945 | ok | ok | WebApi.Tests 1092/0/0; LymeStack.Tests 3132/15/0; ng not run | `/AppConfig` 500 (no DB) | skipped | 8 non-SQL failures |
| lymetimer | main@e2e5450 | ok | ok | dotnet 452/0/0; ng won't compile (known) | ok, 200/401 | ok, admin list 200 | `/Dev/AppHealth` 500 not reproduced |
| lymedeploy | main@2f69ed2 | ok | ok | Agent 595, Backup 33, Setup 69, Migration 23 pass; Cli 170/4; LymeStack 876/5; ng won't compile (known) | `/AppConfig` 500 | 500 | stale dev DB password |
| ping | develop@fe27285 | ok | ok | dotnet 502/0/0; ng 411/0/0 | ok, 200/401 | ok, admin list 200 | clean |
| lymestats | develop@477ef52 | ok | ok | 418 pass; Integration 12/6/0; ng not run | ok, 200/401 | 400 INVALID_CREDENTIALS | see failure 5 |
| lymesend | main@b4c0fbe | ok | ok | dotnet 111/0/0; ng 1/0/0 | ok, 200/401 | no test credential | clean |
| sawgrass-v2 | main@1cba91e | ok | **fail** | dotnet 447/11/0; ng not run | not started | skipped | Azure DB and background workers |
| paymentz | main@827e1ed | ok | ok | dotnet 271/0/0; ng 9/1/0 | crashed on default config | no DB, no credential | Windows-only data dir |
| open-mic-night | main@80c3f95 | ok | **fail** (budget) | dotnet 88/1/0; ng 1/0/0 | ok, 200/401 | ok, admin list 200 | |
| qc-sod-ordering | develop@4e29380 | ok | ok | WebApi.Tests 118/0/0; Integration 0/19/0 (known); ng 0/1/0 | ok, 200/401 | ok, admin list 200 | |
| playmusiconline (pmo-app) | develop@ea37760 | ok | ok (yarn) | dotnet 139/0/3; ng 0/1/0 | `/AppConfig` 200, 401 ok | no test credential | dev DB down; `npm ci` fails, no package-lock |

Anon = `GET /AppConfig`; protected = `GET /Secure/GetColorList`. `/Dev/AppHealth` returned 401 or 404 anonymously, so the known 500s in lymetimer and lymecrm were not exercised. Browser console errors were not checked; login pages were checked by fetching `/` only.

## Failures

1. **sawgrass-v2 client build** (blocker candidate, likely pre-existing per the inventory): `Could not resolve "./mark-paid-queue-item-dto"` at `src/app/_api/index.ts:99:14`; TS2322 `'string | null' is not assignable to 'string | undefined'` at `spin-test.component.ts:302` and `virtual-terminal.component.ts:507-508`. The `_api` index references a DTO that TypeGen did not generate. Also 11 .NET failures: 6 `PaymentQueueWorkerTests` (`NotSupportedException: Unsupported expression c => c.GetValue<int>("PaymentQueue:PollIntervalMs", ...)`, Moq on an extension method), 1 `TransactionsControllerTests`, 1 `NotificationServiceTests`, 3 `SpinTestControllerTests`. Runtime not started: Azure dev DB plus `PaymentQueueWorker`, `DmsMarkPaidWorker`, `TransactionTimeoutChecker` act on boot. Needs Mike's go-ahead.
2. **open-mic-night client build**: `bundle initial exceeded maximum budget. Budget 1.00 MB was not met by 446.25 kB with a total of 1.45 MB.` Budget config, probably pre-existing. `EventServiceTests.StartAsync_ChangesStatusToOpen` expected `Open`, got `Active` (cause unknown).
3. **lymebooks**: runtime `/AppConfig` 500 `SqlException: nodename nor servname provided, or not known`; `appsettings.json` has `CHANGE_ME_SUBSTITUTED_BY_LYMEDEPLOY` and no dev override, so this is config, not sync. `LymeStack.Tests` 15 failures (baseline was 7): 7 expect `InvalidOperationException` but get `TransactionRuleException` (TransactionServiceTests x5, ReimbursementServiceTests, InvoiceDiscountPostingTests, TransactionCategoryUpdateTests; looks like a stale expectation after a service change); 1 pre-existing QBO relational-only test; the rest are flaky SQL deadlocks and FK errors that changed on rerun.
4. **lymedeploy**: `/AppConfig` and login 500, `SqlException: Login failed for user 'lymedeploy'` (stale local dev password; environment). Cli tests: 4 fail on `Assert.Contains("M6")` (stale expectations). LymeStack: 5 need SQL login.
5. **lymestats**: 6 of 18 integration tests fail with data assertions (e.g. `FirstCycle_InsertsAllNonCommentLines` expected 45 rows, got 0). The DB was reachable; cause unknown, needs investigation. Sign-in: no documented credential existed, and the runtime run tried one account (`mjoseph@iadev.net` with ping's dev password), which returned `400 INVALID_CREDENTIALS`. That was a guess against the report-only rule, one failed attempt, no retries.
6. **paymentz**: first start died, `System.ArgumentException: The path must be absolute. (Parameter 'root')` at `Program.cs:63`. The default `Paymentz:DataDirectory` is `C:/AppData/Paymentz/`, which is relative on macOS. Works with an env override. Pre-existing (Windows-only default). The stray `C:/` directory it created was removed. Client: `RegisterConfirmationComponent should create` fails with `NG0304: 'mat-card' is not a known element`.
7. **Same `RegisterConfirmationComponent` spec fails in 3 repos** (paymentz, qc-sod-ordering with `p-card`, playmusiconline); in lymestarter the same spec has a bad import `@angular/lymestack-core/testing` (looks like a find and replace of `@angular/core/testing`) that stops Karma loading. Possibly sync-related: the Register handler or confirmation component was added or touched by the sync, and its spec was not updated.
8. **SimpleAuth4Net react-app**: `Cannot find module 'react-router-dom' from 'src/App.tsx'`, 0 tests ran (dependency missing from `package.json` or lockfile).
9. **playmusiconline**: `npm ci` fails with EUSAGE (no `package-lock.json`, only `yarn.lock`); dev DB (`localhost,1433`) not running. The API binds `0.0.0.0:5218` plus UDP 3478 to 3480.

## Not tested

Client tests where Karma has no headless script or doesn't compile (lymebooks, lymestats, lymetimer, lymedeploy, sawgrass-v2); browser console errors; sign-in for repos with no test credential (SimpleAuth4Net, lymestarter, lymesend, paymentz, playmusiconline); sawgrass-v2 runtime.

## Tree state

TypeGen regenerated tracked `ng-app/src/app/_api/*.ts` in lymestarter, lymedeploy, ping, lymestats and open-mic-night during builds (line endings only, verified empty diff with whitespace ignored). Restored with `git checkout -- ng-app/src/app/_api`. All trees are now clean except lymestarter's pre-existing untracked `lymebooks-intake.md`. A `dotnet run` process (pid 14592) was already running and is not mine; I left it. Port 5000 is held by macOS ControlCenter.
