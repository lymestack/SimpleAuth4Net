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
| [M2 — SimpleAuth4Net catch-up (L1 upstream)](#m2--simpleauth4net-catch-up-l1-upstream) | Opus | 🔄 In Progress | — | Auth hole, logger, publish env, email check. Dispatched by the orchestrator 2026-10-01 17:45 (the earlier hand-issued prompt was never run) |
| [T1 — Test SimpleAuth4Net](#t1--test-simpleauth4net) | Sonnet | ⬜ Not Started | — | Unit/integration |
| [M3 — LymeStarter template catch-up](#m3--lymestarter-template-catch-up) | Opus | ⬜ Not Started | — | Template is the L2 diff base |
| [T2 — Test the template](#t2--test-the-template) | Sonnet | ⬜ Not Started | — | Unit/integration + build |
| [M4 — Urgent security fan-out](#m4--urgent-security-fan-out) | Sonnet | ⬜ Not Started | — | 7 repos (L1) + 5 repos (L2) |
| [M5 — Finish the Argon2id port](#m5--finish-the-argon2id-port) | Opus | ⬜ Not Started | — | sawgrass-v2, qc-sod-ordering (pmo-app deferred per D1; atcc-app excluded per D2) |
| [T3 — Security verification sweep](#t3--security-verification-sweep) | Sonnet | ⬜ Not Started | — | Probe + build + anonymous-call checks |
| [M6 — Branch reconciliation](#m6--branch-reconciliation) | Sonnet | ⬜ Not Started | — | develop ↔ main, stale branches |
| [M7 — Remaining L1 fan-out](#m7--remaining-l1-fan-out) | Sonnet | ⬜ Not Started | — | Email check, publish env, TypeGen, G19 gaps |
| [M8 — LymeBooks → template intake](#m8--lymebooks--template-intake) | Opus | ⬜ Not Started | — | 189 pending entries to triage |
| [M9 — Design system fan-out](#m9--design-system-fan-out) | Opus | ⬜ Not Started | — | 5 LymeStarter-lineage apps |
| [M10 — L2 small-fix fan-out](#m10--l2-small-fix-fan-out) | Sonnet | ⬜ Not Started | — | Attachments, bootstrap, `--check`, transport |
| [T4 — L2 verification](#t4--l2-verification) | Sonnet | ⬜ Not Started | — | Build/tests + hands-on visual pass |
| [T5 — Deferred hands-on gate](#t5--deferred-hands-on-gate) | — (Mike) | ⬜ Not Started | — | Every hands-on check and push approval, batched at the end |
| [M11 — Production: migrations and deploys](#m11--production-migrations-and-deploys) | Sonnet (Mike-driven) | ⬜ Not Started | — | Migrate before deploying code |
| [M12 — Trackers and registration](#m12--trackers-and-registration) | Haiku | ⬜ Not Started | — | Inventory rows, skill lists, tags, docs |

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

- [ ] `lymestarter/downstream-inventory.md` has no rows for `lymestats` or `md-ccrs-dev/atcc-app`.
- [ ] Its 2026-09-26 note says `qc-sod-ordering` and `sawgrass-v2` are unported for Argon2id. Both ports exist — on `develop`, and unpushed on this machine, respectively. The other machine never saw them.
- [ ] `lymestarter/core-fix-log.md` has no entries for `8c201de` (A, B), `3751db1` (G), `9ce88cd` (L), `b8ec996` (K) or `18a9108` (backslash TypeGen files), so none of them has ever had a tracked fan-out.
- [ ] `lymestarter/security-port-plan.md` status header still reads "MED batch next".
- [ ] The `port-core-fix` skill's member lists omit `lymestats` and `atcc-app`.
- [ ] `> **Upstream:**` tag missing from `CLAUDE.md` in `lymecrm`, `qc-sod-ordering` (`develop`), `playmusiconline`, `md-ccrs-dev`. `ping` and `lymestats` have no root `CLAUDE.md` at all.
- [ ] LymeBooks `lymestack-todos.md`: 52 design-pass entries are ported but still under Pending.
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

- [ ] **A:** remove class-level `[AllowAnonymous]` from `AuthController`; add `[AllowAnonymous]` to each action that must be public, using lymestarter `8c201de` as the reference list. Drop the dead `is VerifyOtpModel` check in `VerifyMfa`.
- [ ] **B:** `DefaultAuthLogger.WriteLog` takes and logs `eventType`.
- [ ] **G:** `<EnvironmentName>Production</EnvironmentName>` and `CopyToPublishDirectory="Never"` for `appsettings.Development.json` in `WebApi.csproj`; flip `web.config` to `Production`.
- [ ] **F:** implement email duplicate validation per `simple-auth-email-check-fix.md` — `Auth/EmailExists`, the `EMAIL_TAKEN` check in `AppUserController.Post`, and the Angular user-form feedback. Decide the `EmailExists` authorization deliberately: an anonymous version is an account-enumeration oracle, which the 2026-07 hardening closed elsewhere. Default: `[Authorize(Roles = "Admin")]`. No `try/catch` in the controller.
- [ ] UI for F: match the existing Material form's hint/error treatment exactly — spacing, colour tokens and icon weight consistent with the username-availability feedback beside it, no inline `style` attributes.
- [ ] Triage the `SimpleAuthNet` differences between this repo and `lymestarter` (11 files, [1.5](#15-upstream-bound-backlog)): list each as *intentional template adaptation* or *should be upstreamed here*, and upstream the latter.
- [ ] If D11 = yes: add `WebApi/WebApi.Tests` (xUnit) to `WebApi.sln`.
- [ ] If D10 = default: note in `README.md` that `react-app` and `vue-app` are unmaintained.
- [ ] Delete `PLAN-smoke-test.md`; replace this repo's `downstream-inventory.md` with a one-line pointer to `~/git/lymestarter/downstream-inventory.md`; delete `simple-auth-email-check-fix.md` once F is in.
- [ ] Run "update docs" (`documentation/api.md`, `README.md`) for unlock, G19, F and the authorization change.
- [ ] Commit locally as separate commits per item.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## T1 — Test SimpleAuth4Net

**Model:** Sonnet · **Mode:** unit/integration · **Depends on:** M2

- [ ] Write a reflection test: `AuthController` has no class-level `[AllowAnonymous]`, and every action carries exactly one of `[Authorize]` / `[AllowAnonymous]`.
- [ ] Write a test that `UnlockUser`, `RevokeAllSessionsForUser` and `RevokeAllSessions` require the `Admin` role.
- [ ] Write tests for `EMAIL_TAKEN` on create, on edit to another user's address, and no false positive when editing a user without changing their own address (case-insensitive).
- [ ] Run the suite green: `cd WebApi && dotnet test`.
- [ ] Start the API locally (`cd WebApi/WebApi && dotnet run`) and confirm with `curl -X POST` and no token that the three admin endpoints return 401, while `Login`, `ForgotPassword` and `RefreshToken` remain reachable.
- [ ] `cd ng-app && npm run build` succeeds.
- [ ] **Deferred to T5 — do not pause here.** Push approval: this is the first push of `ea622c8`, and it must go together with A. Downstream workers read this repo's local clone, so nothing waits on the push.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## M3 — LymeStarter template catch-up

**Model:** Opus · **Depends on:** T1 · **Repo:** `~/git/lymestarter`

The template is the diff base for every LymeStarter-lineage worker. *Workers must complete all items below.*

- [ ] **N:** port LymeBooks' `ConfigSettingController` change. First establish what the pre-login Angular bootstrap actually reads from it, so removing anonymous access does not break startup; follow LymeBooks' working implementation.
- [ ] **O:** port LymeBooks' `UserFeedbackController` change.
- [ ] **F:** port email duplicate validation from M2 (column is `EmailAddress`; user form lives under `lymestack-admin/security/users/` and uses the `.ls-*` design system — follow `docs/design-system.md`, `.ls-inset--danger` / form-hint patterns, no bespoke SCSS).
- [ ] Port anything M2's triage upstreamed that the template lacks.
- [ ] **J:** finish the design-system port's "Built" box — `npm ci`, fix the esbuild arch issue if present (`npm install @esbuild/darwin-arm64 --no-save`), `npx ng build --configuration development`.
- [ ] Add the T1 reflection test to `WebApi/LymeStack.Tests`.
- [ ] Open `core-fix-log.md` sections for A/B, G, K, L, N/O and F with per-repo tables seeded from [1.3](#13-l1-backlog--simpleauth4net-owned-code) and [1.4](#14-l2-backlog--lymestarter-owned-code).
- [ ] Commit locally.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## T2 — Test the template

**Model:** Sonnet · **Mode:** unit/integration + build · **Depends on:** M3

- [ ] Write tests: anonymous `GET ConfigSetting` is refused; a non-admin cannot read another tenant's feedback; `EMAIL_TAKEN` cases as in T1.
- [ ] `cd WebApi && dotnet test` green (baseline before M3: 103+ tests).
- [ ] `npx ng build` green.
- [ ] **Deferred to T5 — do not pause here.** Hands-on, folded into one pass (no screenshots). Start with `cd WebApi/WebApi && dotnet watch run` and `cd ng-app && npm start`, open `http://localhost:4200`, sign in as an Admin (ask Mike for the current password — the one in older notes is stale and locks the account after 3 tries). Check: (1) the app loads past "Waiting for server…" while signed out, proving N did not break bootstrap; (2) Admin → Security → Users → Add User, enter an existing address, tab out, see the in-use message and a disabled Save; (3) the Admin home and Users list render in the `.ls-*` style in both light and dark mode.
- [ ] **Deferred to T5 — do not pause here.** Mike approves pushing `lymestarter` (includes `b8ec996`).

[Return to Top](#downstream-sync--inventory--implementation-plan)

## M4 — Urgent security fan-out

**Model:** Sonnet, one worker per repo (Opus for `pmo-app` and `atcc-app` if D1/D2 include them — 600+ line divergence) · **Depends on:** T1 for the L1 half, T2 for the L2 half

*Workers must complete all items below.*

**L1 half — items A + B**, reference SimpleAuth4Net's M2 commit:

- [ ] `lymetimer` (production)
- [ ] `lymesend` (production)
- [ ] `paymentz` (production)
- [ ] `sawgrass-v2` — apply on top of the local unpushed commits; do not resolve the origin divergence here (M5)
- [ ] `open-mic-night`
- [ ] `qc-sod-ordering` — on `develop`
- [ ] `playmusiconline/pmo-app` — in scope per D1
- [x] ~~`md-ccrs-dev/atcc-app`~~ — excluded per D2; do not open the repo

For each: enumerate that repo's own actions before deciding which are public — several have app-specific endpoints the reference knows nothing about. Add the reflection test where a test project exists.

**L2 half — items N + O**, reference lymestarter's M3 commit:

- [ ] `lymecrm`
- [ ] `lymetimer`
- [ ] `lymedeploy`
- [ ] `ping`
- [ ] `lymestats`

For each: confirm the app does not read `ConfigSetting` anonymously somewhere app-specific before removing access.

- [ ] Each worker builds, runs that repo's tests, commits locally, and reports pre-existing failures separately from new ones.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## M5 — Finish the Argon2id port

**Model:** Opus (`sawgrass-v2`, `pmo-app`, `atcc-app`); Sonnet (`qc-sod-ordering`) · **Depends on:** M1 (D1, D2, D4, D5), M4

*Workers must complete all items below.*

- [ ] **`sawgrass-v2` (D4):** rebase local `3491a28` + `b229593` + M4's commit onto `origin/main`. Resolve the G19 conflict by replacing origin's inline-HMAC hashing in `AppUserController.Post` with `SimpleAuthPasswordHasher`. Confirm `Services/UserMigrationService.cs` also uses the hasher.
- [ ] **`qc-sod-ordering` (D5):** bring G19 (`0363f69`) onto `develop` and convert it from inline HMAC to the hasher. Leave the `main` merge to M6.
- [x] **`pmo-app` (D1):** deferred to the rebuild (decided 2026-10-01). Nothing to do here.
- [x] **`atcc-app` (D2):** excluded (decided 2026-10-01). Nothing to do; do not open the repo.
- [ ] For every repo touched: legacy HMAC verify is **retained** (removing it locks out every user); rehash-on-login present; migration written in that repo's own DbUp convention and applied to its **dev** database only.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## T3 — Security verification sweep

**Model:** Sonnet · **Mode:** unit/integration + scripted probe · **Depends on:** M4, M5

- [ ] Re-run the appendix probe at each repo's working tree: columns A, B, N, O read ✅ for every in-scope repo; C/D/E read ✅ with no ⚠️ for `sawgrass-v2` and `qc-sod-ordering`.
- [ ] `grep` every ported repo for remaining inline `HMACSHA512` **hash writes** outside `SimpleAuthPasswordHasher` (verify-only legacy paths are expected).
- [ ] `dotnet build` + `dotnet test` per repo; table of results with pre-existing failures called out (`sawgrass-v2`, `lymedeploy`, `open-mic-night` have known ones).
- [ ] For each repo that can run locally, start the API and `curl -X POST` the three admin endpoints without a token: expect 401.
- [ ] `git merge-base --is-ancestor <sha> HEAD` for every port commit on its working branch.
- [ ] Update the `core-fix-log.md` tables. Push approvals are deferred to T5; do not pause here.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## M6 — Branch reconciliation

**Model:** Sonnet · **Depends on:** M1, T3

*Workers must complete all items below. Branch deletion and any force-push need Mike's explicit confirmation per branch.*

- [x] `lymecrm`: merge `origin/main`'s G19 into `develop`; confirm `main` and `develop` then differ only by intended unreleased work. Done 2026-10-01: both at `4dd7811`, pushed.
- [ ] `ping`: same.
- [ ] `qc-sod-ordering`: merge `develop` into `main` so the Argon2id port, unlock, G19 and the CLAUDE.md tag are on both.
- [ ] `lymestats`: port G19 (it is on neither branch), then bring `main` level.
- [x] `playmusiconline`: Mike's call 2026-10-01 — leave `main` alone. No merge, no report needed. (The D1 authorization fix on `develop` in M4 still stands.)
- [x] `lymesend`, `paymentz`: stale `origin/develop` branches deleted 2026-10-01 on Mike's go-ahead (tips `82c3f43` and `b6fedd5`; both were strict ancestors of `main`, and TeamCity builds from `main`). `lymesend`'s local `develop` deleted too.
- [x] `lymecrm`: act on Mike's M1 decision for the `worktree-agent-*` branches. All 29 deleted 2026-10-01 (see Progress Log).
- [x] `lymebooks`: act on Mike's M1 decision for `accounting-remediation` and `backup/pre-rewrite`. Both local branches deleted 2026-10-01 (`origin/accounting-remediation` still exists).
- [ ] Produce a table: repo · working branch · deploy branch · commits apart.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## M7 — Remaining L1 fan-out

**Model:** Sonnet per repo; **Opus for item F's UI** in the three non-Material kits (`qc-sod-ordering` PrimeNG, LymeStarter-lineage `.ls-*`, `lymesend` Bootstrap) · **Depends on:** T3, M6

*Workers must complete all items below.*

- [ ] **F** — email duplicate validation to the 12 scaffolds other than `atcc-app` (which already has it). Probe the user form's actual UI kit before writing markup; express the feedback in that kit's idiom so it reads as native to the form (matching the adjacent username-availability hint), not pasted in.
- [ ] **G** — publish-as-Production to `lymecrm` (both `WebApi` and `NasRemoteApi` `web.config`), `lymetimer`, `lymedeploy`, `lymesend` (csproj only), `sawgrass-v2`, `paymentz`, `open-mic-night`, `qc-sod-ordering`, `pmo-app`. Check first whether LymeDeploy already overrides the environment at deploy time for each app, and say so in the summary.
- [ ] **H** — forward-slash TypeGen `outputPath` in `sawgrass-v2`, `paymentz`, `open-mic-night`; clean-rebuild (`rm -rf bin obj`) and confirm `_api/` is populated in the right place. In `open-mic-night`, `git rm` the 18 tracked backslash-named files and add the `.gitignore` rule from lymestarter `18a9108`.
- [ ] **I** — per D3: `ISimpleAuthEmailSender` and `IPostRegistrationHandler` into `sawgrass-v2`, `paymentz`, `open-mic-night`, `qc-sod-ordering` (and `pmo-app` per D1; `atcc-app` is excluded per D2).
- [ ] Add the `> **Upstream:**` tag to `lymecrm`, `qc-sod-ordering`, `playmusiconline` `CLAUDE.md` (not `md-ccrs-dev`: excluded per D2). Flag (do not create) the missing root `CLAUDE.md` in `ping` and `lymestats`.
- [ ] Each worker builds, tests and commits locally.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## M8 — LymeBooks → template intake

**Model:** Opus (189 entries of judgment: generic vs app-specific, plus shell/UI changes) · **Depends on:** T2 · **Repos:** `~/git/lymebooks` (read), `~/git/lymestarter` (write)

*Workers must complete all items below.*

- [ ] Move the 52 design-pass entries in LymeBooks `lymestack-todos.md` from Pending to Completed (ported 2026-09-25).
- [ ] Classify each remaining Pending entry: **port** (generic framework fix), **app-specific** (stays in LymeBooks — everything under `lymestack-invoicing/` and `LymeStackInvoicing/`, which the template does not have), **already upstream**, or **needs Mike**. Write the classification to `~/git/lymestarter/lymebooks-intake.md`.
- [ ] Pause for Mike's review of `~/git/lymestarter/lymebooks-intake.md` before porting anything.
- [ ] Port the approved entries to `lymestarter`, grouped into coherent commits (e.g. tenant-context incident fixes; shell loading-pill self-heal; modal-host dialog; dark-mode `color-scheme`; quick-launch Recents; ConfigSetting cache). Shell and shared-component work must hold to `docs/design-system.md` — tokens only, no fixed colours, light/dark parity.
- [ ] Entries that touch `SimpleAuthNet/`, `AuthController.cs`, or `account/` are L1: port them to `SimpleAuth4Net` first, then the template.
- [ ] Audit the untracked core drift in [1.7](#17-raw-drift-against-each-template) for `lymetimer` (LymeStackCore 3 files, shared 4, shell 4), `ping` (`account/` 26 files, lymestack-core 2 + 2 new) and `lymecrm` / `lymedeploy` (shell): for each differing file, upstream it, revert it, or record it as an intentional app divergence.
- [ ] `dotnet test` and `ng build` green in `lymestarter`; commit locally; add `core-fix-log.md` sections for each ported group.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## M9 — Design system fan-out

**Model:** Opus, one worker per repo (front-end design work) · **Depends on:** T2 · **Repos:** `lymecrm`, `lymetimer`, `lymedeploy`, `ping`, `lymestats`

Reference: `lymestarter` `f1d88b5` and `docs/design-system.md`; LymeBooks `develop` is the fullest implementation. *Workers must complete all items below.*

- [ ] Adopt `_components.scss` wholesale; **merge** (never overwrite) token additions into each repo's `_variables.scss`, `_themes.scss` and `_bootstrap-overrides.scss` where the repo carries its own overrides.
- [ ] Bring the 55 `lymestack-admin/**` files to the template versions. Where the repo has modified one (see drift counts — `lymetimer` 56), merge by hand and keep the app's behaviour.
- [ ] Apply the design system to the app's **own** pages with real craft: consistent spacing rhythm, a clear visual hierarchy with one primary action per view, hairline dividers and small-caps section labels, tabular figures in numeric columns, restrained hover/focus transitions, and full light/dark parity through `--ls-*` tokens only. The result should feel deliberately designed — closer to Linear or Notion than to stock Bootstrap — and every page should look like it belongs to the same product as LymeBooks.
- [ ] Remove banned classes listed in `docs/design-system.md`; replace Font Awesome with Bootstrap Icons where the template did.
- [ ] Copy `docs/design-system.md` and add the CLAUDE.md pointer.
- [ ] `ng build` green; commit locally.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## M10 — L2 small-fix fan-out

**Model:** Sonnet · **Depends on:** T2 (and M8 for its output)

*Workers must complete all items below.*

- [ ] **K** — LymeSend attachments to `lymebooks`, `lymetimer`, `lymedeploy`, `ping`, `lymestats`, with the template's `LymeSendEmailTransportTests`.
- [ ] **L** — AppConfig bootstrap to `lymebooks` and `lymetimer`. Both are in production with a working bootstrap, so first read how each resolves its API URL today and keep any explicit hostname cases.
- [ ] **M** — DbUp `--check` to `lymedeploy`'s own `WebApi/DbUp`.
- [ ] **P** — transport consolidation in `lymebooks` (remove `UseSmtpPickup`; settings under `LymeStackCore:Email`). Check the deployed config substitution before changing key names.
- [ ] Fan out whatever M8 ported to the template, to the LymeStarter-lineage apps that lack it.
- [ ] Each worker builds, tests and commits locally.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## T4 — L2 verification

**Model:** Sonnet · **Mode:** unit/integration + hands-on visual sign-off · **Depends on:** M9, M10

- [ ] Re-run the appendix probe: columns J–P ✅ for all LymeStarter-lineage repos.
- [ ] `dotnet test` + `ng build` per repo; results table.
- [ ] Re-run the [1.7](#17-raw-drift-against-each-template) drift measurement; every remaining differing framework file is either gone or listed as an intentional divergence.
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

- [ ] Add a `lymestats` row (lineage, DbUp layout, Angular path, UI kit, branch) to `~/git/lymestarter/downstream-inventory.md`, plus a one-line note that `md-ccrs-dev/atcc-app` exists but is deliberately not a fleet member (D2: not Mike's project, SimpleAuth v1.0, never port to it); correct the 2026-09-26 "still unported" note.
- [ ] Add `lymestats` to the member lists in `~/git/claude-shared-settings/skills/iadev-lyme/skills/port-core-fix/SKILL.md`, and add a step telling it to `git fetch` and compare against origin before probing.
- [ ] Update `security-port-plan.md`'s status header, or retire the file.
- [ ] Close out every `core-fix-log.md` table opened in M3 and M8 with final SHAs and push state.
- [ ] Update `~/.claude/claude-md-sections/products/lymestack.md` ("roughly half rolled out" is no longer true).
- [ ] Write a short "how to stay in sync" note at the top of `downstream-inventory.md`: fetch first, one working branch per repo, `lymestack-todos.md` in every LymeStarter-lineage repo.
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
| G | `<EnvironmentName>Production` in `WebApi.csproj`; `ASPNETCORE_ENVIRONMENT" value="Development"` in `web.config` |
| H | no `\\` in any `tgconfig.json`; `git ls-files` shows no backslash-named paths |
| I | `IPostRegistrationHandler.cs`, `ISimpleAuthEmailSender.cs`, `Models/Config/SimpleAuthMode.cs` exist |
| J | `ng-app/src/scss/_components.scss` exists |
| K | `Attachments` in `LymeSendEmailTransport.cs` |
| L | `document.baseURI` in `ng-app/src/main.ts` |
| M | `--check` in `WebApi/DbUp/Program.cs` |
| N | count of `AllowAnonymous` in `ConfigSettingController.cs` (template has 2) |
| O | count of `Authorize(Roles = "Admin")` in `UserFeedbackController.cs` (template 2, LymeBooks 4) |
| P | `UseSmtpPickup` absent from `LymeStackCoreOptions.cs` |

Markers N and O are counts, not proofs — M3 reads the LymeBooks diff rather than trusting them.

[Return to Top](#downstream-sync--inventory--implementation-plan)

## Progress Log / Notes

Newest first. Format: `**YYYY-MM-DD HH:MM** - entry`.

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
