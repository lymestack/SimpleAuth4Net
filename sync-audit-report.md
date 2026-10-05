# Downstream sync audit report

Audited 2026-10-04, read-only (`git fetch` only). Scope: the 14 repos in `t5-checklist.md` section 6 (including `SimpleAuth4Net` and `lymestarter`), plus `lymestats` and `qc-sod-ordering` `main`. `md-ccrs-dev` was excluded. The other session's untracked `lymestarter/lymebooks-intake.md` was ignored.

## Summary

- **Code verdict: good.** I checked about 230 claimed SHAs. Every one exists and is on its pushed branch except one stale ledger SHA. I read every port diff, and none is wrong. Six are partial; all are small.
- **Push state: done.** Every working branch is level with origin. The only unpushed commit is `SimpleAuth4Net` `c2f10db` (RESUME only). No tree is dirty except the other session's file.
- **Process and tracking: not done.** The claim "nothing is deployed" is very likely false for UAT: these repos' TeamCity configs build on push and create a UAT release. The trackers still show everything as unpushed.
- **Counts:** 2 blockers, 9 should fix, 11 cosmetic.
- **Blockers:** (1) the pushes probably triggered UAT releases, including `qc-sod-ordering` (a client app, through Octopus), and nothing has been checked. (2) `lymebooks` `main` carries the email-settings rename (P) and has been building to UAT since 2026-10-03. Nothing in the `lymebooks` repo records that the LymeDeploy variables must be renamed first.
- **Biggest real code gap:** `lymecrm` and `lymetimer` `Register` still write an audit log entry only for the first user. That is the second half of template fix `2fa0dd2` and of decision 6, and T4 missed it.
- **Schema:** none of the sync commits touch SQL, so no new migrations are needed. Every security-hardening script that exists is guarded and idempotent.

## Findings

| # | Repo | Fix | Claim | Actual | Severity |
|---|---|---|---|---|---|
| 1 | `lymesend`, `paymentz`, `lymetimer`, `lymestats`, `lymebooks`, `qc-sod-ordering` | All (push) | Resume prompt and plan say "Nothing is deployed." | Each committed `.teamcity/settings.kts` has a VCS trigger and a "Create Release in LymeDeploy … `--deploy-to 'UAT'`" step. `lymetimer` also creates an Octopus UAT release. `qc-sod-ordering`'s VCS root is `refs/heads/main` and it auto-creates an Octopus release to UAT, and `main` was pushed. `lymecrm` and `ping` VCS roots watch `main`, which was not moved, so they were probably not triggered. Live TeamCity/LymeDeploy/Octopus state is unchecked (see "Could not verify"). | blocker |
| 2 | `lymebooks` | P (email transport under `LymeStackCore:Email`) | Checklist: "**Do not deploy** until LymeDeploy's email variables are renamed." | `c14e9ff3` is on `origin/main`. The `lymebooks` resume prompt (2026-10-03) says `develop` was merged to `main` and "TeamCity is building the first release", deploying to UAT. That resume prompt and the repo docs never mention the variable rename. UAT mail may already be falling back to SMTP pickup, and the next UAT→prod promote would ship it. | blocker |
| 3 | `lymecrm`, `lymetimer` | Register audit log (`2fa0dd2` / decision 6) | Logger, cookie and Register fixes are "in all six LS apps"; T4 found Register gaps only in `lymedeploy`, `ping`, `lymestats`. | Both call the post-registration handler for every user, but `if (userCount != 0)` / `if (!isFirstUser) return` runs before `LogAsync(Registration)`. Non-first registrations are never audit-logged. Every other repo logs all registrations. | should fix |
| 4 | `lymestarter` (ledger) | Push columns | `core-fix-log.md` header: "Nothing in this log is pushed unless a row says so." | Every 2026-10-01 row, plus Q, R and S, still shows Pushed ⬜, but everything is now on origin. The ledger is wrong about push state for every repo. | should fix |
| 5 | `lymestarter` (ledger) | Secure cookies `537daf5`, Register handler `2fa0dd2`, IAuthLogger `18304c0` | M12 says the T4 gap-fill SHAs are still to add. | There is no fan-out table for cookies or the Register handler. The A/B "Related" paragraph still says the logger is "still to port" to `SimpleAuth4Net`, `lymetimer`, `lymestats` and `lymecrm`, but all four are done. Missing SHAs: `3ff1542`, `49dc74a`, `d4b26e0`+`aeb7c20`, `ac05cb1`; `1582f71`, `f6764eb`, `e55d7b6`, `1f67577`, `fedc66b5`; `3d39ac6`, `dc76393`, `ba375cf`+`3ee8e81`. | should fix |
| 6 | `SimpleAuth4Net` (plan) | Progress Log, trackers | M11, M12 and T5 are open. | The push is not in the Progress Log (the resume prompt admits this). Ticks are stale: T1's bundle budget (done in `e2a1f66`); T2/T5's template hands-on (passed, section 3); M12's D6 to D9 backlog (done in `59962b2`); M7 still 🔄 although decision 1 removed `pmo-app` from M7; T5's "Push approvals" (done). T5 still reads "Ready for Mike" with no record of the visual-run results. | should fix |
| 7 | `SimpleAuth4Net`, `lymestarter`, `ping`, `lymedeploy` | Always-registered `IAuthLogger` (L1 code) | Ground rule: "Upstream before downstream, always." | The template had it at 18:00, `ping` and `lymedeploy` at 18:14, and `SimpleAuth4Net` only at 18:48 (`3ff1542`). The Progress Log mentions it, but no exception is recorded as a decision. The end state is correct. | should fix (record it) |
| 8 | `lymedeploy` | M (`--check`) | Ported in `a825192`. | It works in the standalone DbUp exe only. `LymeDeploy.Tools dbup --check` still runs a real migration. This is known (checklist section 8) but not tracked as an open item anywhere actionable. | should fix |
| 9 | `lymebooks` | Secure cookies | `fedc66b5` marks the auth cookies `Secure`. | That is correct in `AuthController`. However, `Infrastructure/AuthCookieOptions.cs` (support-session cookie) sets `Domain` from `CookieDomain` without `Secure = true`. | should fix |
| 10 | `lymestarter` (inventory) | Push and migration state | `downstream-inventory.md`: "Not pushed: `sawgrass-v2`"; `paymentz` H "fixed locally (not pushed)"; `lymesend` Migrated "⬜ script only, no local connstr". | `sawgrass-v2` and `paymentz` are pushed. `core-fix-log.md` says `lymesend`'s dev DB **was** migrated, and that the "unapplied" reading was an error. The two trackers contradict each other. | should fix |
| 11 | `sawgrass-v2` | Outside the sync (pre-existing) | — | `DbUp/Program.cs` line 10 hardcodes a SQL password in a pushed repo. | should fix (out of scope, note only) |
| 12 | `lymebooks` (plan) | Todos tidy | Plan 1.8 cites `34175437`. | That SHA is not on origin. The rebased commit is `2ca0dfe3`. The Progress Log's `5bf34763` (cookies) is now `fedc66b5`. | cosmetic |
| 13 | `lymestarter` (ledger) | J design system | The 2026-09-23 tracker shows ⬜ for `lymecrm`, `lymetimer`, `lymedeploy`, `ping` and has no `lymestats` row. | It contradicts the 2026-10-01 J table, which shows all ✅. | cosmetic |
| 14 | `lymestarter`, `lymetimer`, `lymedeploy` | G (publish as Production) | Mirrors `524264f`. | None of them excludes `appsettings.Development.local.json`. None loads that file today. | cosmetic |
| 15 | `SimpleAuth4Net` | Docs `ced452a` | Docs cover unlock, G19, F and per-action auth. | G19 (admin-set password) is not documented. | cosmetic |
| 16 | `open-mic-night` | Instructions `80c3f95` | Fixes the `[AllowAnonymous]` advice. | It still says "Use [AllowAnonymous] for public endpoints" with no per-action wording. | cosmetic |
| 17 | `ping` | J | Brand pages "untouched by design". | `box-shadow: none` was added to `letters-preview.component.scss` and `_ping-elements.scss`. This is minor, but it is not "untouched". | cosmetic |
| 18 | `lymetimer` | N/O tests `c83f95e` | Tests ported. | No non-admin 403 test and no Admin-can-read test, unlike `lymedeploy`. | cosmetic |
| 19 | fleet | S (dark links) | Ported verbatim. | `--ls-link-color-rgb` and `--ls-link-hover-color-rgb` are undefined everywhere. This is already known (resume item 5). | cosmetic |
| 20 | `lymebooks`, template | P | `UseSmtpPickup` removed. | It is removed from `LymeStackCore` only. SimpleAuthNet's `EmailService` still reads `EmailSettings`/`UseSmtpPickup`, a dead but live-looking path. | cosmetic |
| 21 | `SimpleAuth4Net` | — | Resume prompt: "`master` is level with `origin/master`." | It is 1 ahead (`c2f10db`, the resume prompt itself). | cosmetic |
| 22 | `lymecrm`, `ping`, `playmusiconline` | Deploy branch | `main` untouched by design. | `origin/main` has none of the sync work in `lymecrm`/`ping` (19 commits behind in `ping`). `playmusiconline` local `main` is 36 ahead of `origin/main` (older app work, already on `develop`). This is expected, but a release from `main` gets none of the fixes. | cosmetic (note) |

## Repo by fix matrix

State at the pushed working branch. ✅ present · ◐ partial · ❌ missing · ➖ n/a or held by decision · ? not verified.

Fix key: **A** per-action auth · **B** logger event type · **C** Argon2id · **D** unlock · **E** G19 · **F** email check · **G** publish as Production · **H** TypeGen slashes · **I** email sender/registration handler · **J** design system · **K** attachments · **L** bootstrap · **M** DbUp `--check` · **N/O** ConfigSetting/UserFeedback · **P** transport · **Q/R/S** dark forms / logger toast / dark links · **Log** always-registered `IAuthLogger` · **Ck** Secure cookies · **Reg** Register handler and audit log for every user.

| Repo | A | B | C | D | E | F | G | H | I | J | K | L | M | N/O | P | Q | R | S | Log | Ck | Reg |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| `SimpleAuth4Net` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ✅ | ✅ | ✅ |
| `lymestarter` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `lymecrm` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ◐ |
| `lymebooks` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ◐ | ✅ | ✅ | ✅ | ✅ | ◐ | ✅ |
| `lymetimer` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ◐ |
| `lymedeploy` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ◐ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `ping` (`develop`) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `lymestats` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `lymesend` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅¹ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ❌² | ? | ✅ |
| `sawgrass-v2` | ✅ | ✅ | ✅³ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ❌² | ? | ✅ |
| `paymentz` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅⁴ | ✅ | ✅ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ❌² | ? | ✅ |
| `open-mic-night` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ❌² | ? | ✅ |
| `qc-sod-ordering` (both branches) | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ❌² | ? | ✅ |
| `playmusiconline/pmo-app` | ✅ | ✅ | ➖ | ➖ | ✅⁵ | ➖ | ➖ | ✅ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ➖ | ❌² | ? | ? |

¹ `lymesend` forked before SSO, so SSO modes are absent by design. ² Deferred to intake group G1 by decision; expected. ³ The migration is written and guarded but unapplied (Azure firewall). ⁴ The source `web.config` stays Development on purpose; the csproj publishes Production. ⁵ Stored with inline HMAC; Argon2id is deferred (D1).

Branch notes: `lymecrm` and `ping` `origin/main` (their deploy branches) have none of these fixes, by design. `lymestats` `main` = `develop` = `477ef52` on origin. `qc-sod-ordering` `main` = `develop` = `4e29380` on origin.

## Could not verify

- **Whether the pushes built and deployed anything.** That needs TeamCity, LymeDeploy and Octopus access. I only read the committed `settings.kts`, and the live configuration can differ.
- **Whether LymeDeploy's `lymebooks` email variables were renamed.** That needs the live LymeDeploy variable table.
- **Live database migration state** on UAT and prod (`lymesend`, `paymentz`, `sawgrass-v2` Azure dev), and whether `ping`/`lymestats` dev DBs have `0022`. I made no DB connections, by design.
- **Builds and tests.** These were not re-run; test counts were taken from summaries and attribute counts.
- **The visual quality of the app-page design commits** (`lymecrm`, `lymetimer`, `lymedeploy`, `lymestats`). I checked them by diff stats and binding preservation only.
- **Secure cookies in the SimpleAuth-lineage apps** (the "Ck" column `?`). No claim covers them, and I did not probe them.
- **Whether Octopus overrides `ASPNETCORE_ENVIRONMENT` for `qc-sod-ordering`.**
