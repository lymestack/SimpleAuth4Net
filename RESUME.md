# SimpleAuth4Net / Downstream Sync — Resume Prompt

## Project Overview

`SimpleAuth4Net` is the open-source L1 auth reference (.NET 8 API with the `SimpleAuthNet` library, Angular 19 client). Its code is vendored as source into the `lymestarter` template (L2) and 13 live scaffolds under `~/git/`. This repo also holds `downstream-sync-plan.md`, the plan that brings all of those back in sync, and the orchestrator session that runs it (`.orchestrator/downstream-sync-plan/`, gitignored). Upstream before downstream, semantic merges, nothing deployed without Mike driving it.

## Current Status

**Every repo in the sync is pushed (2026-10-01, on Mike's "push everything"). Nothing is deployed. T5 review items and the lymecrm production check are still open.**

- Pushed: all 14 repos in the `t5-checklist.md` section 6 table, plus `lymestats` `main` (now tracks `origin/main`) and `qc-sod-ordering` `main` (`0363f69..4e29380`; TeamCity/Octopus may pick it up, check).
- Not touched: `md-ccrs-dev` (its 29 local commits stay local).
- The plan's Progress Log does **not** yet record the push. M12's final pass should.
- All worker agents stopped, ports free, every tree clean except the lymetools session's untracked `~/git/lymestarter/lymebooks-intake.md`.

## What's Done

- **M2 to M10, T1 to T4:** authorization fix, logger event type, publish as Production, email duplicate check, tests, template catch-up, G19, design system ports, attachments/bootstrap/DbUp `--check`. See the Progress Log.
- **T5 hands-on (2026-10-01 evening):** visual sign-off runs in every LymeStarter app; three shared template fixes (dark mode, logger toast messages, dark-mode links) fanned out to all six LymeStarter apps; lymestats Live-page delay and search-page contrast fixed; ledger (`~/git/lymestarter/core-fix-log.md`) updated with verified SHAs.
- **Review page built:** `.orchestrator/downstream-sync-plan/t5-review/index.html` (745 images, 12 open items with tick boxes and "Copy my answers").

## What's Next

1. **Mike's review page answers:** he works through the 12 items and pastes "Copy my answers". Any fix becomes a gap-fill worker, then a new push.
2. **lymecrm production spot check** (checklist section 5): Mike signs in to Production `2.0.0.22`; confirm which environment UAT and Production run as.
3. **M12 final pass:** record the push state in the Progress Log and `core-fix-log.md`, D6 to D9 as backlog items, close the plan.
4. **M11 (production), Mike drives, in checklist section 7 order:** authorization fix first (`lymesend`, `lymetimer`, `paymentz`); **`lymebooks` must not deploy** until LymeDeploy's email variables move from `EmailSettings:*` to `LymeStackCore:Email:*`; `sawgrass-v2` needs the Azure firewall rule and its security migration first.
5. **Optional small fixes Mike hasn't ruled on:** lymetimer NG0100 (defer the emit from commit `b84a4e0`); `.link-primary` underline still Bootstrap blue (two undefined RGB tokens); light-mode link contrast 3.68:1 fleet-wide (visible design change); PinG brand pages forced to light mode.
6. **Round two** (after Mike reviews `core-drift-audit.md` and `lymebooks-intake.md`): 20 LymeBooks fixes up to the template, audit items U1 to U14, fan out.

To resume orchestration, run `/iadev:orchestrator downstream-sync-plan.md` **from `~/git/SimpleAuth4Net`** and choose resume.

## Planning Docs

- `downstream-sync-plan.md`: inventory, milestones, decisions D1 to D11, Progress Log (source of truth; only the orchestrator edits it).
- `t5-checklist.md`: the T5 list (now committed). Sections 5, 7 and the review page items remain.
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
9a84bdb Add the core drift audit and the T5 checklist
47c8e94 Record the lymestats verification and the review page
e7a2712 Record the visual sign-off runs, the three template fixes and their fan-out
54f3be2 Record the lymebooks rebase onto origin
7d273eb Record the template hands-on pass and the visual sign-off dispatch
12164e1 Record the T5 section 1 decisions in the downstream sync plan
e2a1f66 Raise the production bundle error budget to 1.5 MB
```

`master` is level with `origin/master` (pushed 2026-10-01).

## Any Other Notes

- `dotnet` on this Mac: `export DOTNET_ROOT=/usr/local/share/dotnet PATH="/usr/local/share/dotnet:$PATH"` (the brew one is x86_64 and breaks TypeGen).
- Tests: `cd WebApi && dotnet test` (28 pass).
- Commit hook rejects any commit command containing an assistant or vendor name, a co-author trailer, or the instructions file's name. Commit the plan with the pathspec form (`git commit downstream-sync-plan.md -m "…"`).
- Another orchestrator (`lymetools-port-plan`, run from `~/git/lymetools`) shares `~/git/lymestarter`; its uncommitted edits live there.
- Dev data gaps seen in sign-off: lymetimer and lymecrm `/Dev/AppHealth` return 500 (missing `usp_TableStats`/`usp_GetIndexStats` in lymetimer dev; data directory issue in lymecrm). Old drift, not from this run.
- For tracker or ledger updates across repos, use Sonnet, not Haiku, and verify the diff.
