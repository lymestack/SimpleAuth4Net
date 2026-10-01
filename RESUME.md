# SimpleAuth4Net / Downstream Sync — Resume Prompt

## Project Overview

`SimpleAuth4Net` is the open-source L1 auth reference (.NET 8 API with the `SimpleAuthNet` library, Angular 19 client). Its code is vendored as source into the `lymestarter` template (L2) and 13 live scaffolds under `~/git/`. This repo also holds `downstream-sync-plan.md`, the plan that brings all of those back in sync, and the orchestrator session that runs it (`.orchestrator/downstream-sync-plan/`, gitignored). Upstream before downstream, semantic merges, local commits only, nothing pushed without Mike's say-so.

## Current Status

**The orchestrated run is parked at the T5 gate: all automated work is done and verified, everything is local and unpushed, nothing is deployed, and the next move is Mike's.**

- Just finished: M2 through M10 and T1 through T4, including two gap-fills (the tracker corrections, and three template fixes that had no fan-out milestone).
- Waiting on Mike: the T5 checklist (decisions, hands-on checks, push approvals) and two reviews that gate round two.
- Held: M7 for `pmo-app` (scope question), the M8 ports (intake review), M12's final pass (push state), M11 (production).
- All worker agents are stopped. No worker entered `~/git/md-ccrs-dev`.

## What's Done

- **M2 / T1:** authorization fix in `AuthController` (no class-level `[AllowAnonymous]`), logger event type, publish as Production, email duplicate check (`Auth/EmailExists`, Admin only, error code `EMAIL_EXISTS`), xUnit project with 28 tests, always-registered `IAuthLogger`.
- **M3 / T2:** template caught up (`lymestarter` `main`): `ConfigSetting` and `UserFeedback` reads Admin-only, `AppConfig` secret filter, email check, Register handler, Secure cookies, logger fix, 161 tests, both Angular builds green.
- **M4:** authorization fix in 7 repos; config/feedback lockdown in 5 repos.
- **M5:** `sawgrass-v2` rebased onto origin with G19 on the Argon2id hasher; `qc-sod-ordering` G19 on `develop`.
- **T3 / T4:** nothing failed; tokenless calls to the admin endpoints return 401 in all 14 repos.
- **M6:** G19 on `develop` in `ping` and `lymestats`; local `main` level with `develop` in `qc-sod-ordering` and `lymestats`.
- **M7:** email check, publish as Production, TypeGen path, older L1 features in every listed repo except `pmo-app`.
- **M9:** design system in `lymecrm`, `lymetimer`, `lymedeploy`, `ping` (framework parts only), `lymestats`.
- **M10:** attachments, bootstrap, DbUp `--check`, transport consolidation.
- **M8 analysis:** `~/git/lymestarter/lymebooks-intake.md` and `core-drift-audit.md` written, uncommitted.
- **M12 (pre-push parts):** inventory, `core-fix-log.md`, security port plan, `port-core-fix` skill, product notes, stale lines in four repos' project instructions.

## What's Next

1. **Walk Mike through `t5-checklist.md`** from section 1: 11 decisions (each has a recommendation), the template hands-on pass, visual sign-off for five repos, the `lymecrm` production spot check, then a yes or no per repo in the push table.
2. **Before specific pushes:** rebase `lymebooks` onto origin (7 ahead, 29 behind; trial merge is clean); coordinate the `lymestarter` push with the lymetools session (its commits and uncommitted edits share that tree); decide whether `core-drift-audit.md` and `t5-checklist.md` get committed here.
3. **If a hands-on check fails:** dispatch a gap-fill worker and re-present only that check.
4. **After T5:** M12 final pass (push state and the T4 gap-fill SHAs into `~/git/lymestarter/core-fix-log.md`; D6 to D9 as backlog items; close the plan), then M11 with Mike driving every production step. `lymebooks` must not deploy until LymeDeploy's email variables move from `EmailSettings:*` to `LymeStackCore:Email:*`.
5. **Round two (after Mike reviews the intake and the audit):** 20 LymeBooks fixes in 8 groups up to the template (two groups via this repo first), the approved audit items U1 to U14, then fan out and verify. Smaller than today.

To resume orchestration, run `/iadev:orchestrator downstream-sync-plan.md` **from `~/git/SimpleAuth4Net`** and choose resume. Read "Orchestrator run policy and handoff state" and the Progress Log first.

## Planning Docs

- `downstream-sync-plan.md`: inventory, milestones, decisions D1 to D11, Progress Log (source of truth; only the orchestrator edits it).
- `t5-checklist.md`: the single T5 list (uncommitted).
- `core-drift-audit.md`: per-file verdicts on core drift in four apps (uncommitted, awaiting review).
- `~/git/lymestarter/lymebooks-intake.md`: classification of 189 LymeBooks upstream entries (uncommitted, awaiting review).

## Servers

- API: `cd WebApi/WebApi && dotnet watch run` (dev config points at the shared dev `SimpleAuth` database).
- Client: `cd ng-app && npm start`, `http://localhost:4200`; user admin at `/auth-admin/users`.
- Accounts: ask Mike for the current Admin password; older notes are stale and lock the account after 3 tries.

## Key File Paths

```text
downstream-sync-plan.md                       plan and Progress Log
t5-checklist.md                               what Mike decides and checks next
core-drift-audit.md                           drift verdicts, awaiting review
.orchestrator/downstream-sync-plan/
  state.json                                  session state (gate: T5)
  prompts/                                    every worker prompt; _standing-constraints.md
  prompts/ledger-data-2026-10-01.md           consolidated commit ledger for the trackers
  processed/                                  all worker summaries (review click paths are in the m9 ones)
WebApi/WebApi/Controllers/AuthController.cs   per-action auth attributes, EmailExists
WebApi/WebApi/Controllers/AppUserController.cs  duplicate-email check
WebApi/SimpleAuthNet/                         the L1 library
WebApi/WebApi.Tests/                          28 tests (AuthApiFactory, attribute, endpoint, email)
ng-app/src/app/auth-admin/users/user-form/    email availability feedback
~/git/lymestarter/core-fix-log.md             port ledger for every fix
~/git/lymestarter/downstream-inventory.md     per-repo quirks and lineage
```

## Recent Git Log

```text
6b01c1e Close T4 and mark the plan ready for the T5 gate
f3eedf5 Record further gap fill results and the T5 checklist in the downstream sync plan
c5759f1 Record the corrected trackers and the first gap fill results in the downstream sync plan
3ff1542 Always register an IAuthLogger so Auth endpoints work with audit logging off
47a6fb7 Record the L2 verification results and its gap fill in the downstream sync plan
6322435 Record the tracker update gap fill in the downstream sync plan
040c4e0 Record the tracker update dispatch in the downstream sync plan
e5e706c Close M9 and record the L2 verification dispatch in the downstream sync plan
```

`master` is 49 commits ahead of `origin/master`, deliberately unpushed (the admin unlock `ea622c8` must go out together with the authorization fix).

## Any Other Notes

- `dotnet` on this Mac: `export DOTNET_ROOT=/usr/local/share/dotnet PATH="/usr/local/share/dotnet:$PATH"` (the brew one is x86_64 and breaks TypeGen).
- Tests: `cd WebApi && dotnet test` (28 pass). `npm run build` fails on the bundle budget only (1.42 MB against 1.00 MB, pre-existing; T5 decision 7); the development build passes.
- Commit hook: any command that commits and contains an assistant or vendor name, a co-author trailer, or the instructions file's name is rejected. Commit the plan with the pathspec form (`git commit downstream-sync-plan.md -m "…"`).
- Another orchestrator (`lymetools-port-plan`, run from `~/git/lymetools`) shares `~/git/lymestarter`. This session is registered in `~/git/lymetools/.orchestrator/active-sessions.json` as well as here.
- `~/git/lymestarter` has a leftover detached worktree in the session scratchpad (`lymestarter-sync`); its commits are all on `main`, so it can be removed with `git -C ~/git/lymestarter worktree prune` once the scratchpad is gone.
- For tracker or ledger updates across repos, use Sonnet, not Haiku, and verify the diff (the Haiku pass on 2026-10-01 had to be redone).
