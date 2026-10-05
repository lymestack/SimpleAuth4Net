# SimpleAuth4Net / Downstream Sync — Resume Prompt

## Project Overview

`SimpleAuth4Net` is the open-source L1 auth reference (.NET 8 API with the `SimpleAuthNet` library, Angular 19 client). Its code is vendored as source into the `lymestarter` template (L2) and 13 live scaffolds under `~/git/`. This repo also holds `downstream-sync-plan.md`, the plan that brings all of those back in sync, and the orchestrator session that runs it (`.orchestrator/downstream-sync-plan/`, gitignored). Upstream before downstream, semantic merges, nothing deployed without Mike driving it.

## Current Status

**The downstream sync plan is complete (2026-10-04). M11, M12 and T5 are closed.**

- Seven apps are on Production through LymeDeploy with UAT equal to Prod: `lymecrm` 2.0.0.23, `lymetimer` 3.0.0.64, `ping` 2.0.0.31, `lymesend` 2.0.0.7, `lymestats` 1.0.0.18, `paymentz` 1.0.0.57, `lymebooks` 1.0.0.184.
- Mike approved the T5 visual sign-off with no feedback. The audit (`sync-audit-report.md`) and smoke test (`smoke-test-report.md`) are committed.
- Two Production deploys are handed off to per-repo backlogs, not open plan items.
- Untracked `~/git/lymestarter/lymebooks-intake.md` belongs to another session; leave it.

## What's Done

- **M0 to M12 and T1 to T5**: authorization fix, logger, email check, Argon2id, design system fan-out, template catch-up, all pushed 2026-10-01. See the Progress Log.
- **2026-10-04:** audit, Register audit-log fix in `lymecrm` (`ce81684`) and `lymetimer` (`e2e5450`), `lymebooks` email variables renamed (release 184 verified in UAT and Prod), smoke test and its two fixes, M11 Production deploys, `lymecrm` spot check.

## What's Next

1. **Round two** (Mike reviews first): `core-drift-audit.md` (U1 to U14), `~/git/lymestarter/lymebooks-intake.md` (8 port groups), then fan out.
2. **`sawgrass-v2` Production:** Azure firewall rule and the `2026-08-26 - Security hardening.sql` migration; waiting on Brian (vacation). See `~/git/sawgrass-v2/RESUME.md`.
3. **`qc-sod-ordering` Production:** Ordering `0.1.0.32` is UAT only; move Ordering into its own Octopus project first. See `~/git/qc-sod-ordering/BACKLOG.md`.
4. **Backlog** (also in the 2026-10-04 close-out log entry):
   - `onEmailFieldChanged(string)` breaks under nullable TypeGen output in `lymesend`, `open-mic-night`, `qc-sod-ordering`, `paymentz`.
   - `sawgrass-v2`: dormant tgconfig with `clearOutputDirectory:true`; `DbUp/Program.cs` hardcodes a SQL password.
   - `lymedeploy` `Tools dbup --check` still runs migrations.
   - `lymecrm` and `lymetimer`: post-registration handler runs before the admin grant.
   - Stale .NET tests: `lymebooks` (15), `lymedeploy` (9); `lymestats` integration failures (6); `open-mic-night` bundle budget; `paymentz` macOS data-dir default.
   - Optional design calls: lymetimer NG0100, `.link-primary` underline, light-mode link contrast 3.68:1, PinG brand pages in dark mode.

## Planning Docs

- `downstream-sync-plan.md`: inventory, milestones, decisions D1 to D11, Progress Log (source of truth; only the orchestrator edits it).
- `t5-checklist.md`: the T5 list (now committed). All sections now ticked.
- `core-drift-audit.md`: per-file core drift verdicts in four apps (committed, awaiting review).
- `~/git/lymestarter/lymebooks-intake.md`: 189 LymeBooks upstream entries classified (untracked, awaiting review).

## Servers

- API: `cd WebApi/WebApi && dotnet watch run` (dev config points at the shared dev `SimpleAuth` database).
- Client: `cd ng-app && npm start`, `http://localhost:4200`; user admin at `/auth-admin/users`.
- Accounts: ask Mike for the current Admin password; older notes are stale and lock the account after 3 tries.

## Key File Paths

```text
downstream-sync-plan.md                       plan and Progress Log
t5-checklist.md                               open T5 items, push table, M11 order
core-drift-audit.md                           drift verdicts, awaiting review
.orchestrator/downstream-sync-plan/
  state.json                                  session state (gate: T5)
  t5-review/index.html                        screenshot review page, 12 open items
  processed/                                  all worker summaries
WebApi/WebApi/Controllers/AuthController.cs   per-action auth attributes, EmailExists
WebApi/WebApi.Tests/                          28 tests
~/git/lymestarter/core-fix-log.md             port ledger for every fix
~/git/lymestarter/downstream-inventory.md     per-repo quirks and lineage
```

## Recent Git Log

```text
313dadc Add the downstream sync smoke test report
983af34 Fix the RegisterConfirmationComponent spec and accept a null email on the user form
8746812 Add the downstream sync audit report
296e056 Log the 2026-10-01 push and the 2026-10-04 audit, and fix the stale tracker ticks
c2f10db Update the resume prompt after the push
9a84bdb Add the core drift audit and the T5 checklist
47c8e94 Record the lymestats verification and the review page
e7a2712 Record the visual sign-off runs, the three template fixes and their fan-out
```

`master` is pushed to `origin/master` (close-out commit on top of this list).

## Any Other Notes

- `dotnet` on this Mac: `export DOTNET_ROOT=/usr/local/share/dotnet PATH="/usr/local/share/dotnet:$PATH"` (the brew one is x86_64 and breaks TypeGen).
- Tests: `cd WebApi && dotnet test` (28 pass).
- Commit hook rejects any commit command containing an assistant or vendor name, a co-author trailer, or the instructions file's name. Commit the plan with the pathspec form (`git commit downstream-sync-plan.md -m "…"`).
- Another orchestrator (`lymetools-port-plan`, run from `~/git/lymetools`) shares `~/git/lymestarter`; its uncommitted edits live there.
- Dev data gaps seen in sign-off: lymetimer and lymecrm `/Dev/AppHealth` return 500 (missing `usp_TableStats`/`usp_GetIndexStats` in lymetimer dev; data directory issue in lymecrm). Old drift, not from this run.
- For tracker or ledger updates across repos, use Sonnet, not Haiku, and verify the diff.
