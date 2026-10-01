# Downstream Sync — Inventory & Implementation Plan

**Created:** 2026-10-01 · **Owner:** Michael Joseph · **Scan basis:** every repo under `~/git/` fetched and probed at its `origin` tip on 2026-10-01 (not at the local working tree — see [1.2](#12-clone-state-on-this-machine)).

## Summary

Framework code in this estate is vendored as source copies. `SimpleAuth4Net` (layer **L1**) feeds the `lymestarter` template (layer **L2**), and 13 live scaffolds hang off the two of them. This document is the single inventory of everything that is out of sync between those layers, in both directions, plus the plan to close it.

**What the scan found, in priority order:**

1. **An unpatched authorization hole in 9 repos, including this one.** `AuthController` carries a class-level `[AllowAnonymous]`, which overrides the per-action `[Authorize(Roles = "Admin")]` attributes. `UnlockUser`, `RevokeAllSessionsForUser` and `RevokeAllSessions` are therefore callable without a token. LymeStarter fixed this on 2026-05-06 (`8c201de`); the fix was never upstreamed here and never reached `lymetimer`, `lymesend`, `paymentz` (all in production) or the other SimpleAuth-lineage apps. The 2026-08 admin-unlock port made it worse: an anonymous caller can now clear a lockout.
2. **Two more template-level holes, fixed only in LymeBooks.** `ConfigSettingController` GETs are anonymous (LymeBooks' note: they return every setting, secrets included) and `UserFeedbackController` GETs are readable by any signed-in user across tenants. Present in the template and all five other LymeStarter-lineage apps.
3. **The Argon2id port is incomplete in 4 repos** and split across branches or machines in two of them (`sawgrass-v2`, `qc-sod-ordering`).
4. **This machine's clones are stale.** 10 of 15 repos are behind origin; 5 of those have diverged. The trackers in `~/git/lymestarter` read here are a week old.
5. **Ports landed on the wrong branch.** The G19 fix went to `main` in `lymecrm`, `ping` and `qc-sod-ordering` while their working branch is `develop`.
6. **A large upstream-bound backlog.** LymeBooks' `lymestack-todos.md` holds 189 pending non-design entries for the template; the `.ls-*` design system has reached the template but none of the other five apps.
7. **Two scaffolds are missing from every tracker:** `lymestats` and `md-ccrs-dev/atcc-app`. (`atcc-app` has since been excluded from this plan — decision D2.)

**Critical success factors:** upstream before downstream, always; pull before porting; every port verified on the repo's deploy branch, not merely committed; nothing pushed or deployed without Mike's say-so; no testing against production.

### Milestone Progress Tracker

| Milestone | Model | Status | Duration (min) | Notes |
|---|---|---|---|---|
| [M0 — Reconcile clones with origin](#m0--reconcile-clones-with-origin) | Sonnet | ✅ Complete | not timed | Done 2026-10-01; nothing pushed; `sawgrass-v2` left diverged for M5 |
| [M1 — Decisions gate](#m1--decisions-gate) | — (Mike) | ✅ Complete | not timed | All 11 decisions made 2026-10-01; stale branches in `lymecrm` and `lymebooks` deleted |
| [M2 — SimpleAuth4Net catch-up (L1 upstream)](#m2--simpleauth4net-catch-up-l1-upstream) | Opus | ✅ Complete | 6 | `8417557..ced452a`, 12 local commits. F uses the existing `EMAIL_EXISTS` code; prod `ng build` budget failure is pre-existing |
| [T1 — Test SimpleAuth4Net](#t1--test-simpleauth4net) | Sonnet | ✅ Complete | 4 | `7da6c02` (test seam) + `bd4dd98`; 28 tests green; admin endpoints 401 without a token. Prod `ng build` budget item and push carried to T5 |
| [M3 — LymeStarter template catch-up](#m3--lymestarter-template-catch-up) | Opus | ✅ Complete | 8 | 10 commits `404a8f0..36f3416`, merged with the sample features as `b60d1b3` on `lymestarter` `main` (ahead of origin by 14, unpushed). 138 tests pass; dev and production `ng build` green |
| [T2 — Test the template](#t2--test-the-template) | Sonnet | ✅ Complete | 1 | `283405f`: 161 tests pass; production and development `ng build` green. Hands-on pass and push carried to T5 |
| [M4 — Urgent security fan-out](#m4--urgent-security-fan-out) | Sonnet | ✅ Complete | 8 + 4 | Both halves done: A + B in 7 repos, N + O in 5 repos (see log). No new test failures anywhere |
| [M5 — Finish the Argon2id port](#m5--finish-the-argon2id-port) | Opus | ✅ Complete | 2 | `sawgrass-v2` rebased onto origin (ahead 5, behind 0) and G19 converted (`ddc4fec`); `qc-sod-ordering` G19 on `develop` with the hasher (`c3331ae`). `sawgrass-v2` migration unapplied (Azure firewall) |
| [T3 — Security verification sweep](#t3--security-verification-sweep) | Sonnet | ✅ Complete | 7 | Nothing failed: A, B in 14 repos; N, O in 7; 401 over HTTP in 8 repos; all 50 port commits on their branches. Open: ledger update (tree in use), and HTTP checks for the 6 repos that had workers in them (moved to T4) |
| [M6 — Branch reconciliation](#m6--branch-reconciliation) | Sonnet | ✅ Complete | 7 + 4 | `ping` G19 merged into `develop`; `lymestats` G19 ported and local `main` level; `qc-sod-ordering` local `main` = `develop` = `788b3bf`. All local, unpushed. Table in the log |
| [M7 — Remaining L1 fan-out](#m7--remaining-l1-fan-out) | Sonnet | 🔄 In Progress | 6 (longest) | Done in all 11 repos except `pmo-app`, which is held for Mike's scope answer (see T5) |
| [M8 — LymeBooks → template intake](#m8--lymebooks--template-intake) | Opus | 🔄 In Progress | 7 (analysis) | Analysis done: `~/git/lymestarter/lymebooks-intake.md` (20 to port in 8 groups, 6 questions) and `core-drift-audit.md` (14 upstream items). **Porting waits on Mike's review** and T2 |
| [M9 — Design system fan-out](#m9--design-system-fan-out) | Opus | ✅ Complete | 22 (longest; 5 in parallel) | All five repos done, builds and tests green (see log). `ping`'s brand pages and `lymecrm`'s POS register, pos-count and customer portal kept by design. Visual sign-off at T5 |
| [M10 — L2 small-fix fan-out](#m10--l2-small-fix-fan-out) | Sonnet | 🔄 In Progress | 5 | K, L, M, P done in all five repos (see log). Open: fanning out whatever M8 ports, which waits on Mike's intake review |
| [T4 — L2 verification](#t4--l2-verification) | Sonnet | ✅ Complete | 10 + 23 (gap-fill) | Automated checks pass: J to P ✅ in six repos, no new failures, 30 of 30 tokenless calls 401. Gap-fill ported the three missed template fixes to all seven repos. Visual sign-off is at T5 |
| [T5 — Deferred hands-on gate](#t5--deferred-hands-on-gate) | — (Mike) | 🔄 Ready for Mike | — | Checklist prepared: `t5-checklist.md` in this repo. Every hands-on check and push approval, batched at the end |
| [M11 — Production: migrations and deploys](#m11--production-migrations-and-deploys) | Sonnet (Mike-driven) | ⬜ Not Started | — | Migrate before deploying code |
| [M12 — Trackers and registration](#m12--trackers-and-registration) | Haiku | 🔄 In Progress | 15 + 10 | Everything that needs no push state is done and verified (first pass on Haiku had errors; corrected by a Sonnet gap-fill). Open until after T5: final push state and the T4 gap-fill SHAs in the ledger, D6 to D9 backlog items, closing this document |

Status key: ⬜ Not Started / 🔄 In Progress / ✅ Complete. Record actual minutes in **Duration** when a milestone closes.

## Table of Contents

- [Summary](#summary)
- [Part 1 — Inventory](#part-1--inventory)
  - [1.1 Repo roster](#11-repo-roster)
  - [1.2 Clone state on this machine](#12-clone-state-on-this-machine)
  - [1.3 L1 backlog — SimpleAuth4Net-owned code](#13-l1-backlog--simpleauth4net-owned-code)
  - [1.4 L2 backlog — LymeStarter-owned code](#14-l2-backlog--lymestarter-owned-code)
  - [1.5 Upstream-bound backlog](#15-upstream-bound-backlog)
  - [1.6 Branch and deploy gaps](#16-branch-and-deploy-gaps)
  - [1.7 Raw drift against each template](#17-raw-drift-against-each-template)
  - [1.8 Tracker and documentation drift](#18-tracker-and-documentation-drift)
  - [1.9 Open decisions](#19-open-decisions)
- [Part 2 — Implementation plan](#part-2--implementation-plan)
  - [Ground rules for every milestone](#ground-rules-for-every-milestone)
  - [M0](#m0--reconcile-clones-with-origin) · [M1](#m1--decisions-gate) · [M2](#m2--simpleauth4net-catch-up-l1-upstream) · [T1](#t1--test-simpleauth4net) · [M3](#m3--lymestarter-template-catch-up) · [T2](#t2--test-the-template) · [M4](#m4--urgent-security-fan-out) · [M5](#m5--finish-the-argon2id-port) · [T3](#t3--security-verification-sweep) · [M6](#m6--branch-reconciliation) · [M7](#m7--remaining-l1-fan-out) · [M8](#m8--lymebooks--template-intake) · [M9](#m9--design-system-fan-out) · [M10](#m10--l2-small-fix-fan-out) · [T4](#t4--l2-verification) · [T5](#t5--deferred-hands-on-gate) · [M11](#m11--production-migrations-and-deploys) · [M12](#m12--trackers-and-registration)
- [Parallel Development Recommendations](#parallel-development-recommendations)
- [Appendix — probe markers](#appendix--probe-markers)
- [Progress Log / Notes](#progress-log--notes)

---

# Part 1 — Inventory

## 1.1 Repo roster

Found by scanning `~/git/` at any depth for a `WebApi/SimpleAuthNet` directory. Lineage: **LS** = has `WebApi/LymeStackCore` (LymeStarter-lineage), **SA** = does not (SimpleAuth-lineage).

| Repo | Lineage | Working branch | Stack | Production? | In lymestarter inventory? |
|---|---|---|---|---|---|
| `SimpleAuth4Net` | L1 template | `master` | net8 · ng19 | open source (GitHub) | — |
| `lymestarter` | L2 template | `main` | net10 · ng19 | — | — |
| `lymecrm` | LS | `develop` | net10 · ng19 | yes — UAT and Production both on release `2.0.0.22` (2026-10-01); no active users | ✅ |
| `lymebooks` | LS | `develop` | net10 · ng19 | yes, lymebooks.com | ✅ |
| `lymetimer` | LS | `main` | net10 · ng19 | yes, timer.lymestack.com | ✅ |
| `lymedeploy` | LS | `main` | net10 · ng19 | yes, deploys the fleet | ✅ |
| `ping` | LS | `develop` | net10 · ng19 | unconfirmed | ✅ |
| `lymestats` | LS | `develop` | net10 · ng19 | UAT (scaffolded 2026-08-30) | ❌ **missing** |
| `lymesend` | SA hybrid (pre-SSO) | `main` | net9 · ng19 | yes, carries mail for 5 apps | ✅ |
| `sawgrass-v2` | SA | `main` | net9 · ng19 | no (Azure dev DB) | ✅ |
| `paymentz` | SA | `main` | net8 · ng19 | yes, paymentz.app | ✅ |
| `open-mic-night` | SA | `main` | net9 · ng19 | no (confirmed 2026-08-26) | ✅ |
| `qc-sod-ordering` | SA | `develop` | net9 · ng19 | unconfirmed (client) | ✅ |
| `playmusiconline/pmo-app` | SA | `develop` | net8 · ng18 | deployed; being rebuilt | ✅ (skipped by decision) |
| `md-ccrs-dev/atcc-app` | SA | `develop` | net8 · ng18 | client app (MD CCRS) | ❌ **excluded (D2) — not Mike's project, do not touch** |

Retired and excluded: `lymeauth`, `lymecrm-v1`.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## 1.2 Clone state on this machine

After `git fetch` on 2026-10-01. "Behind" work was done and pushed from another machine on 2026-09-23 through 2026-10-01.

> **Superseded by M0 (2026-10-01 17:01).** The table below is the pre-M0 snapshot, kept for the record. Every repo is now level with origin except `sawgrass-v2` (deliberately diverged) and unpushed local commits. The rebases changed two SHAs: SimpleAuth4Net unlock `160a9db` → `ea622c8`, lymestarter attachments `4ea9c23` → `b8ec996`; the rest of this document uses the new ones. See the Progress Log.

| Repo | Behind | Ahead | What is local-only | Action |
|---|---|---|---|---|
| `SimpleAuth4Net` | 1 | 1 | `ea622c8` admin unlock (code) | **Diverged.** Rebase onto origin; do not push the unlock until M2's fix is in the same push |
| `lymestarter` | 7 | 1 | `b8ec996` LymeSend attachments (code) | **Diverged.** Rebase, then push in M3 |
| `lymebooks` | 379 | 0 | — | Fast-forward. Local `accounting-remediation` (407 ahead) and `backup/pre-rewrite` (501 ahead) are pre-history-rewrite leftovers |
| `lymetimer` | 1 | 1 | `b24b904` RESUME only | Diverged, trivial |
| `lymesend` | 1 | 1 | `39d2151` doc only | Diverged, trivial |
| `sawgrass-v2` | 3 | 2 | `3491a28` Argon2id port, `b229593` unlock (held back 2026-08-26) | **Diverged with a real conflict** — origin got G19 with inline HMAC. Handled in M5 |
| `lymedeploy` | 3 | 0 | — | Fast-forward |
| `paymentz` | 1 | 0 | untracked literal `..\..\ng-app\src\app\_api/` dir | Fast-forward; delete the stray dir |
| `open-mic-night` | 1 | 0 | — | Fast-forward |
| `playmusiconline` | 18 | 0 | — | Fast-forward |
| `lymecrm` | 0 | 2 | two doc commits | Push when approved. 25 stale `worktree-agent-*` branches, each 1–2 commits ahead of `develop` |
| `lymestats` | 0 | 1 | RESUME only | Push when approved |
| `md-ccrs-dev` | 0 | 29 | 29 commits on `develop` (2026-08) | Leave untouched (D2); `origin/main` also has 14 commits `develop` lacks |
| `ping`, `qc-sod-ordering` | 0 | 0 | — | Current |

[Return to Top](#downstream-sync--inventory--implementation-plan)

## 1.3 L1 backlog — SimpleAuth4Net-owned code

L1 code is `WebApi/SimpleAuthNet/**`, `AuthController.cs`, `AppUserController.cs`, the Angular auth surface and the user/role admin UI. An L1 change flows to **every** scaffold.

| ID | Changeset | Reference |
|---|---|---|
| **A** | Remove class-level `[AllowAnonymous]` from `AuthController`; mark public actions individually | lymestarter `8c201de` |
| **B** | `DefaultAuthLogger` passes `eventType` as the `{Label}` argument | lymestarter `8c201de` |
| **C** | 2026-07 security hardening (Argon2id, lockout, token integrity, enumeration) | SimpleAuth4Net `ebc5f51..2a9901d` |
| **D** | Admin account unlock | SimpleAuth4Net `ea622c8` (local only) |
| **E** | G19 — admin Create User stores the password | SimpleAuth4Net `4d61814` |
| **F** | Email duplicate validation on the user form | `simple-auth-email-check-fix.md`; only implementation is in `atcc-app` |
| **G** | Publish as Production (`<EnvironmentName>`, `web.config`, exclude `appsettings.Development.json`) | lymestarter `3751db1` |
| **H** | TypeGen `outputPath` uses forward slashes | SimpleAuth4Net `93079b2` |
| **I** | Older L1 features: `IPostRegistrationHandler`, `ISimpleAuthEmailSender`, SSO modes | `105d6e4`, `ebc5f51`, `c708f44` |

State at each repo's origin tip. ✅ present · ❌ missing · ⚠️ partial (see note) · ➖ not applicable.

| Repo | A | B | C | D | E | F | G | H | I |
|---|---|---|---|---|---|---|---|---|---|
| `SimpleAuth4Net` | ❌ | ❌ | ✅ | ⚠️¹ | ✅ | ❌ | ❌ | ✅ | ✅ |
| `lymestarter` | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ | ✅ |
| `lymecrm` | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ | ✅ |
| `lymebooks` | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ✅ | ✅ | ✅ |
| `lymetimer` | ❌ | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ | ✅ |
| `lymedeploy` | ✅ | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ | ✅ |
| `ping` | ✅ | ✅ | ✅ | ✅ | ⚠️² | ❌ | ✅ | ✅ | ✅ |
| `lymestats` | ✅ | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ | ✅ | ✅ |
| `lymesend` | ❌ | ❌ | ✅ | ✅ | ✅ | ❌ | ⚠️³ | ✅ | ⚠️⁴ |
| `sawgrass-v2` | ❌ | ❌ | ⚠️¹ | ⚠️¹ | ⚠️⁵ | ❌ | ❌ | ❌ | ❌ |
| `paymentz` | ❌ | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `open-mic-night` | ❌ | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ | ❌⁶ | ❌ |
| `qc-sod-ordering` | ❌ | ❌ | ⚠️⁷ | ⚠️⁷ | ⚠️⁵ ⁷ | ❌ | ❌ | ✅ | ❌ |
| `pmo-app` | ❌ | ❌ | ❌ | ❌ | ⚠️⁵ | ❌ | ❌ | ✅ | ❌ |
| `atcc-app` | ❌ | ➖ | ❌ | ❌ | ➖⁸ | ✅ | ❌ | ❌ | ❌ |

¹ Committed on this machine only, never pushed. ² On `main` only; working branch `develop` lacks it. ³ `web.config` hand-corrected; csproj `EnvironmentName` missing. ⁴ No SSO modes, by design (forked pre-SSO). ⁵ G19 ported with inline HMAC because the hasher was absent on that branch; must switch to `SimpleAuthPasswordHasher` once C lands. ⁶ Also has 18 tracked files with literal backslashes in their names, which break `git checkout` on Windows. ⁷ C and D are on `develop` only; E is on `main` only. ⁸ Already stores the password (legacy HMAC).

**Exposure from A, by repo.** Anonymous `RevokeAllSessions` / `RevokeAllSessionsForUser` (forced logout of every user) wherever those endpoints exist; anonymous `UnlockUser` (lockout bypass) wherever D is ✅. `SetupAuthenticator` is still guarded by its inline identity check. Rate limiting applies but does not gate. In production today: `lymetimer`, `lymesend`, `paymentz`.

**Outstanding database work for C** (as of the 2026-08-26 log; re-verify in M11): production DBs of `lymesend` and `paymentz`; `sawgrass-v2`'s Azure dev DB (firewall blocks this machine).

**SimpleAuth4Net's own housekeeping**

- [ ] `react-app/` and `vue-app/` were last touched 2025-06 and have never been run against the hardened API (server-driven MFA routing, pending-login gate on TOTP). Assess or retire (decision D10).
- [ ] `documentation/` and `README.md` predate the unlock endpoint, G19 and every item in this plan. Run "update docs" at the end of M2.
- [ ] `PLAN-smoke-test.md` is obsolete — the SSO commits it gates were pushed in April.
- [ ] `downstream-inventory.md` in this repo is an already-executed discovery prompt; the live inventory is `~/git/lymestarter/downstream-inventory.md`.
- [ ] `simple-auth-email-check-fix.md` is an unimplemented plan (item F).
- [ ] No test project exists in this repo (decision D11).
- [ ] .NET 8 (this repo, `paymentz`, `pmo-app`, `atcc-app`) and .NET 9 (`lymesend`, `sawgrass-v2`, `open-mic-night`, `qc-sod-ordering`) both leave support on 2026-11-10 (decision D8).
- [ ] Angular 19 → 21 clears ~29 npm advisories; deferred since July (decision D9).

[Return to Top](#downstream-sync--inventory--implementation-plan)

## 1.4 L2 backlog — LymeStarter-owned code

L2 code is `WebApi/LymeStackCore/**`, `WebApi/DbUp`, and the `lymestack-core` / `lymestack-admin` / `lymestack-shared` / `shell` Angular areas. An L2 change flows to LymeStarter-lineage apps only.

| ID | Changeset | Reference |
|---|---|---|
| **J** | `.ls-*` design system (global SCSS + 55 `lymestack-admin` files + `docs/design-system.md`) | lymestarter `f1d88b5`; source is LymeBooks `develop` |
| **K** | LymeSend transport sends `MailMessage` attachments | lymestarter `b8ec996` (local only) |
| **L** | AppConfig bootstrap resolves the deployed origin instead of `localhost:5218` | lymestarter `9ce88cd` |
| **M** | DbUp console `--check` preview mode | lymestarter `3b34a0a` |
| **N** | `ConfigSettingController` GETs no longer anonymous | LymeBooks, 2026-09-28 (pending upstream) |
| **O** | `UserFeedbackController` GETs require Admin; tenant-safe | LymeBooks, 2026-09-28 (pending upstream) |
| **P** | Email transport consolidated into the `Transport` setting (`UseSmtpPickup` removed) | lymestarter `bff8d80` |

| Repo | J | K | L | M | N | O | P |
|---|---|---|---|---|---|---|---|
| `lymestarter` | ⚠️¹ | ⚠️² | ✅ | ✅ | ❌ | ❌ | ✅ |
| `lymecrm` | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ | ✅ |
| `lymebooks` | ✅ (source) | ❌ | ❌ | ✅ | ✅ | ✅ | ❌ |
| `lymetimer` | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ✅ |
| `lymedeploy` | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ | ✅ |
| `ping` | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | ✅ |
| `lymestats` | ❌ | ❌ | ✅ | ✅ | ❌ | ❌ | ✅ |

¹ Ported 2026-09-25 but never built (`ng build` not run; only a `sass` compile). ² Committed on this machine only.

Already fully propagated and not listed: Reset Password modal, session-expired notice, date-range-picker `rangeChange`, `Web.config`/nuspec bundling, `LymeStackSimpleAuthEmailSender`.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## 1.5 Upstream-bound backlog

Fixes that exist downstream and have not reached the layer that owns the code.

| From | To | What | Size |
|---|---|---|---|
| `lymestarter` `8c201de` | `SimpleAuth4Net` | Items A and B | Small, **urgent** |
| `lymestarter` `3751db1` | `SimpleAuth4Net` | Item G | Small |
| `md-ccrs-dev/atcc-app` | `SimpleAuth4Net` | Item F (email duplicate validation) | Medium; spec already written — implement from `simple-auth-email-check-fix.md`, not from the `atcc-app` repo (D2) |
| `lymebooks` `lymestack-todos.md` | `lymestarter` | 241 "Pending" entries: 52 design-pass entries already ported on 2026-09-25 but never moved to Completed, plus **189 others** | Large |
| `lymebooks` (subset of the above) | `lymestarter` | Items N and O | Small, **urgent** |
| `lymebooks` (subset) | `SimpleAuth4Net` | Entries touching `SimpleAuthNet/` and `AuthController.cs`: `IPostRegistrationHandler` invite-token overload, `RegisterModel.InviteToken`, `AddSimpleAuthLogging` always registering an `IAuthLogger`, explicit Bearer-header read in `OnMessageReceived`, `register.component.ts` fix (2026-09-26), admin support access (2026-09-30) | Triage in M8 |

`lymestarter`'s `SimpleAuthNet` already differs from this repo's in 11 files (plus 2 it dropped when TypeGen was consolidated), and its `AuthController.cs` by 152 lines. Some of that is intentional template adaptation (`EmailAddress` column, net10 APIs); M8 separates intentional from accidental.

**No other LymeStarter-lineage repo keeps a `lymestack-todos.md`.** Core edits made in `lymecrm`, `lymetimer`, `lymedeploy`, `ping` and `lymestats` are untracked; [1.7](#17-raw-drift-against-each-template) measures them.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## 1.6 Branch and deploy gaps

| Repo | Gap |
|---|---|
| `lymecrm` | ~~G19 (`6017be8`) is on `origin/main` only; `develop` lacks it~~ Closed 2026-10-01: `develop` = `main` = `4dd7811` |
| `ping` | G19 (`7e13de8`) is on `origin/main` only; `develop` lacks it |
| `qc-sod-ordering` | **Split.** `develop` has the Argon2id port and unlock but no G19. `main` has G19 (inline HMAC) and the CLAUDE.md upstream tag but no Argon2id. 8 vs 3 commits apart |
| `lymestats` | `origin/main` is 1 commit behind `develop`; G19 on neither |
| `playmusiconline` | `origin/main` is 71 commits behind `develop` |
| `lymesend`, `paymentz` | ~~Stale `origin/develop` branches (19 and 74 commits behind `main`)~~ Deleted 2026-10-01 |
| `md-ccrs-dev` | 29 unpushed commits on `develop`; `origin/main` has 14 that `develop` lacks. Left as is (D2) |
| `lymebooks` | Local leftover branches from before the history rewrite |
| `lymecrm` | 29 `worktree-agent-*` branches (M0 count): 24 are patch-identical to `develop`; 5 carry commits `develop` lacks (4 POS feature commits, 1 worker summary) — the same pattern that hid LymeBooks' lost unlock port |

Which code is actually **deployed** in each environment is not determinable from the repos. M11 establishes it from LymeDeploy.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## 1.7 Raw drift against each template

Count of framework files that differ from the template's origin tip. This catches drift the marker probes above cannot name. Design-system item J accounts for the ~55 `lymestack-admin` files and 3–4 `scss` files in each LS row.

**LymeStarter-lineage vs `lymestarter@origin/main`**

| Repo | SimpleAuthNet | LymeStackCore | AuthController (lines) | lymestack-core | lymestack-admin | lymestack-shared | shell | account |
|---|---|---|---|---|---|---|---|---|
| `lymecrm` | 0 | 1 | 52 | 0 | 55 | 0 | 2 | 1 |
| `lymebooks` | 5 | 10 | 367 | 11 (+5 new) | 8 | 8 (+3 new) | 4 (+7 new) | 11 (+2 new) |
| `lymetimer` | 2 | 3 | 96 | 3 | 56 | 4 | 4 | 3 |
| `lymedeploy` | 1 | 0 | 18 | 1 | 55 | 0 | 3 | 2 |
| `ping` | 0 | 1 | 0 | 2 (+2 new) | 55 | 0 | 4 | 26 (+1 new) |
| `lymestats` | 0 | 0 | 0 | 0 | 55 | 0 | 2 | 1 |

**SimpleAuth-lineage vs `SimpleAuth4Net@origin/master`**

| Repo | SimpleAuthNet (differ / missing) | AuthController (lines) | account | auth-admin | core |
|---|---|---|---|---|---|
| `lymesend` | 7 / 4 | 199 | 18 | 17 | 10 |
| `sawgrass-v2` (local) | 6 / 7 | 228 | 17 | 22 (+6) | 9 (+11) |
| `paymentz` | 7 / 6 | 263 | 1 | 5 | 7 (+6) |
| `open-mic-night` | 6 / 6 | 190 | 23 | 24 | 6 |
| `qc-sod-ordering` (develop) | 6 / 6 | 174 | 19 (+1) | 21 | 11 (+2) |
| `pmo-app` (develop) | 8 / 7 | 625 | 8 | 7 | 9 (+9) |
| `atcc-app` | 13 / 17 | 725 | 23 | 22 | 11 (+9) |

SimpleAuth-lineage Angular counts are inflated by UI-kit rewrites (Bootstrap, PrimeNG) and are not all defects.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## 1.8 Tracker and documentation drift

- [x] `lymestarter/downstream-inventory.md` has no rows for `lymestats` or `md-ccrs-dev/atcc-app`. *(2026-10-01: `lymestats` added; `atcc-app` recorded as excluded.)*
- [x] Its 2026-09-26 note says `qc-sod-ordering` and `sawgrass-v2` are unported for Argon2id. Both ports exist — on `develop`, and unpushed on this machine, respectively. The other machine never saw them. *(Corrected 2026-10-01.)*
- [x] `lymestarter/core-fix-log.md` has no entries for `8c201de` (A, B), `3751db1` (G), `9ce88cd` (L), `b8ec996` (K) or `18a9108` (backslash TypeGen files), so none of them has ever had a tracked fan-out. *(Sections added 2026-10-01.)*
- [x] `lymestarter/security-port-plan.md` status header still reads "MED batch next". *(Updated 2026-10-01.)*
- [x] The `port-core-fix` skill's member lists omit `lymestats` and `atcc-app`. *(`lymestats` added, `atcc-app` listed as excluded, 2026-10-01.)*
- [ ] `> **Upstream:**` tag missing from `CLAUDE.md` in `lymecrm`, `qc-sod-ordering` (`develop`), `playmusiconline`, `md-ccrs-dev`. `ping` and `lymestats` have no root `CLAUDE.md` at all.
- [x] LymeBooks `lymestack-todos.md`: 52 design-pass entries are ported but still under Pending. *(Moved 2026-10-01, `34175437`.)*
- [ ] Pushed history still carries AI co-author trailers in 11 repos: `lymetimer` 58, `sawgrass-v2` 44, `lymesend` 43, `playmusiconline` 38, `qc-sod-ordering` 15, `lymestarter` 7, `open-mic-night` 7, `paymentz` 5, `SimpleAuth4Net` 3, `lymedeploy` 1, `md-ccrs-dev` 1 (decision D6).

[Return to Top](#downstream-sync--inventory--implementation-plan)

## 1.9 Open decisions

Resolved in [M1](#m1--decisions-gate). The **default** is what the plan assumes if Mike gives no answer.

| # | Decision | Default |
|---|---|---|
| D1 | `pmo-app`: port A + C now, or keep waiting for the rebuild? | Port A now (small); keep C deferred. **Decided 2026-10-01: default** |
| D2 | `atcc-app` (client repo, ng18/net8, 725-line AuthController drift): bring into the fleet? | Register it in the inventory; port A only; assess C separately with the client's schedule. **Decided 2026-10-01: no — excluded entirely.** It is not Mike's project; it was built on SimpleAuth v1.0 and is far behind. No worker enters `md-ccrs-dev`: no ports, no pushes (the 29 local commits stay as they are), no `CLAUDE.md` tag, no inventory or skill registration. Its rows in the Part 1 tables remain as a record of the scan only |
| D3 | Item I (older L1 features) in SimpleAuth-lineage apps: port, or accept as permanent drift? | Port `ISimpleAuthEmailSender` + `IPostRegistrationHandler` (small, shrinks future merges); skip SSO modes |
| D4 | `sawgrass-v2`: its Argon2id port was held back on 2026-08-26. Release it? | Yes, in M5. **Decided 2026-10-01: yes.** The hold was only that Mike did not want to touch `sawgrass-v2` at the time; no technical blocker |
| D5 | `qc-sod-ordering`: which branch deploys? | `develop` is the working branch; merge to `main` in M6. **Decided 2026-10-01: confirmed as the default** |
| D6 | Rewrite pushed history to strip AI trailers in 11 repos? | No action in this plan; Mike decides separately |
| D7 | Replace vendoring with NuGet/npm packages (the structural fix)? | Out of scope; revisit after M12 |
| D8 | .NET 8/9 end of support 2026-11-10: upgrade to net10 as part of this plan? | Separate plan, sequenced after M7 |
| D9 | Angular 19 → 21? | Separate plan |
| D10 | `react-app` / `vue-app` in this repo: maintain or mark unmaintained? | Mark unmaintained in the README |
| D11 | Add a small xUnit project to this repo for auth regression tests? | Yes — M2/T1 assume it |

**2026-10-01: all eleven decided.** D1, D2, D4, D5 as annotated above; D3, D6, D7, D8, D9, D10 and D11 accepted as their defaults. D8's separate .NET 10 plan must start soon: support ends 2026-11-10.

[Return to Top](#downstream-sync--inventory--implementation-plan)

---

# Part 2 — Implementation plan

Organized by layer and urgency: reconcile → fix upstream → fan out security → finish in-flight ports → reconcile branches → non-urgent fan-outs → production → trackers. Testing is incremental: a test milestone follows each group.

## Ground rules for every milestone

- **Workers complete every item in their milestone.** Do not skip an item because it looks lower priority or slightly out of scope. If an item should be dropped or deferred, say so in the summary for the orchestrator to decide, and still attempt it unless truly blocked.
- **`git pull` before touching a repo.** Three G19 workers ported against stale clones on 2026-09-26.
- **Work on the repo's current branch. Never create a branch.** Read the branch; do not assume `develop`.
- **Upstream before downstream.** Downstream patches are cut from the ported template file.
- **Semantic merge, never blind patch.** Every `AuthController.cs` has diverged.
- **Commit locally, plain human commit messages, no AI attribution. Do not push** unless the milestone says Mike has approved it.
- **Never exercise a production system.** All verification runs against local or dev instances.
- **On this Mac, run `dotnet` from the arm64 SDK:** `export DOTNET_ROOT=/usr/local/share/dotnet PATH="/usr/local/share/dotnet:$PATH"`. The brew `dotnet` in `/usr/local/bin` is x86_64 and fails at the TypeGen post-build step (found in M0).
- Fan-outs use `/iadev-lyme:port-core-fix`; every fan-out gets a section in `~/git/lymestarter/core-fix-log.md`.
- Per-repo quirks (DbUp layout, UI kit, gitignored `_api/`) live in `~/git/lymestarter/downstream-inventory.md`. Read it before porting.

## M0 — Reconcile clones with origin

**Model:** Sonnet · **Depends on:** nothing · **Blocks:** everything

Bring this machine's clones level with origin so every later milestone works from one truth. *Workers must complete all items below.*

- [x] Fast-forward `lymebooks`, `lymedeploy`, `paymentz`, `open-mic-night`, `playmusiconline`.
- [x] Rebase the trivial divergences onto origin: `lymetimer` (now `27d6499`), `lymesend` (now `f35587c`).
- [x] Rebase `lymestarter` `4ea9c23` onto `origin/main` (now `b8ec996`); rebuild and run `LymeStack.Tests` — 110 passed.
- [x] Rebase `SimpleAuth4Net` `160a9db` onto `origin/master` (now `ea622c8`); `dotnet build WebApi/WebApi.sln` — succeeded. **Do not push** — the unlock must not reach GitHub before item A is fixed.
- [x] Leave `sawgrass-v2` diverged; record its state for M5 (see Progress Log).
- [x] Delete the untracked literal-backslash directory in `paymentz/WebApi/WebApi/` (88 untracked TypeGen `.ts` files).
- [x] List (do not delete) stale local branches: `lymebooks` `accounting-remediation` and `backup/pre-rewrite`; `lymecrm`'s `worktree-agent-*` (29, not 25). For each `lymecrm` branch, report whether its commit is patch-identical to something on `develop` (`git cherry`). Report is in the Progress Log.
- [x] Re-run the [appendix probe](#appendix--probe-markers) against local working trees and confirm it matches tables 1.3 and 1.4. Matches.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## M1 — Decisions gate

**Owner:** Mike · **Depends on:** nothing (can run alongside M0) · **Blocks:** M5, M6, M7 scope

- [x] Answer D1–D11 in [1.9](#19-open-decisions), or accept the defaults. All decided 2026-10-01.
- [x] Review M0's stale-branch report and say which branches may be deleted. `lymecrm`: all 29 deleted 2026-10-01. `lymebooks` `accounting-remediation` and `backup/pre-rewrite`: deleted 2026-10-01.
- [x] Say whether `md-ccrs-dev`'s 29 unpushed commits should be pushed. No: the repo is not to be touched (D2).

[Return to Top](#downstream-sync--inventory--implementation-plan)

## M2 — SimpleAuth4Net catch-up (L1 upstream)

**Model:** Opus (security-sensitive merge in the reference implementation; includes user-form UI) · **Depends on:** M0

Make this repo the true L1 source again. *Workers must complete all items below.*

- [x] **A:** remove class-level `[AllowAnonymous]` from `AuthController`; add `[AllowAnonymous]` to each action that must be public, using lymestarter `8c201de` as the reference list. Drop the dead `is VerifyOtpModel` check in `VerifyMfa`.
- [x] **B:** `DefaultAuthLogger.WriteLog` takes and logs `eventType`.
- [x] **G:** `<EnvironmentName>Production</EnvironmentName>` and `CopyToPublishDirectory="Never"` for `appsettings.Development.json` in `WebApi.csproj`; flip `web.config` to `Production`.
- [x] **F:** implement email duplicate validation per `simple-auth-email-check-fix.md` — `Auth/EmailExists`, the `EMAIL_TAKEN` check in `AppUserController.Post`, and the Angular user-form feedback (done as `EMAIL_EXISTS`, the code this repo and the template already use; wherever this plan says `EMAIL_TAKEN`, read `EMAIL_EXISTS`). Decide the `EmailExists` authorization deliberately: an anonymous version is an account-enumeration oracle, which the 2026-07 hardening closed elsewhere. Default: `[Authorize(Roles = "Admin")]`. No `try/catch` in the controller. (`EmailExists` is Admin-only. The pre-existing `try/catch` in `AppUserController.Post` was left: this repo has no global error middleware. Decision queued for T5.)
- [x] UI for F: match the existing Material form's hint/error treatment exactly — spacing, colour tokens and icon weight consistent with the username-availability feedback beside it, no inline `style` attributes.
- [x] Triage the `SimpleAuthNet` differences between this repo and `lymestarter` (11 files, [1.5](#15-upstream-bound-backlog)): list each as *intentional template adaptation* or *should be upstreamed here*, and upstream the latter.
- [x] If D11 = yes: add `WebApi/WebApi.Tests` (xUnit) to `WebApi.sln`.
- [x] If D10 = default: note in `README.md` that `react-app` and `vue-app` are unmaintained.
- [x] Delete `PLAN-smoke-test.md`; replace this repo's `downstream-inventory.md` with a one-line pointer to `~/git/lymestarter/downstream-inventory.md`; delete `simple-auth-email-check-fix.md` once F is in.
- [x] Run "update docs" (`documentation/api.md`, `README.md`) for unlock, G19, F and the authorization change.
- [x] Commit locally as separate commits per item.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## T1 — Test SimpleAuth4Net

**Model:** Sonnet · **Mode:** unit/integration · **Depends on:** M2

- [x] Write a reflection test: `AuthController` has no class-level `[AllowAnonymous]`, and every action carries exactly one of `[Authorize]` / `[AllowAnonymous]`.
- [x] Write a test that `UnlockUser`, `RevokeAllSessionsForUser` and `RevokeAllSessions` require the `Admin` role.
- [x] Write tests for `EMAIL_TAKEN` on create, on edit to another user's address, and no false positive when editing a user without changing their own address (case-insensitive).
- [x] Run the suite green: `cd WebApi && dotnet test`.
- [x] Start the API locally (`cd WebApi/WebApi && dotnet run`) and confirm with `curl -X POST` and no token that the three admin endpoints return 401, while `Login`, `ForgotPassword` and `RefreshToken` remain reachable.
- [ ] `cd ng-app && npm run build` succeeds. **Open:** fails on the bundle budget only (1.42 MB against 1.00 MB), unchanged from before M2; no compile errors, and the development build succeeds. Decision queued for T5.
- [ ] **Deferred to T5 — do not pause here.** Push approval: this is the first push of `ea622c8`, and it must go together with A. Downstream workers read this repo's local clone, so nothing waits on the push.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## M3 — LymeStarter template catch-up

**Model:** Opus · **Depends on:** T1 · **Repo:** `~/git/lymestarter`

The template is the diff base for every LymeStarter-lineage worker. *Workers must complete all items below.*

- [x] **N:** port LymeBooks' `ConfigSettingController` change. First establish what the pre-login Angular bootstrap actually reads from it, so removing anonymous access does not break startup; follow LymeBooks' working implementation.
- [x] **O:** port LymeBooks' `UserFeedbackController` change.
- [x] **F:** port email duplicate validation from M2 (column is `EmailAddress`; user form lives under `lymestack-admin/security/users/` and uses the `.ls-*` design system — follow `docs/design-system.md`, `.ls-inset--danger` / form-hint patterns, no bespoke SCSS).
- [x] Port anything M2's triage upstreamed that the template lacks.
- [x] **J:** finish the design-system port's "Built" box — `npm ci`, fix the esbuild arch issue if present (`npm install @esbuild/darwin-arm64 --no-save`), `npx ng build --configuration development`.
- [x] Add the T1 reflection test to `WebApi/LymeStack.Tests`.
- [x] Open `core-fix-log.md` sections for A/B, G, K, L, N/O and F with per-repo tables seeded from [1.3](#13-l1-backlog--simpleauth4net-owned-code) and [1.4](#14-l2-backlog--lymestarter-owned-code).
- [x] Commit locally.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## T2 — Test the template

**Model:** Sonnet · **Mode:** unit/integration + build · **Depends on:** M3

- [x] Write tests: anonymous `GET ConfigSetting` is refused; a non-admin cannot read another tenant's feedback; `EMAIL_TAKEN` cases as in T1.
- [x] `cd WebApi && dotnet test` green (baseline before M3: 103+ tests).
- [x] `npx ng build` green.
- [ ] **Deferred to T5 — do not pause here.** Hands-on, folded into one pass (no screenshots). Start with `cd WebApi/WebApi && dotnet watch run` and `cd ng-app && npm start`, open `http://localhost:4200`, sign in as an Admin (ask Mike for the current password — the one in older notes is stale and locks the account after 3 tries). Check: (1) the app loads past "Waiting for server…" while signed out, proving N did not break bootstrap; (2) Admin → Security → Users → Add User, enter an existing address, tab out, see the in-use message and a disabled Save; (3) the Admin home and Users list render in the `.ls-*` style in both light and dark mode.
- [ ] **Deferred to T5 — do not pause here.** Mike approves pushing `lymestarter` (includes `b8ec996`).

[Return to Top](#downstream-sync--inventory--implementation-plan)

## M4 — Urgent security fan-out

**Model:** Sonnet, one worker per repo (Opus for `pmo-app` and `atcc-app` if D1/D2 include them — 600+ line divergence) · **Depends on:** T1 for the L1 half, T2 for the L2 half

*Workers must complete all items below.*

**L1 half — items A + B**, reference SimpleAuth4Net's M2 commit:

- [x] `lymetimer` (production) — `4df9db3`, `e49c9c2`; 415 tests pass
- [x] `lymesend` (production) — `35a0156`, `283f9f5`; 103 tests pass
- [x] `paymentz` (production) — `147f33d`, `8fd4f43`; 260 tests pass
- [x] `sawgrass-v2` — apply on top of the local unpushed commits; do not resolve the origin divergence here (M5) — `37194ed`, `512462f`; 435 pass, the same 11 pre-existing failures as before
- [x] `open-mic-night` — `a2a21a1`, `cbd6f5d`; 76 of 77 pass, 1 pre-existing failure
- [x] `qc-sod-ordering` — on `develop` — `7f16632`, `26ceaf8`; 107 tests pass
- [x] `playmusiconline/pmo-app` — in scope per D1 — `f47ecc6`, `ea37760`; 139 pass, 3 skipped
- [x] ~~`md-ccrs-dev/atcc-app`~~ — excluded per D2; do not open the repo

For each: enumerate that repo's own actions before deciding which are public — several have app-specific endpoints the reference knows nothing about. Add the reflection test where a test project exists.

**L2 half — items N + O**, reference lymestarter's M3 commit:

- [x] `lymecrm` — `84cc373`, `8c83115`, tests `8f255e3`; 2817 + 128 tests pass
- [x] `lymetimer` — `9b0a18d`, `4f88fa9`, tests `c83f95e`; 429 pass
- [x] `lymedeploy` — `cb46e00`, `e636e95`, tests `355734c`; pre-existing failures unchanged (9)
- [x] `ping` — `9ab26d4`, `a5c6621`; 134 + 354 pass
- [x] `lymestats` — `559ad1f`, `4338526`, tests `eb620ee`; 131 pass

For each: confirm the app does not read `ConfigSetting` anonymously somewhere app-specific before removing access.

- [x] Each worker builds, runs that repo's tests, commits locally, and reports pre-existing failures separately from new ones.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## M5 — Finish the Argon2id port

**Model:** Opus (`sawgrass-v2`, `pmo-app`, `atcc-app`); Sonnet (`qc-sod-ordering`) · **Depends on:** M1 (D1, D2, D4, D5), M4

*Workers must complete all items below.*

- [x] **`sawgrass-v2` (D4):** rebase local `3491a28` + `b229593` + M4's commit onto `origin/main`. Resolve the G19 conflict by replacing origin's inline-HMAC hashing in `AppUserController.Post` with `SimpleAuthPasswordHasher`. Confirm `Services/UserMigrationService.cs` also uses the hasher.
- [x] **`qc-sod-ordering` (D5):** bring G19 (`0363f69`) onto `develop` and convert it from inline HMAC to the hasher. Leave the `main` merge to M6.
- [x] **`pmo-app` (D1):** deferred to the rebuild (decided 2026-10-01). Nothing to do here.
- [x] **`atcc-app` (D2):** excluded (decided 2026-10-01). Nothing to do; do not open the repo.
- [x] For every repo touched: legacy HMAC verify is **retained** (removing it locks out every user); rehash-on-login present; migration written in that repo's own DbUp convention and applied to its **dev** database only. (Both migrations exist as `2026-08-26 - Security hardening.sql`. Neither was applied today: `sawgrass-v2`'s only database is the unreachable Azure dev DB (M11); `qc-sod-ordering` is recorded as already migrated and its DbUp journal is out of sync, so the worker left it alone.)

[Return to Top](#downstream-sync--inventory--implementation-plan)

## T3 — Security verification sweep

**Model:** Sonnet · **Mode:** unit/integration + scripted probe · **Depends on:** M4, M5

- [x] Re-run the appendix probe at each repo's working tree: columns A, B, N, O read ✅ for every in-scope repo; C/D/E read ✅ with no ⚠️ for `sawgrass-v2` and `qc-sod-ordering`.
- [x] `grep` every ported repo for remaining inline `HMACSHA512` **hash writes** outside `SimpleAuthPasswordHasher` (verify-only legacy paths are expected).
- [x] `dotnet build` + `dotnet test` per repo; table of results with pre-existing failures called out (`sawgrass-v2`, `lymedeploy`, `open-mic-night` have known ones). (Built fresh in 8 repos; the 6 repos with design-system workers in them carry worker-reported results and are rebuilt in T4.)
- [x] For each repo that can run locally, start the API and `curl -X POST` the three admin endpoints without a token: expect 401. (401 in `SimpleAuth4Net`, `lymestarter`, `lymesend`, `paymentz`, `open-mic-night`, `sawgrass-v2`, `qc-sod-ordering`, `pmo-app`, each started with an unreachable database override. The other 6 are checked in T4.)
- [x] `git merge-base --is-ancestor <sha> HEAD` for every port commit on its working branch.
- [ ] Update the `core-fix-log.md` tables. Push approvals are deferred to T5; do not pause here. **Open:** the table updates are written (in the T3 summary) but not applied, because the other orchestrator has a worker in the `lymestarter` tree; applied with M12.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## M6 — Branch reconciliation

**Model:** Sonnet · **Depends on:** M1, T3

*Workers must complete all items below. Branch deletion and any force-push need Mike's explicit confirmation per branch.*

- [x] `lymecrm`: merge `origin/main`'s G19 into `develop`; confirm `main` and `develop` then differ only by intended unreleased work. Done 2026-10-01: both at `4dd7811`, pushed.
- [x] `ping`: same. Done 2026-10-01: merge `87f14f7` on `develop` (only G19 came in; no conflicts); `develop..origin/main` is empty. Not pushed.
- [x] `qc-sod-ordering`: merge `develop` into `main` so the Argon2id port, unlock, G19 and the CLAUDE.md tag are on both. Done 2026-10-01 locally: merge `788b3bf` (conflicts in `AppUserController.cs` and its tests resolved to the `develop` side); `develop` fast-forwarded to the same commit. `WebApi.Tests` 118 pass on `main`.
- [x] `lymestats`: port G19 (it is on neither branch), then bring `main` level. Done 2026-10-01 locally: G19 `be15a68` on `develop`; local `main` fast-forwarded to `develop` (`191cbd1`) after the design-system work landed.
- [x] `playmusiconline`: Mike's call 2026-10-01 — leave `main` alone. No merge, no report needed. (The D1 authorization fix on `develop` in M4 still stands.)
- [x] `lymesend`, `paymentz`: stale `origin/develop` branches deleted 2026-10-01 on Mike's go-ahead (tips `82c3f43` and `b6fedd5`; both were strict ancestors of `main`, and TeamCity builds from `main`). `lymesend`'s local `develop` deleted too.
- [x] `lymecrm`: act on Mike's M1 decision for the `worktree-agent-*` branches. All 29 deleted 2026-10-01 (see Progress Log).
- [x] `lymebooks`: act on Mike's M1 decision for `accounting-remediation` and `backup/pre-rewrite`. Both local branches deleted 2026-10-01 (`origin/accounting-remediation` still exists).
- [x] Produce a table: repo · working branch · deploy branch · commits apart. In the Progress Log, 2026-10-01 18:45.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## M7 — Remaining L1 fan-out

**Model:** Sonnet per repo; **Opus for item F's UI** in the three non-Material kits (`qc-sod-ordering` PrimeNG, LymeStarter-lineage `.ls-*`, `lymesend` Bootstrap) · **Depends on:** T3, M6

*Workers must complete all items below.*

- [ ] **F** — email duplicate validation to the 12 scaffolds other than `atcc-app` (which already has it). Probe the user form's actual UI kit before writing markup; express the feedback in that kit's idiom so it reads as native to the form (matching the adjacent username-availability hint), not pasted in. **Status 2026-10-01:** done in 11 scaffolds; `pmo-app` held for Mike's scope answer.
- [ ] **G** — publish-as-Production to `lymecrm` (both `WebApi` and `NasRemoteApi` `web.config`), `lymetimer`, `lymedeploy`, `lymesend` (csproj only), `sawgrass-v2`, `paymentz`, `open-mic-night`, `qc-sod-ordering`, `pmo-app`. Check first whether LymeDeploy already overrides the environment at deploy time for each app, and say so in the summary. **Status 2026-10-01:** done or already present in all listed repos except `pmo-app` (held). LymeDeploy does not override the environment for any app; TeamCity passes `-p:EnvironmentName=Production` where a pipeline exists; `qc-sod-ordering` deploys through Octopus (not checked).
- [x] **H** — forward-slash TypeGen `outputPath` in `sawgrass-v2`, `paymentz`, `open-mic-night`; clean-rebuild (`rm -rf bin obj`) and confirm `_api/` is populated in the right place. In `open-mic-night`, `git rm` the 18 tracked backslash-named files and add the `.gitignore` rule from lymestarter `18a9108`. Done 2026-10-01 in all three.
- [ ] **I** — per D3: `ISimpleAuthEmailSender` and `IPostRegistrationHandler` into `sawgrass-v2`, `paymentz`, `open-mic-night`, `qc-sod-ordering` (and `pmo-app` per D1; `atcc-app` is excluded per D2). **Status 2026-10-01:** done in `sawgrass-v2`, `paymentz`, `open-mic-night`, `qc-sod-ordering`; `pmo-app` held.
- [ ] Add the `> **Upstream:**` tag to `lymecrm`, `qc-sod-ordering`, `playmusiconline` `CLAUDE.md` (not `md-ccrs-dev`: excluded per D2). Flag (do not create) the missing root `CLAUDE.md` in `ping` and `lymestats`. **Status 2026-10-01:** added in `lymecrm` (`63121f4`) and `qc-sod-ordering` (`9cbe315`); `playmusiconline` held with the rest of `pmo-app`. `ping` and `lymestats` have no root file (flagged, not created).
- [x] Each worker builds, tests and commits locally. 

[Return to Top](#downstream-sync--inventory--implementation-plan)

## M8 — LymeBooks → template intake

**Model:** Opus (189 entries of judgment: generic vs app-specific, plus shell/UI changes) · **Depends on:** T2 · **Repos:** `~/git/lymebooks` (read), `~/git/lymestarter` (write)

*Workers must complete all items below.*

- [x] Move the 52 design-pass entries in LymeBooks `lymestack-todos.md` from Pending to Completed (ported 2026-09-25).
- [x] Classify each remaining Pending entry: **port** (generic framework fix), **app-specific** (stays in LymeBooks — everything under `lymestack-invoicing/` and `LymeStackInvoicing/`, which the template does not have), **already upstream**, or **needs Mike**. Write the classification to `~/git/lymestarter/lymebooks-intake.md`.
- [ ] Pause for Mike's review of `~/git/lymestarter/lymebooks-intake.md` before porting anything. **Waiting on Mike** (non-blocking; the file is written, uncommitted, with 8 port groups and 6 questions at the top).
- [ ] Port the approved entries to `lymestarter`, grouped into coherent commits (e.g. tenant-context incident fixes; shell loading-pill self-heal; modal-host dialog; dark-mode `color-scheme`; quick-launch Recents; ConfigSetting cache). Shell and shared-component work must hold to `docs/design-system.md` — tokens only, no fixed colours, light/dark parity.
- [ ] Entries that touch `SimpleAuthNet/`, `AuthController.cs`, or `account/` are L1: port them to `SimpleAuth4Net` first, then the template.
- [ ] Audit the untracked core drift in [1.7](#17-raw-drift-against-each-template) for `lymetimer` (LymeStackCore 3 files, shared 4, shell 4), `ping` (`account/` 26 files, lymestack-core 2 + 2 new) and `lymecrm` / `lymedeploy` (shell): for each differing file, upstream it, revert it, or record it as an intentional app divergence. *(Verdicts written to `core-drift-audit.md` in this repo on 2026-10-01: 69 files, no reverts, 14 upstream items. Acting on them waits on Mike's review.)*
- [ ] `dotnet test` and `ng build` green in `lymestarter`; commit locally; add `core-fix-log.md` sections for each ported group.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## M9 — Design system fan-out

**Model:** Opus, one worker per repo (front-end design work) · **Depends on:** T2 · **Repos:** `lymecrm`, `lymetimer`, `lymedeploy`, `ping`, `lymestats`

Reference: `lymestarter` `f1d88b5` and `docs/design-system.md`; LymeBooks `develop` is the fullest implementation. *Workers must complete all items below.*

- [x] Adopt `_components.scss` wholesale; **merge** (never overwrite) token additions into each repo's `_variables.scss`, `_themes.scss` and `_bootstrap-overrides.scss` where the repo carries its own overrides.
- [x] Bring the 55 `lymestack-admin/**` files to the template versions. Where the repo has modified one (see drift counts — `lymetimer` 56), merge by hand and keep the app's behaviour.
- [x] Apply the design system to the app's **own** pages with real craft: consistent spacing rhythm, a clear visual hierarchy with one primary action per view, hairline dividers and small-caps section labels, tabular figures in numeric columns, restrained hover/focus transitions, and full light/dark parity through `--ls-*` tokens only. The result should feel deliberately designed — closer to Linear or Notion than to stock Bootstrap — and every page should look like it belongs to the same product as LymeBooks. *(Exceptions kept by design, for Mike to confirm at T5: `ping`'s own brand pages and account screens; `lymecrm`'s POS register surface, `/crm/pos-count` and the customer portal.)*
- [x] Remove banned classes listed in `docs/design-system.md`; replace Font Awesome with Bootstrap Icons where the template did.
- [x] Copy `docs/design-system.md` and add the CLAUDE.md pointer. *(`ping` and `lymestats` have no root file; none was created. `ping`'s pointer is in `docs/README.md`.)*
- [x] `ng build` green; commit locally.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## M10 — L2 small-fix fan-out

**Model:** Sonnet · **Depends on:** T2 (and M8 for its output)

*Workers must complete all items below.*

- [x] **K** — LymeSend attachments to `lymebooks`, `lymetimer`, `lymedeploy`, `ping`, `lymestats`, with the template's `LymeSendEmailTransportTests`.
- [x] **L** — AppConfig bootstrap to `lymebooks` and `lymetimer`. Both are in production with a working bootstrap, so first read how each resolves its API URL today and keep any explicit hostname cases.
- [x] **M** — DbUp `--check` to `lymedeploy`'s own `WebApi/DbUp`.
- [x] **P** — transport consolidation in `lymebooks` (remove `UseSmtpPickup`; settings under `LymeStackCore:Email`). Check the deployed config substitution before changing key names.
- [ ] Fan out whatever M8 ported to the template, to the LymeStarter-lineage apps that lack it. **Open:** nothing is ported from M8 until Mike has reviewed the intake.
- [x] Each worker builds, tests and commits locally.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## T4 — L2 verification

**Model:** Sonnet · **Mode:** unit/integration + hands-on visual sign-off · **Depends on:** M9, M10

- [x] Re-run the appendix probe: columns J–P ✅ for all LymeStarter-lineage repos.
- [x] `dotnet test` + `ng build` per repo; results table.
- [x] Re-run the [1.7](#17-raw-drift-against-each-template) drift measurement; every remaining differing framework file is either gone or listed as an intentional divergence. *(Measured 2026-10-01: `lymestack-admin` drift is 0 in five apps; the three template fixes from M3 that had no fan-out milestone (`537daf5`, `18304c0`, `2fa0dd2`) were ported by the gap-fill. Remaining differences in the five apps are intentional or pending audit items U1 to U14. `lymebooks`' 79 differing files have no per-file classification; they map to the intake groups waiting on Mike.)*
- [ ] **Deferred to T5 — do not pause here.** **Hands-on visual sign-off by Mike** (no screenshots — the look is judged live, one repo at a time). For each of `lymecrm`, `lymetimer`, `lymedeploy`, `ping`, `lymestats`: `cd WebApi/WebApi && dotnet watch run`, `cd ng-app && npm start`, open `http://localhost:4200`, sign in as an Admin, and walk: app home → Admin home (`/admin`) → Security → Users → open a user → toggle dark mode on each. Expected: `.ls-*` cards, tables and page headers throughout; no fixed-light surfaces in dark mode; app-specific pages visually consistent with the admin pages.
- [ ] The orchestrator does **not** pause for sign-off here. T4 closes on the automated checks above; the per-repo sign-off happens in T5.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## T5 — Deferred hands-on gate

**Owner:** Mike · **Depends on:** every milestone through T4 (and as much of M12 as does not need final push state) · **Blocks:** M11

Mike's instruction (2026-10-01): the orchestrator bypasses every hands-on testing gate while the work runs and collects them here, in one sitting at the end. The orchestrator prepares a single checklist with the exact commands and URLs, then walks Mike through it.

- [ ] **`lymestarter` hands-on pass** (from T2): app loads past "Waiting for server…" while signed out; Add User with an existing address shows the in-use message and disables Save; Admin home and Users list render in `.ls-*` style in light and dark mode.
- [ ] **Visual sign-off** (from T4), one repo at a time: `lymecrm`, `lymetimer`, `lymedeploy`, `ping`, `lymestats`.
- [ ] **`lymecrm` production spot-check:** release `2.0.0.22` was promoted on 2026-10-01 without a sign-in check.
- [ ] **Push approvals, repo by repo:** `SimpleAuth4Net` (first push of `ea622c8`, together with item A), `lymestarter` (includes `b8ec996`), then each downstream repo. The orchestrator presents a table: repo · branch · commits ahead · what they contain.
- [ ] Anything a worker flagged along the way as needing Mike's eyes.
  - **M9 `lymecrm`: deployed environment.** LymeDeploy does not set the environment and this repo has no CI pipeline, so deployed `lymecrm` (UAT and Production) has probably been running as `Development` unless it was set on the server. Check both. The `web.config` files now say Production (local commit `119e6a1`).
  - M9 `lymecrm`: the e2e contrast probe (`e2e/tests/crm-contrast-probe.spec.ts`) was tuned to the old chip and banner colours and needs a re-run, with `crm-customer-tabs.spec.ts` and `crm-lead-inline-customer.spec.ts` (two selectors changed). Kept by design: POS register surface, `/crm/pos-count`, customer portal. Some views keep more than one primary button on purpose; POS keycaps now render in Roboto.
  - M9 `lymedeploy`: its emerald, amber and red status colours now come from the shared tokens and shift slightly. Its Karma suite does not compile on `main` (`dashboard.component.spec.ts:240`, pre-existing).
  - M9: `ping` and `lymedeploy` picked up the template's always-registered `IAuthLogger` fix because the email-check tests need it. `SimpleAuth4Net`, `lymetimer`, `lymestats` and others still lack it (intake group G1).
  - M9: the template has no global `.tnum` rule, so tabular figures in its own admin pages do nothing; `lymetimer` and `lymestats` each added one locally. Also `bootstrap-icons` only became a template dependency with the samples commit. Both belong in the template.
  - M9 `lymetimer`, visible changes to judge: user tags render as uppercase pills; the duplicate Archived badge on Projects is gone and deleted rows are muted, not red; Save and Return to Timer moved into page headers; team member row actions are always visible; team modals are no longer vertically centred; "Clean Up Empty Teams" is an outline button. Its Karma specs do not compile (pre-existing, `team-context.service.spec.ts`).
  - M9 `lymestats`: `appsettings.json` points at the shared LymeStats database on `192.168.50.42` and there is no Development override; point it at a scratch database before running the visual check. New page titles "Analytics", "Live", "Search paths"; form footers reordered to Cancel then Save.
  - M6: `lymebooks` local `main` is 499 commits behind local `develop` (stale local branch; `origin/main` equals `origin/develop`). `qc-sod-ordering` local branches are level but `origin/main` and `origin/develop` still differ until pushed.
  - T3: `lymestarter` commit `ff2df33` (the lymetools plan's samples commit, unpushed) names the root instructions file in its body. It is a file reference, not attribution, and it now sits under this plan's merge `b60d1b3`, so rewording it means rewriting later commits. Left alone; Mike's call before the push.
  - T3: `pmo-app` still writes password hashes with inline HMAC (expected: Argon2id is deferred there, D1). Its Development config pins Kestrel to port 5218.
  - M9 `ping`: home's module menu sits in a plain Bootstrap card, which now picks up the framework's soft shadow.
  - **M9 `ping`: brand pages left alone.** PinG's own pages and its account screens use a deliberate, client-approved `--ping-*` design that does not inherit the framework theme. The orchestrator told the worker not to restyle them to `.ls-*`; only the framework parts get the design system. Confirm or overrule.
  - M4 `lymedeploy`: the Settings page reads `Deployment.Mode` from `GET /ConfigSetting`, now Admin-only; a non-admin silently saw the default. The M9 worker was asked to make the page say the value is unavailable. Stale comments about the "full" AppConfig settings list remain in `CliContracts.cs`, `LymeDeployApiClient.cs` and `deploy-api.service.ts`.
  - M4 `lymestats` (and likely other LymeStarter-lineage apps): still has the audit-logger registration defect the template fixed in `18304c0`; fan-out waits on the intake review (group G1).
  - **M10 `lymebooks`, item P: must be handled before that code is deployed (M11).** The email settings moved from `EmailSettings:*` to `LymeStackCore:Email:*`. LymeDeploy's variables must be renamed first (`Transport`, `LymeSend:ApiUrl`, `LymeSend:SubjectPrefix`, `LymeSend:ApiKey`), or the app silently falls back to SMTP pickup and delivers nothing. The worker could only read the onboarding doc, not the live variable table.
  - M7 `sawgrass-v2`: the Angular build fails before and after today's work, because TypeGen's post-build step fails for the WebApi project (`WebApi.TypeScriptExports` not found) and `_api/` is stale; so the new form markup was reviewed, not compiled. Also a stray tracked `WebApi/WebApi/ng-app/` directory.
  - M7 `qc-sod-ordering`: it deploys through TeamCity and Octopus, not LymeDeploy; whether Octopus overrides `ASPNETCORE_ENVIRONMENT` could not be checked. Its `web.config` now says Production.
  - M7, item I (`sawgrass-v2`, `qc-sod-ordering`, `open-mic-night`): `Register` now writes every registration to the audit log, as the reference does; `paymentz` kept first-user-only.
  - M10 `lymebooks`: `ng build --configuration development` fails on stale gitignored `_api` models, before and after; `LymeStack.Tests` has 6 to 13 SQL-backed failures that vary run to run on the unmodified baseline.
  - **Reviews waiting on Mike (nothing from them is ported until answered):** `/Users/michaeljosephwork/git/lymestarter/lymebooks-intake.md` (8 port groups, 6 questions) and `/Users/michaeljosephwork/git/SimpleAuth4Net/core-drift-audit.md` (14 upstream items, 2 questions).
  - M3: the always-registered `IAuthLogger` fix is now in the template (`18304c0`) but not in `SimpleAuth4Net`, which still cannot build `AuthController` with audit logging off. It is intake group G1 (L1); port it upstream once the intake is approved.
  - M3: the template has global error middleware, yet `AppUserController.Post` and `AuthController.Register` keep a `try/catch (DbUpdateException)` that maps unique-index races to `EMAIL_EXISTS` / `USERNAME_EXISTS`. Remove (race becomes a 500) or keep?
  - M3: anonymous `GET /AppConfig` still returns every `ConfigSetting` whose name does not contain `ApiKey`, `Secret`, `Password`, `Token` or `ConnectionString` (LymeBooks' filter). Angular does not read them from there. Good enough, or stop sending settings anonymously at all?
  - M7 `paymentz` vs `open-mic-night`: after item I, `open-mic-night` writes every registration to the audit log (as the reference does); `paymentz` kept its first-user-only logging. Pick one.
  - M7 `paymentz`: source `web.config` still says `Development` on purpose (its own `paymentz-production-environment-fix.md`: the file drives local IIS and the SDK rewrites it at publish). The deployed production `web.config` stays `Development` until a new package ships.
  - M7 `open-mic-night`: fixing the TypeGen path exposed that TypeGen wiped hand-added types in `_api/`; the worker turned off `clearOutputDirectory` and `createIndexFile`, so `index.ts` is hand-maintained there. Also a stale `processPath` in `web.config`.
  - M10 `lymedeploy`: `--check` works on the standalone DbUp exe only; `LymeDeploy.Tools dbup` still migrates for real. Should the Tools subcommand honour it?
  - M10 `lymetimer`: the new bootstrap guard needs `/api/AppConfig` to return a JSON content type on UAT and production; check after deploy.
  - **`pmo-app` scope for M7:** the run policy says `pmo-app` gets the authorization fix only (D1), but M7 lists it for F, G, I and the upstream tag. Held; nothing beyond A and B has been ported there. Say whether M7 applies to `pmo-app`.
  - M5 `qc-sod-ordering`: `WebApi.IntegrationTests` fails 19 of 19 before and after today's work (EF service provider resolution); the DbUp journal is out of sync with the database.
  - M4 `lymesend`: `SendController` is class-level `[AllowAnonymous]` by design; `ApiKeyAuthMiddleware` is its only gate. Also, `CLAUDE.md` there carries a plaintext DB credential.
  - M4 `qc-sod-ordering`: `MockQcApiController` is class-level `[AllowAnonymous]` with its own service JWT; confirm it is not reachable in production.
  - M4 `pmo-app`: `RoomHub` has `[Authorize]` commented out ("temporarily removed to debug SignalR authentication") and checks auth per method.
  - M4, several repos: each repo's `CLAUDE.md` still tells readers to put `[AllowAnonymous]` on the controller, or says the 2026-07 hardening is outstanding. Docs touch-up in M12.
  - M8 drift audit: `/Users/michaeljosephwork/git/SimpleAuth4Net/core-drift-audit.md` proposes 14 upstream items (U1 to U14) and two template design questions.
  - M2: `SimpleAuth4Net` production `npm run build` fails its bundle budget (1.42 MB against a 1.00 MB error limit in `angular.json`); it fails the same way before M2. Raise the budget or trim the bundle?
  - M2: `SimpleAuth4Net` has no global error middleware, so the old `try/catch (DbUpdateException)` in `AppUserController.Post` stays. Add middleware and drop the catch, or keep it?
  - M2: `Auth/UserExists` is still anonymous (the public register form uses it), so usernames can be probed, rate limited only. Same in the template. Accept, or close it?

If a hands-on check fails, the orchestrator dispatches a gap-fill worker and re-presents only the failed check.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## M11 — Production: migrations and deploys

**Model:** Sonnet assisting; **Mike drives every production action** · **Depends on:** T5

*All items must be completed; each production step needs Mike's explicit go-ahead.*

- [ ] From LymeDeploy, record the currently deployed release and commit per app and environment (UAT, prod). Fill a table: app · env · deployed commit · has C? · has A?
- [ ] Confirm whether the security-hardening migration has been applied on the **production** databases of `lymesend` and `paymentz`. Migrate before deploying code that needs the columns.
- [ ] Add the Azure firewall rule for `sawgrass-v2`'s dev DB and apply its migration, or record why not.
- [ ] Deploy the M4 security fixes first, in this order: `lymesend`, `lymetimer`, `paymentz`, then the rest. UAT before prod.
- [ ] Set `AuthSettings:AlwaysUseSecureCookies=true` in production config behind the TLS-terminating proxy, where not already set.
- [ ] After each deploy, Mike confirms sign-in works and an existing (legacy-hash) account still logs in.
- [ ] `open-mic-night` has no DbUp project — note that its migration is hand-run at first deploy.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## M12 — Trackers and registration

**Model:** Haiku · **Depends on:** all previous

*Workers must complete all items below.*

- [x] Add a `lymestats` row (lineage, DbUp layout, Angular path, UI kit, branch) to `~/git/lymestarter/downstream-inventory.md`, plus a one-line note that `md-ccrs-dev/atcc-app` exists but is deliberately not a fleet member (D2: not Mike's project, SimpleAuth v1.0, never port to it); correct the 2026-09-26 "still unported" note.
- [x] Add `lymestats` to the member lists in `~/git/claude-shared-settings/skills/iadev-lyme/skills/port-core-fix/SKILL.md`, and add a step telling it to `git fetch` and compare against origin before probing.
- [x] Update `security-port-plan.md`'s status header, or retire the file.
- [ ] Close out every `core-fix-log.md` table opened in M3 and M8 with final SHAs and push state. **Open:** tables filled with today's SHAs and sections added for H, I, M, P, J and the C/D/E completions (`lymestarter` `6b62ae1`). Still to add after T5: push state, and the T4 gap-fill commits (`Secure` cookies, `IAuthLogger`, `Register` handler).
- [x] Update `~/.claude/claude-md-sections/products/lymestack.md` ("roughly half rolled out" is no longer true).
- [x] Write a short "how to stay in sync" note at the top of `downstream-inventory.md`: fetch first, one working branch per repo, `lymestack-todos.md` in every LymeStarter-lineage repo.
- [ ] Record D6, D7, D8, D9 outcomes as backlog items where Mike wants them.
- [ ] Mark this document complete and move it to `documentation/` only when Mike says he is done with it.

[Return to Top](#downstream-sync--inventory--implementation-plan)

---

## Parallel Development Recommendations

**Sequential blockers:** M0 → (M2 → T1) → (M3 → T2). Nothing downstream starts before its reference implementation is tested. M11 is last and Mike-driven. M12 closes.

| Group | Milestones | Can run together because |
|---|---|---|
| **A** | M0, M1 | M1 is Mike reading; M0 is mechanical |
| **B** | M4 L1 half (7 workers, one per repo) | One repo each; starts after T1. `lymetimer` is in both halves — give it one worker for both |
| **C** | M4 L2 half (5 workers) | One repo each; starts after T2 |
| **D** | M5's two repos (`sawgrass-v2`, `qc-sod-ordering`) | Independent repos; after M4 |
| **E** | M8, M9, M10 (K/L/M/P items) | M8 writes only `lymestarter`; M9 and M10 touch different files in the same repos — run M9 and M10 **sequentially per repo** (M9 first), in parallel across repos |
| **F** | M7 per repo | One worker per repo; after M6 so each has a single working branch |

Do not run two workers in the same repo at once; no worktree isolation is needed if that holds.

**Orchestrator run policy and handoff state (set 2026-10-01).** Read this before dispatching anything.

- **State at handoff:** M0 and M1 are complete. The M2 worker prompt was issued to Mike on 2026-10-01 but may or may not have been run. Before dispatching M2, check `git log` in `~/git/SimpleAuth4Net` for M2 commits after `9e75a85` and ask Mike whether a worker is in flight; never run two workers in that repo.
- **No hands-on gates mid-run.** Every hands-on check and every push approval is deferred to [T5](#t5--deferred-hands-on-gate). Test milestones close on their automated checks. Do not stop to ask Mike to look at anything until T5.
- **Nothing is pushed and nothing is deployed before T5.** Workers commit locally; downstream workers use the local clones as their reference.
- **`md-ccrs-dev/atcc-app` is off limits** (D2). No worker enters that repo.
- **`pmo-app`** gets the authorization fix only (D1); **`sawgrass-v2`**'s Argon2id port is released in M5 (D4); **`qc-sod-ordering`** works on `develop` and merges to `main` in M6 (D5).
- **.NET on this Mac:** every worker prompt that builds includes `export DOTNET_ROOT=/usr/local/share/dotnet PATH="/usr/local/share/dotnet:$PATH"`.
- **Attribution hook:** a PreToolUse hook rejects any shell command that both commits and contains an assistant/vendor name or a co-author trailer, including inside heredoc text or a `grep` pattern. Keep such words out of commit commands; run message checks as a separate command.
- **Shared repo, shared index:** when the orchestrator commits this document while a worker is active in `SimpleAuth4Net`, use `git commit downstream-sync-plan.md -m "…"` (pathspec form) so no worker's staged files are swept in.
- **Still needing Mike mid-run** (not a hands-on test): the `lymebooks-intake.md` review in M8. Treat it as non-blocking: carry on with every milestone that does not depend on the answer. M6's Mike-dependent items were all settled on 2026-10-01 (see M6).

**Orchestrator context management.** Fan-outs here reach 8–13 workers. If dispatching prompts fills the orchestrator context, prompt Mike to run `/compact` while workers run; suggest it proactively when approaching the limit. After compacting, resume from `.orchestrator/state.json`.

**Gap-filling prompts.** When a milestone comes back with items skipped or partly done, the follow-up prompt must:

- follow the same structure as the original (header, mission statement, reference to this document and the milestone);
- state what the first attempt already completed and which files it changed;
- list the other active workers and their directories;
- be labelled `Worker Context: [Milestone Name] - Gap Fill`;
- end with the standard completion instructions: (1) commit code changes before writing the summary, (2) write the summary to `.orchestrator/worker-summary-[milestone-slug]-gap.md`, (3) prompt the user to close/clear the context.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## Appendix — probe markers

How each cell in 1.3 and 1.4 was decided. Probe by `git ls-tree` / `git show <ref>:<path>` against the ref, never by `find` (stale `.claude/worktrees/` copies give wrong answers).

| ID | Marker |
|---|---|
| A | `[AllowAnonymous]` within the 6 lines above `class AuthController` |
| B | `WriteLog(LogLevel.Information, eventType` in `Logging/DefaultAuthLogger.cs` |
| C | `SimpleAuthNet/SimpleAuthPasswordHasher.cs` exists |
| D | `UnlockUser` in `AuthController.cs` |
| E | `NewPassword` / `INVALID_PASSWORD` in `AppUserController.cs` |
| F | `EmailExists` in `AuthController.cs` |
| G | `<EnvironmentName` … `Production` in `WebApi.csproj` (the element may carry a `Condition` attribute: the literal `<EnvironmentName>Production` probe misread `lymesend` and `paymentz`, which already had G); `ASPNETCORE_ENVIRONMENT" value="Development"` in `web.config` |
| H | no `\\` in any `tgconfig.json`; `git ls-files` shows no backslash-named paths |
| I | `IPostRegistrationHandler.cs`, `ISimpleAuthEmailSender.cs`, `Models/Config/SimpleAuthMode.cs` exist |
| J | `ng-app/src/scss/_components.scss` exists |
| K | `Attachments` in `LymeSendEmailTransport.cs` |
| L | `document.baseURI` in `ng-app/src/main.ts` |
| M | `--check` in `WebApi/DbUp/Program.cs` |
| N | count of `AllowAnonymous` in `ConfigSettingController.cs` (template had 2 before N; after M3 it has 1, in a comment, and the class is Admin-only) |
| O | count of `Authorize(Roles = "Admin")` in `UserFeedbackController.cs` (template 2, LymeBooks 4) |
| P | `UseSmtpPickup` absent from `LymeStackCoreOptions.cs` |

Markers N and O are counts, not proofs — M3 reads the LymeBooks diff rather than trusting them.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## Progress Log / Notes

Newest first. Format: `**YYYY-MM-DD HH:MM** - entry`.

**2026-10-01 19:36** - `lymebooks` synced at Mike's request (Sonnet worker, about 4 minutes): the 7 local commits were rebased onto `origin/develop` with no conflicts; new tip `009e14b0` (old tip `5bf34763`), 0 behind, 7 ahead, tree clean, not pushed. The 29 incoming commits touched only plans, two PowerShell scripts and the website (8 files), so nothing in them is template-bound and `lymebooks-intake.md` needs no refresh. Build 0 errors; `WebApi.Tests` 836 of 836; `LymeStack.Tests` 7 failures, all SQL-backed tests against the dev `LymeBooks` database and inside the known 6 to 13 baseline range; `ng build --configuration development` now passes. Round two will be added to this plan as new milestones once Mike approves the intake groups and the drift audit; no separate planning document.

**2026-10-01 19:30** - T5 section 3 (template hands-on) passed, run headlessly by the orchestrator with the template's own Playwright setup and its `e2e/.env` account, because Mike is working from another machine: signed-out load, the Add User email check (message in red, Save disabled, a fresh address clears it), no message on a user's own address, the nav menu sort modal ("Sorting Admin Children", 7 items), Admin home and Users in light and dark. Screenshots in `.orchestrator/downstream-sync-plan/t5-review/lymestarter/`. Finding: sign-in on the template returned a 500 against the dev database `LymeTemplate` (`192.168.50.42\SQLEXPRESS`), which had never received `0022-security-hardening.sql`; `0001a-simpleauth-tables.sql` was also unjournaled and has no guards. With Mike's go-ahead the orchestrator journaled `0001a` as run and applied `0022` through DbUp; `--check` reports the schema current. Section 4 (visual sign-off) is being run the same way by one Sonnet worker per repo, in sequence because every app uses ports 4200 and 5218: `lymecrm` dispatched first, then `lymetimer`, `lymedeploy`, `ping`, `lymestats`. Workers change no source and commit nothing; screenshots and a manifest go to `t5-review/<repo>/` for Mike to judge.

**2026-10-01 19:03** - (Wall-clock time; the earlier entries today run about an hour ahead.) T5 section 1 closed in a resumed session: Mike accepted all 11 checklist decisions as recommended. `pmo-app` stays at the authorization fix (M7 does not apply there until the rebuild); `ping` brand pages and the `lymecrm` kept-by-design surfaces confirmed; the early `IAuthLogger` fix stays; `lymestarter` `ff2df33` is left as written; the `try/catch (DbUpdateException)` stays; anonymous `Auth/UserExists` is accepted for now. Follow-up work done by the orchestrator: `paymentz` now writes every registration to the audit log (`827e1ed`; build clean, 271 tests pass); `SimpleAuth4Net` production bundle error budget raised to 1.5 MB (`e2a1f66`; `npm run build` passes, warning only); `~/git/lymestarter/BACKLOG.md` created with D6 to D9 and the `Auth/UserExists` item (`59962b2`), which closes that part of M12. New round-two item: stop sending settings from anonymous `GET /AppConfig`, template first. Still open at T5: the two reviews, the hands-on checks (sections 3 to 5) and the push approvals. Nothing pushed.

**2026-10-01 19:55** - Usage for the orchestrated run up to the T5 gate (from Mike's session report): total cost \$145.01; wall time 1h 17m; API time across all agents 4h 3m 36s; 6,752 lines added and 4,247 removed. By model: Opus 5.5 \$84.38 (M2, M3, the five M9 ports, the intake and drift analysis), Fable 5.1 \$39.00 (the orchestrator), Sonnet 5.5 \$20.62 (fan-outs, tests, sweeps, gap-fills), Haiku 4.5 \$1.01 (the first M12 pass, redone on Sonnet). About 49 worker agents. A resume prompt is in `RESUME.md`.

**2026-10-01 19:50** - T4 complete with its gap-fill; the run is at the T5 gate.

- **`lymedeploy` gap-fill:** `Register` calls the post-registration handler for every user `ba375cf`, test `3ee8e81`. Its cookies already hardcode `Secure = true`, so no cookie change. Tests: the same 4 Cli.Tests and 5 LymeStack.Tests failures as before, nothing new.
- **State at the gate:** M2 through M10 and T1 through T4 are done, except M7 for `pmo-app` (held for Mike's scope answer), the M8 ports (waiting on Mike's review of the intake and the drift audit) and M12's final pass (push state in the ledger, D6 to D9 backlog items, closing this document). Every commit is local. Nothing was pushed or deployed. No worker entered `md-ccrs-dev`.
- **Not done, by design:** hands-on checks, visual sign-off, the `lymecrm` production spot check and all push approvals: `t5-checklist.md`.

**2026-10-01 19:40** - T4 gap-fill done in six of seven repos; T5 checklist drafted.

- **`lymecrm`:** logger `d4b26e0` with test `aeb7c20`, cookies `f6764eb`. LymeStack.Tests 2833, NasRemoteApi.Tests 128.
- **`lymestats` (18:47 to 19:10):** logger `ac05cb1`, `Register` handler `dc76393`, cookies `e55d7b6`. LymeStack.Tests 149. Local `main` moved up to `develop` again.
- Both workers found that the older mode tests in these repos set `AuthSettings:Mode` too late to take effect (they exercise Standalone), and that RelyingApp mode fails at startup because `UseRateLimiter` runs without a registered limiter; the template has the same shape (intake group G4).
- `lymedeploy` gap-fill still running.
- **T5 checklist** written to `t5-checklist.md` in this repo (uncommitted): 11 decisions with recommendations, the two reviews, the hands-on checks with start commands, the push table for 14 repos, and the items to carry into M11.

**2026-10-01 19:25** - M12 gap-fill complete (about 10 minutes); T4 gap-fill done in four of seven repos.

- **Trackers, corrected and verified by the orchestrator:** `lymestarter` `9f9e896` (inventory: the 2026-07 hardening rows restored with their real commits and pushed state; `ping` and `lymestats` rows added as scaffolded from the already-hardened template, migrations "not verified"), `6b62ae1` (`core-fix-log.md`: A/B, G, K, L, N/O, F filled; new sections H, I, M, P, J, C/D/E and today's template commits), `0a64478` (`security-port-plan.md` header). `claude-shared-settings` `8a227d2` (product note reworded; the skill's new fetch step had ahead and behind swapped, fixed; `lymestats` added to its branch list).
- **Project instructions** amended in `lymesend` `b4c0fbe`, `paymentz` `35f7d2a`, `open-mic-night` `80c3f95`, `qc-sod-ordering` `4e29380`: the hardening port "has been in place since 2026-08-26"; `[AllowAnonymous]` goes on the action. `playmusiconline` has a root file and a `pmo-app/` file; neither was edited (its hardening is still outstanding).
- **Pushed state of the 2026-07 hardening:** on origin everywhere except `sawgrass-v2` (`27096d3`, local only).
- `qc-sod-ordering` local `main` moved up to `develop` again (`4e29380`) after the amended commit.
- **T4 gap-fill so far:** `SimpleAuth4Net` `3ff1542` (logger; 28 tests, 7 fail without it); `lymetimer` `49dc74a` (logger), `1582f71` (cookies), 452 pass; `ping` `1f67577` (cookies), `3d39ac6` (`Register` handler), 148 + 354 pass; `lymebooks` `5bf34763` (cookies), 836 pass. Still running: `lymecrm`, `lymestats`, `lymedeploy`.

**2026-10-01 19:12** - T4 automated checks complete (about 10 minutes); gap-fill dispatched.

- **Probe:** J, K, L, M, N, O, P (and F, G) ✅ at HEAD in `lymecrm`, `lymebooks`, `lymetimer`, `lymedeploy`, `ping`, `lymestats`.
- **Builds and tests:** no new failures. Pre-existing, unchanged: `lymedeploy` Cli.Tests 4 and LymeStack.Tests 5; `lymestats` Integration.Tests 6 of 18; `lymebooks` 7 SQL-backed failures this run. Karma was not part of this pass (`lymedeploy` and `lymetimer` suites do not compile, pre-existing).
- **Tokenless calls (carried over from T3):** all 30 requests returned 401 (three admin Auth endpoints plus `GET ConfigSetting` and `ConfigSetting/1`, in each of the six repos), each API started against an unreachable database address.
- **Drift against template `283405f`:** `lymestack-admin` is down from 55 differing files per repo to 0 (`lymetimer` 2, both intentional). Every remaining difference in the five apps is intentional, a pending audit item (U1 to U14), or one of the three items below.
- **Found, no owning milestone:** three template fixes from M3 never reached the apps: `Secure` cookies with `CookieDomain` (`537daf5`, missing in all six); always-registered `IAuthLogger` (`18304c0`, missing in `lymecrm`, `lymetimer`, `lymestats`, and upstream in `SimpleAuth4Net`); `Register` calls `IPostRegistrationHandler` (`2fa0dd2`, missing in `lymedeploy`, `ping`, `lymestats`). **Gap-fill dispatched** (Sonnet, one worker per repo, seven repos including `SimpleAuth4Net` for the logger fix). The SimpleAuth-lineage apps get the logger fix with the intake's group G1.
- **Hygiene scan of the M9 diffs:** 0 banned classes and 0 inline `style` attributes in app pages in all five repos; the remaining raw colours are print rules, modal backdrops, chart series palettes and the kept-by-design surfaces.
- **Push state:** nothing pushed; 0 attribution hits in 87 unpushed commits.
- Appendix marker N corrected (the template now has 1 `AllowAnonymous` mention, not 2).

**2026-10-01 19:05** - M12 first pass (Haiku, 18:39 to 18:54) came back partial and partly wrong; gap-fill dispatched on Sonnet.

- **Done correctly:** `lymestats` and the `atcc-app` exclusion in the `port-core-fix` skill (`claude-shared-settings` `de26a31`); the `[AllowAnonymous]` wording in `paymentz` and `qc-sod-ordering`; the inventory's "How to stay in sync" note and per-repo quirks (`lymestarter` `c96ecd4`).
- **Wrong:** it treated the 2026-07 hardening port and today's work as one thing. In `downstream-inventory.md` it overwrote the hardening tracker's real commit SHAs with milestone labels and rewrote the status counts; in four repos' project instructions and in `products/lymestack.md` it wrote that the hardening port was "ported locally as of 2026-10-01 (unpushed)", which is false where that port was pushed in August.
- **Incomplete:** `core-fix-log.md` was edited but never committed, with errors (already-pushed rows flipped to unpushed; `sawgrass-v2`'s pre-rebase SHAs), and the H, I, M, P, J and C/D/E sections were not added.
- **Gap-fill** (Sonnet, not the plan's Haiku, because the task needs each SHA and push state checked against the repos): repair the inventory table, redo the ledger from the orchestrator's consolidated data, correct the five wrong statements. Model change is the orchestrator's call.
- 41 finished worker agents stopped at Mike's request. T4 is still running.

**2026-10-01 18:52** - M9 complete in all five repos; T4 dispatched.

- **M9 `lymedeploy` (18:14 to 18:33), 12 commits `497f19b..e7a60f5`:** tokens `497f19b`, 57 admin files `64ff1de`, `IAuthLogger` fix `7bfc18b`, F `8f70777` (28 tests), design layer onto `.ls-*` `6beddd0`, app pages `cc040fd` `0abbba2` `1a38b5e` `d0d9367` (9 pages and the quick-view drawer thoroughly), Settings note for non-admins `6912cd1`, docs `1b66ea9`, G `e7a60f5`. LymeStack.Tests 874 pass with the same 5 known failures; Cli.Tests the same 4. Production `ng build` green.
- **M9 `lymecrm` (18:13 to 18:35), 14 commits `83963bd..63121f4`:** tokens `83963bd`, 57 admin files `ea7b547`, bootstrap-icons `9909402`, F `b6ca9c7` (14 tests), Font Awesome to Bootstrap Icons in app pages `e0bab28` (about 1,080 usages), the app's `crm-*` page layer rebased onto `.ls-*` with class names kept for the e2e specs `36c4e54`, docs `7c0414e`, page passes `ef4fe13` `d1707f7` `80cf543` `fc3467d` `5160532`, G `119e6a1` (both `web.config` files), upstream tag `63121f4`. LymeStack.Tests 2831, NasRemoteApi.Tests 128, Karma 1002 of 1002; development and production `ng build` green.
- **M7 status:** every item is done in every listed repo except `pmo-app`, which waits for Mike's scope answer.
- **T4** dispatched (Sonnet): probe J to P, fresh builds and tests in the six LymeStarter-lineage apps, the drift re-measure against template `283405f`, a hygiene scan of the M9 diffs, and the tokenless 401 checks T3 could not run in those six repos.
- `lymestarter` `main` has moved on to `e24d5e8` (the other orchestrator's sample-marker commits on top of `283405f`).

**2026-10-01 18:45** - M6 complete; M9 complete in `lymetimer` and `lymestats`.

- **M6 part b (18:26 to 18:30):** `qc-sod-ordering` local `main` and `develop` both at merge `788b3bf` (`git diff main develop` empty; 118 tests pass on `main`; no inline HMAC write). `lymestats` local `main` fast-forwarded to `191cbd1`. Nothing pushed; no branch created or deleted. `playmusiconline`, `ping` and `lymecrm` `main` untouched.
- **Branch table (read 18:26; ahead/behind):**

| Repo | Working | Deploy | Local working vs origin | Local deploy vs local working | origin deploy vs origin working |
|---|---|---|---|---|---|
| `SimpleAuth4Net` | master | master | 40/0 | same branch | same branch |
| `lymestarter` | main | main | 16/0 | same branch | same branch |
| `lymecrm` | develop | main | 9/0 (still moving) | 0/9 | 0/0 |
| `lymebooks` | develop | main | 6/0 | 0/499 (stale local `main`) | 0/0 |
| `lymetimer` | main | main | 19/0 | same branch | same branch |
| `lymedeploy` | main | main | 9/0 (still moving) | same branch | same branch |
| `ping` | develop | main | 14/0 | 0/14 | 1/0 |
| `lymestats` | develop | main | 14/0 | 0/0 | 0/0 |
| `lymesend` | main | main | 4/0 | same branch | same branch |
| `sawgrass-v2` | main | main | 10/0 | same branch | same branch |
| `paymentz` | main | main | 5/0 | same branch | same branch |
| `open-mic-night` | main | main | 6/0 | same branch | same branch |
| `qc-sod-ordering` | develop | main | 11/0 | 0/0 | 3/8 |
| `playmusiconline` | develop | main | 2/0 | 0/55 | 0/89 (left alone) |

- **M9 `lymetimer` (18:10 to 18:24), 11 commits `4e46521..7942c06`:** tokens and components `4e46521`, 57 `lymestack-admin` files `2017dd6` (App Health keeps the app's Send Test Email card), F `44456ef` + tests `9029a57`, global `.tnum` `ca7fba2`, app pages `9242140` `3e06ca6` `c17a774` `8eda66e` (14 of 16 routes thoroughly), docs and project-instructions pointer `85f889c`, G `7942c06` (only `web.config` needed flipping). 452 tests pass; development and production `ng build` green.
- **M9 `lymestats` (18:10 to 18:25), 8 commits `5b1ac46..191cbd1`:** tokens `5b1ac46`, 57 admin files `fabc2cc`, F `b258d12`, chart tokens and `.tnum` `065400d`, analytics pages `d6ccc7f` `2a2e86e`, home and about `1f24175`, docs `191cbd1`. LymeStack.Tests 145, Api 113, Ingest 156, Karma 164; both `ng build`s green. No root project instructions file; none created.
- Still running: M9 in `lymecrm` and `lymedeploy`.

**2026-10-01 18:38** - T3 complete (about 7 minutes): nothing failed. M9 complete in `ping`.

- **T3 probe:** A and B ✅ in all 14 repos; C, D, E ✅ wherever in scope, with no ⚠️ left for `sawgrass-v2` or `qc-sod-ordering`; N and O ✅ in all seven LymeStarter-lineage repos (the two controllers are byte-identical to the template's in all six apps). Every `AuthController` action carries exactly one attribute; no controller has a class-level `[AllowAnonymous]` above an `[Authorize]` action.
- **HMAC:** the only `HMACSHA512` in the 13 ported repos is the legacy verify path in `SimpleAuthPasswordHasher`. `pmo-app` still writes inline (expected, D1).
- **Builds and tests (fresh, 8 repos):** all build; every failure is on the known list (`sawgrass-v2` 11, `open-mic-night` 1, `qc-sod-ordering` integration 19).
- **Anonymous calls over HTTP:** the three admin endpoints return 401 in the 8 repos that could be run (`pmo-app` has no `UnlockUser`: 404). Each API was started against an unreachable database address, so no database was touched.
- **Ancestry and push state:** all 50 port commits are ancestors of their working-branch HEAD; nothing is pushed anywhere; no attribution in any commit message.
- **Not re-run:** `lymecrm`, `lymetimer`, `lymedeploy`, `ping`, `lymestats`, `lymebooks` had workers in them, so their builds and 401 checks move to T4.
- **M9 `ping` (18:10 to 18:18):** tokens and components `9046c95`, 57 `lymestack-admin` files `365038c`, always-registered `IAuthLogger` `840d3ed` (the F tests need it), F `5418400`, About restyled `e09351a`, docs `4f02c76`. Two brand-page edits were backed out after the orchestrator's ruling (`d6684ca`, `7007277`): PinG's own pages and account screens are unchanged by design. LymeStack.Tests 146, Ping.Tests 354; development and production `ng build` green. `ping` has no root project instructions file; none was created, and the design-system pointer is in `docs/README.md`.

**2026-10-01 18:30** - M7 item F complete in `lymebooks` (18:07 to 18:13): `00816ca8` (API, tests, `lymestack-todos.md` entry) and `c98d10ce` (user form). `WebApi.Tests` 836 pass.

- **Tenancy decision:** email uniqueness is installation-wide (`AppUser` has no `TenantId`; `IX_AppUser_Email` is unique on the address alone), so no schema change. `Auth/EmailExists` requires AppRole `Admin`, which only installation staff hold; tenant Owners and Bookkeepers get 403, and support sessions are denied the route.
- **Angular build fixed as a side effect:** TypeGen had been loading a stale `WebApi/WebApi/bin/Release/net10.0/WebApi.dll` from August and generating 97 of about 270 `_api` files. The worker moved that gitignored folder aside; the development `ng build` is now green. Other clones that ever did a Release build may have the same trap (note for M12's inventory update).

**2026-10-01 18:27** - M4 complete (L2 half 18:08 to 18:12); M9 and T3 dispatched.

- **N, O commits (plus tests):** `lymecrm` `84cc373` `8c83115` `8f255e3` · `lymetimer` `9b0a18d` `4f88fa9` `c83f95e` · `lymedeploy` `cb46e00` `e636e95` `355734c` · `ping` `9ab26d4` `a5c6621` · `lymestats` `559ad1f` `4338526` `eb620ee`. Each repo also got the `AppConfigController` secret-name filter.
- **Same finding in all five:** the pre-login bootstrap reads only `GET /AppConfig`; nothing reads `/ConfigSetting` before login or without a user token (checked per app: `NasRemoteApi` and device/portal controllers in `lymecrm`; agents and the CLI in `lymedeploy`, whose `doctor` keeps working through the filtered `/AppConfig`; ingest in `lymestats`). The feedback submit POST stays anonymous and can no longer overwrite a row.
- No new test failures. HTTP-level 403 tests were ported where the repo could host them (`ping`, `lymedeploy`); elsewhere attribute and direct-controller tests pin the same behaviour.
- **M9** dispatched (Opus, one worker per repo), template reference `283405f`. Each M9 worker also ports F (the template user form it adopts calls `Auth/EmailExists`); G rides along in `lymecrm`, `lymetimer`, `lymedeploy`; the upstream tag in `lymecrm`. `ping`'s brand pages are left alone by design (T5 list).
- **T3** dispatched (Sonnet). It verifies committed state only in the six repos with active workers and builds for real in the other eight; ledger updates come back in its summary because the `lymestarter` tree is in use by the other orchestrator.

**2026-10-01 18:20** - T2 complete (18:05 to 18:06); M6 part a complete; M4 L2 half dispatched.

- **T2 (`283405f`):** 23 new tests (161 pass in total): `ConfigSetting` and `UserFeedback` refuse anonymous (401) and non-admin (403) callers; the anonymous feedback POST with an existing id creates a new row and leaves the original alone; anonymous `/AppConfig` omits secret-looking settings; `Register` calls `IPostRegistrationHandler` for first and later users. No M3 defects found. Production `ng build` green with budget warnings only. `lymestarter` `main` fast-forwarded to `283405f` (ahead of origin by 15, unpushed).
- **M6a `ping`:** `origin/main` merged into `develop` as `87f14f7`. LymeStack.Tests 113, Ping.Tests 354, `ng build` green.
- **M6a `lymestats`:** G19 API half ported as `be15a68` (LymeStack.Tests 110, Api.Tests 113, Ingest.Tests 156). `LymeStats.Integration.Tests` fails 6 of 18 before and after (needs a SQL Server).
- **M4 L2 half** dispatched (Sonnet): `lymecrm`, `lymetimer`, `lymedeploy`, `ping`, `lymestats`. References: N `404a8f0`, O `c235db9`. M9 follows in each repo as its L2 worker finishes.
- The other orchestrator has a worker back in `~/git/lymestarter`; this plan writes nothing more to that tree until it is free.

**2026-10-01 18:16** - M10 items K, L, M, P complete; M7 done in the five SimpleAuth-lineage repos; M6 part a dispatched.

- **M10 `lymebooks`:** K `95ad666d`, L `4cd746bc` (deployed hostnames resolve as before), P `21ab84fa` (binder reads `LymeStackCore:Email`; `UseSmtpPickup` removed from core). `WebApi.Tests` 825 pass. **P needs LymeDeploy variables renamed before deploy** (T5 list, M11).
- **M7 `sawgrass-v2`:** F `60461d8`, G `d824692`, H `3e8ee33`, I `ea6d59f`, project-instructions corrections `1cba91e`. 447 pass, the same 11 pre-existing failures. Angular build fails before and after (TypeGen broken there).
- **M7 `qc-sod-ordering` (`develop`):** upstream tag `9cbe315`, F `6f50903` (PrimeNG idiom), G `d4382c7`, I `c2e401d`. `WebApi.Tests` 118 pass; `ng build` green.
- The orchestrator reworded two unpushed commit subjects that named the instructions file (`sawgrass-v2` `4412078` → `1cba91e`; `qc-sod-ordering` `96863fb` → `9cbe315`, which also changed the three SHAs after it: F `51bb1b3` → `6f50903`, G `3aeec41` → `d4382c7`, I `fb115db` → `c2e401d`). Content unchanged.
- **Dispatched:** M7 item F in `lymebooks` (Opus; tenancy-aware), M6 part a in `ping` and `lymestats` (G19 onto `develop`). M7 for the other LymeStarter-lineage repos rides with M9, because the template's user form they adopt already contains F. T3 prompt is ready and goes out after the L2 half of M4.

**2026-10-01 18:10** - M3 complete (17:55 to 18:03) and merged into `lymestarter` `main`; T2 dispatched; more M7 and M10 results.

- **M3 commits:** N `404a8f0` (both `ConfigSetting` GETs Admin-only; `AppConfigController` drops secret-looking setting names, added mid-flight from the intake finding), O `c235db9` (GETs Admin-only; the anonymous POST can no longer overwrite a row by id), F `32827b9` + `b648b3e`, J build fix `3ef72e2` (the design-system port had taken `nav-menu-form`'s html without its `.ts`), `Register` calls `IPostRegistrationHandler` again `2fa0dd2`, `Secure` cookies with `CookieDomain` `537daf5`, always-registered `IAuthLogger` `18304c0`, tests `6c6127c`, `core-fix-log.md` sections `36f3416`. No test seam was needed in the template.
- **N finding:** the pre-login bootstrap reads only `GET /AppConfig`; nothing calls `/ConfigSetting` before login; Angular reads it only on admin pages.
- **Merge:** the other session had committed its samples to `main` (`1ce8193`, `ff2df33`). The orchestrator merged `main` into the worktree as `b60d1b3` (clean, no conflicts), rebuilt (138 tests pass, dev `ng build` green), then fast-forwarded `main` to `b60d1b3`. No branch was created. T2 continues in the worktree.
- **M7 `lymesend`:** F `d01b5e6` (111 tests). G was already there since `5ad96e9`; the probe missed it because the csproj element has a `Condition` attribute (appendix corrected). LymeDeploy does not override the environment for any app; TeamCity passes `-p:EnvironmentName=Production`.
- **M7 `paymentz`:** F `45ce697`, H `23f539e`, I `74e9303`; G already present (`e2768fe`). 271 tests pass.
- **M7 `open-mic-night`:** H `744ab29` (18 backslash-named files removed, ignore rule added), G `f6b4a1c`, F `a7c1011`, I `913dd2a`. 88 of 89 pass (the same 1 pre-existing failure).
- **M10 `lymedeploy`:** K `df464f6`, M `a825192`. Pre-existing failures unchanged (Cli.Tests 4, LymeStack.Tests 5). DbUp was not run.
- Still running: M7 in `sawgrass-v2` and `qc-sod-ordering`, M10 in `lymebooks`.

**2026-10-01 18:12** - M8 intake classification complete (17:53 to 18:00); M10 done in three repos.

- **Intake (`~/git/lymestarter/lymebooks-intake.md`, uncommitted; LymeBooks `34175437` moves the 52 design-pass entries to Completed):** of 189 pending entries, 20 to port, 2 in progress (N, O), 5 need Mike, 74 already upstream, 88 app-specific. Eight port groups: G1 SimpleAuthNet DI fixes (L1), G2 register clears the previous session (L1), G3 shell session and loading, G4 RelyingApp client and host, G5 modal-host dialog, G6 dark mode and styling, G7 quick-launch Recents, G8 nav visibility hook. Six questions for Mike with recommended answers.
- **Corrections to this plan's assumptions:** the template has no multi-tenancy, so the "tenant-context incident fixes" group mostly does not apply; the "ConfigSetting cache" group is already upstream; the template already has the invite-token overload and `RegisterModel.InviteToken` (only SimpleAuth4Net lacks them). About 10 generic fixes filed as Completed in LymeBooks never reached the template (question 6).
- **For M3, sent to the worker mid-flight:** the template's anonymous `AppConfigController` returns every `ConfigSetting`; LymeBooks `a9452db9` filters secret-looking names. Without it, N alone leaves the leak open.
- **M10:** `lymestats` K `1aee758` (107 tests pass); `lymetimer` K `55a5b9d`, L `b5003db` (419 pass; deployed hostnames resolve exactly as before, the manual `useIIS` toggle is replaced by the template's `document.baseURI` logic; `ng build` not run, only `tsc`); `ping` K `38b0858` (LymeStack.Tests 110, Ping.Tests 354; its `success`-flag check kept). `lymedeploy` and `lymebooks` still running.
- **Noted:** building `ping` rewrites three committed `_api` files (its committed TypeGen output is stale); the orchestrator restored them. K assumes the LymeSend API accepts `attachments[]`; `lymesend`'s `SendEmailRequest` has the field.

**2026-10-01 18:08** - M5 complete; M7 and M10 started early on idle repos.

- **`sawgrass-v2`:** rebased cleanly onto `origin/main` (pre-rebase HEAD `512462f`, kept in the reflog). SHA mapping: unlock `b229593` → `cf87cb3`; Argon2id `3491a28` → `27096d3`; M4 fix A `37194ed` → `6216eb3`; M4 fix B `512462f` → `c3237b2`. New `ddc4fec` converts origin's G19 from inline HMAC to `SimpleAuthPasswordHasher`. Ahead 5, behind 0. Tests: 438 pass, the same 11 pre-existing failures. `UserMigrationService` already used the hasher. Migration `2026-08-26 - Security hardening.sql` exists, unapplied (Azure firewall; M11).
- **`qc-sod-ordering` (`develop`):** G19 cherry-picked from `0363f69` and converted to the hasher as `c3331ae`. `WebApi.Tests` 110 pass. For M6: expect a conflict in `AppUserController.cs` and `AppUserControllerTests.cs` when merging `develop` into `main`; take the `develop` side.
- Verified in both: `HMACSHA512` appears only in `SimpleAuthPasswordHasher.cs` (legacy verify), nothing pushed.
- **Early starts (Mike asked for more fan-out; these repos were idle and the references are already tested):** M7 in `lymesend`, `paymentz`, `open-mic-night`, `sawgrass-v2`, `qc-sod-ordering` (items F, G, H, I as applicable, from the SimpleAuth4Net reference T1 tested); M10 in `lymetimer`, `lymedeploy`, `ping`, `lymestats` (items K, L, M from template commits already on `main`). T3 still sweeps every repo afterwards. M7 for the LymeStarter-lineage repos waits for the design system (M9), since F's form feedback uses it.
- **Held:** M7 for `pmo-app`, pending Mike (run policy and M7 disagree on scope).
- `lymestarter` `main` moved to `ff2df33` (the other session committed its samples), so M3's worktree commits will be merged in rather than fast-forwarded.

**2026-10-01 18:04** - M4 L1 half complete in all 7 repos (17:55 to 18:03); M8 drift audit complete; M5 dispatched.

- **Verified by the orchestrator in every repo:** no class-level `[AllowAnonymous]` on `AuthController`, both commits on the working branch, logger fix present, nothing pushed, clean commit messages.
- **Commits (A, B):** `lymetimer` `4df9db3` `e49c9c2` · `lymesend` `35a0156` `283f9f5` · `paymentz` `147f33d` `8fd4f43` · `sawgrass-v2` `37194ed` `512462f` · `open-mic-night` `a2a21a1` `cbd6f5d` · `qc-sod-ordering` (`develop`) `7f16632` `26ceaf8` · `pmo-app` (`develop`) `f47ecc6` `ea37760`.
- Every repo has the global authenticated-user filter (`AddSimpleAuthDefaultAuthorization`), so an unmarked action requires sign-in. Every repo got the reflection and admin-role tests. No new test failures anywhere; pre-existing ones: `sawgrass-v2` 11, `open-mic-night` 1.
- `pmo-app` has one action the reference lacks (`RefreshTokenPost`, public by necessity) and no `UnlockUser`. No other repo has app-specific `AuthController` actions.
- No client calls a newly protected action before login in any repo.
- Other class-level `[AllowAnonymous]` controllers exist (webhooks, public payment pages, API-key send) but none sits above an `[Authorize]` action. Three items queued for T5.
- `paymentz` and `open-mic-night` trees show the backslash-named TypeGen output again after building (item H, M7).
- **M8 drift audit** (`core-drift-audit.md` in this repo, uncommitted until Mike has seen it): 69 files, no reverts; 23 upstream verdicts grouped into 14 items (U1 to U5 are L1). Headlines: the template's error interceptor logs the user out on any 401 (`ping` and `lymetimer` fixed it locally); `AuthService.log()`/`error()` recurse infinitely in the template and here; LymeSend returns 200 on a failed send and only `ping` checks the body; a broken spec import stops Karma in the template.
- **M5** dispatched: `sawgrass-v2` (Opus) and `qc-sod-ordering` (Sonnet).

**2026-10-01 17:57** - T1 complete (17:50 to 17:54; `7da6c02`, `bd4dd98`; orchestrator re-ran `dotnet test`: 28 passed). Wave 2 dispatched.

- **T1:** reflection test, admin-role tests (anonymous 401, non-admin 403), `EMAIL_EXISTS` tests including a mutation check. Live check on port 5299 with no token: `UnlockUser`, `RevokeAllSessionsForUser`, `RevokeAllSessions`, `SetupAuthenticator`, `EmailExists` all 401; `Login`, `ForgotPassword`, `RefreshToken` reachable. No defects found in M2's work.
- **Test seam `7da6c02` (production code):** `SimpleAuthContext` now takes `DbContextOptions` and only configures SQL Server when none are supplied; the two raw role queries use `ExecuteSqlInterpolated`. Behaviour-neutral on SQL Server. The template needs the same seam for the tests (M3).
- **T1 observation:** `AuthController` cannot be constructed when `AuthSettings:AuditLogging:Enabled` is false (`IAuthLogger` is only registered when enabled). LymeBooks already fixed this; handed to M3 and the M8 intake.
- **Open in T1:** the production `npm run build` budget failure (pre-existing) and the push, both at T5.
- **M3** dispatched (Opus) in a detached worktree of `lymestarter` at `b8ec996`, because the main tree holds uncommitted sample-feature work from the `lymetools-port-plan` session. No branch is created; the orchestrator merges M3's commits into `main` once that tree is clean. This session is also registered in `~/git/lymetools/.orchestrator/active-sessions.json` so the other orchestrator sees the overlap.
- **M4 L1 half** dispatched: `lymetimer`, `lymesend`, `paymentz`, `sawgrass-v2`, `open-mic-night`, `qc-sod-ordering` (Sonnet), `pmo-app` (Opus). `lymetimer` gets a second worker for N/O after T2 rather than one worker for both, so the L1 half does not wait on the template.
- **M8 started early, analysis only** (17:53, two Opus workers, on Mike's instruction to fan out): the LymeBooks intake classification (writes `~/git/lymestarter/lymebooks-intake.md`, uncommitted, and moves the 52 design-pass entries in LymeBooks) and the core drift audit (writes `core-drift-audit.md` in this repo). Nothing is ported until T2 is green and Mike has reviewed the intake.

**2026-10-01 17:50** - M2 complete (17:42 to 17:48, 12 local commits `8417557..ced452a`, nothing pushed; `origin/master` still `4d61814`). Verified by the orchestrator: no class-level `[AllowAnonymous]`, all 22 `AuthController` actions carry exactly one attribute, the three admin actions and the new `EmailExists` require `Admin`, markers B/G/F present, test project in the solution, obsolete documents removed.

- **A** `8417557`, **B** `7337a1c`, **G** `524264f` (also excludes `appsettings.Development.local.json`, which holds dev DB credentials, from publish), **F** `86c99bd` (API) + `f978131` (UI), triage upstreams `ccf9096` `5ed59d6` `018ef0a`, test project `b4f094f`, README `5b79514`, housekeeping `1af5759`, docs `ced452a`.
- **F deviations:** error code is `EMAIL_EXISTS` (already used here, in the template and across the fleet), not `EMAIL_TAKEN`. `EmailExists` is `[Authorize(Roles = "Admin")]`.
- **Triage result:** upstreamed here: `AddSimpleAuth(configuration)`, explicit cookie-then-Bearer read, monthly dev SMS log folders, `AppUser` include on refresh-token reuse detection. Deferred to M8: `IPostRegistrationHandler` invite-token overload and `RegisterModel.InviteToken` (dead code in the template; only LymeBooks uses them). Everything else is intentional template adaptation.
- **For M3 (template defects found by the triage):** the template's `Register` never calls `IPostRegistrationHandler`; its `CookieDomain` cookies do not set `Secure`; add `appsettings.Development.local.json` to the publish exclusion if the template uses that file.
- **For M7:** the G fan-out should exclude `appsettings.Development.local.json` wherever it exists.
- **Build:** `dotnet build` clean (131 warnings, 2 fewer than before), `dotnet test` 1/1. Production `npm run build` fails the bundle budget, pre-existing; the development build compiles. Three items queued for T5.
- T1 dispatched to a Sonnet worker.

**2026-10-01 17:45** - Orchestrator session started (state in `.orchestrator/downstream-sync-plan/`, gitignored). Checked before dispatch: no M2 commits after `9e75a85` (only two commits to this document), working tree clean, class-level `[AllowAnonymous]` still present, and the only other context open in this repo is the idle planning session. M2 dispatched to an Opus worker. Noted for M3: `~/git/lymestarter` has uncommitted work from the separate `lymetools-port-plan` session (sample features); re-check before sending a worker there.

**2026-10-01 (evening)** - M1 closed. All eleven decisions recorded in 1.9. `lymebooks` local branches `accounting-remediation` (`f85aeb15`, still on origin at the same commit) and `backup/pre-rewrite` (`d51c299a`, local only) deleted on Mike's go-ahead: every `accounting-remediation` commit has a patch-identical twin in `develop`'s history, and `backup/pre-rewrite`'s one unique commit (unlock + Reset Password modal) was redone on `develop` as `e701d906`. Mike will run the rest of the plan through `/iadev:orchestrator` in a new context, with hands-on gates deferred to the new T5.

**2026-10-01 17:14** - `lymecrm` pushed, branches reconciled, and deployed to Production (Mike's instruction; no active users).

- Merged `origin/main` (G19 `6017be8`) into `develop` as `4dd7811`; pushed `develop`; fast-forwarded `main` to the same commit and pushed. `develop`, `main` and both origin refs are all `4dd7811`. The merged tree differs from the previous `origin/main` only in `BACKLOG.md`, so no code reached `main` that was not already there.
- **Deploy state, from LymeDeploy:** release `2.0.0.22` (built 2026-09-26, minutes after the G19 commit) was already on UAT. Production was on `2.0.0.21` (2026-09-04). Promoted `2.0.0.22` UAT → Production with `lymedeploy promote`; it finished `succeeded` at 17:13. Not verified by signing in; Mike to spot-check.
- The push did not produce a new TeamCity build (no `2.0.0.23` minutes later). Expected: the VCS root's checkout rules are `+:ng-app`, `+:WebApi`, and the push changed only `BACKLOG.md`.
- `lymecrm` does have a LymeDeploy Production environment, which the roster had listed as unconfirmed.

**2026-10-01 ~17:08** - `lymecrm` branch cleanup (Mike's go-ahead). Deleted 24 local `worktree-agent-*` branches whose commits are patch-identical to `develop`, and removed their 12 worktrees (all clean, no uncommitted files). No remote copies existed. Tips, for recovery before `git gc`: `3ff1fc7 d06554b eead3db 4bd985f 79fa2e5 5cea73c 5c413d1 7cac728 d2a8388 2294d2c a888b3c a553569 ada6b5e 0d0f4f1 57ef23c 28e8013 4e3c09b f558681 9d7b97b e8b10f6 9a8d9e1 7541080 d568829 0f09da4`.

- **5 branches held for Mike's confirmation.** M0 reported them as carrying work `develop` lacks; on inspection none does. Each feature commit has a same-subject twin on `develop`: J2-2 `958413e` → `46ba2dc`, P9 `da780f9` → `64a5a19`, P12 `44abe37` → `e9a2ffe`, P29 `67fc01e` → `f9c67cf`. Of 5,840 substantive added lines, 60 are absent from `develop` HEAD, all constructor/signature lines later refactored. The only content that exists nowhere else is two throwaway files: `worker-summary-p7-reference-mappers.md` (`0958e99`) and `worker-summary-p12-tender-capture.md` (in `44abe37`).
- Held: `worktree-agent-a27f245…` (has worktree), `-a35471c…`, `-a3760b3…`, `-a4054db…`, `-ab0bf01…` (has worktree).
- **Minutes later, Mike confirmed; the 5 held branches and their 2 worktrees are deleted.** Tips: `958413e 0958e99 da780f9 44abe37 67fc01e`. `lymecrm` now has no `worktree-agent-*` branches and no extra worktrees.

**2026-10-01 17:01** - M0 complete. Nothing pushed, no branches created or deleted, no conflicts, no dirty repos.

- **Level with origin (0 behind):** `lymebooks` `14f49547` (was 395 behind, not 379), `lymedeploy` `124244c`, `paymentz` `17dd783`, `open-mic-night` `d6ec759`, `playmusiconline` `0e5a8df`.
- **Rebased, unpushed:** `lymetimer` `27d6499` (ahead 1), `lymesend` `f35587c` (ahead 1), `lymestarter` `b8ec996` (was `4ea9c23`, ahead 1; 110 tests pass), `SimpleAuth4Net` `ea622c8` unlock (was `160a9db`) + `b037138` this document (ahead 2; build clean).
- **`sawgrass-v2` (untouched, HEAD `3491a28`, behind 3 / ahead 2):** local-only `3491a28` (Argon2id), `b229593` (unlock); origin-only `3159146` (G19), `2ec26bd` (merge), `ebd2f83` (CLAUDE.md conventions). No textual conflict (`git merge-tree` clean). The conflict is semantic: origin's G19 writes inline HMAC in `AppUserController.cs` and `AppUserControllerTests.cs`, while local adds `SimpleAuthPasswordHasher`.
- **`paymentz` stray directory:** 88 untracked TypeGen-generated `.ts` files; deleted.
- **Stale branches, `lymebooks`:** `accounting-remediation` has nothing `develop` lacks (still tracks origin); `backup/pre-rewrite` has one merge commit `d51c299a`.
- **Stale branches, `lymecrm`:** 29 `worktree-agent-*`, not 25. 24 are fully patch-identical to `develop`. 5 are not: `a27f245` "Add sale detail / receipt read API (J2-2)"; `a3760b3` "P9: customer import with synthetic source key and scored duplicate review"; `a4054db` "POS P12: tender + payment capture"; `ab0bf01` "Add POS reporting: deposit, sales, inventory, purchasing, orders, returns (P29)"; `a35471c` "P7: add worker summary" (its other commit is identical).
- **Probe at local HEAD, all 15 repos:** matches tables 1.3 and 1.4. Only expected deviations: `SimpleAuth4Net` D reads ✅ locally; `lymecrm`, `ping`, `qc-sod-ordering` show their known branch splits.
- **Tooling:** brew's x86_64 `dotnet` fails at the TypeGen post-build step on this arm64 Mac; use the arm64 SDK (added to the ground rules).

**2026-10-01 14:15** - Inventory taken. All 15 repos fetched and probed at their origin tips; trackers in `~/git/lymestarter` read at `origin/main` (the local clone was 7 commits behind). Item A verified by reading `AuthController.cs` at lines 27–30 and the four `[Authorize]` actions in this repo; not exercised against any running system. Items N and O taken from LymeBooks' `lymestack-todos.md` and a marker count, not yet from a diff. Production deployment state and production DB migration state are **unknown** from here and are M11's first job. Nothing was changed in any repo other than adding this file.

[Return to Top](#downstream-sync--inventory--implementation-plan)
