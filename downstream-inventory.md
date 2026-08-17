# Inventory & Tag Downstream Repos scaffolded from LymeStarter / SimpleAuth4Net

You are mapping and tagging (NOT yet porting code into) all local repos that were scaffolded from one of two upstream templates. This is a **discovery + documentation** pass that produces the roadmap for a later security-port phase. Work is macOS, repos live under `~/git/` (search there; note if any appear elsewhere).

## Background (why this matters)

The upstream repo `~/git/SimpleAuth4Net` just received a security-hardening changeset (git commits `d815277`, `5d5ce29`, `2a9901d` on `master`): Argon2id password hashing with rehash-on-login, per-account brute-force lockout, SSO/token integrity fixes, account-enumeration hardening, plus a SQL migration (`migrations/2026-07-security-hardening.sql`). That auth code lives in a **`WebApi/SimpleAuthNet` library + `WebApi/WebApi/Controllers/AuthController.cs`** and gets copied into apps scaffolded from these templates. Those downstream copies now need the same fixes — but first we need to know **which repos exist and where each came from.**

There are TWO upstream templates:
- **LymeStarter** — `~/git/lymestarter` (full-stack Angular + .NET template).
- **SimpleAuth4Net** — `~/git/SimpleAuth4Net` (the auth library + reference app).

Two setup skills scaffold from them. **Read both first** to learn each scaffold's fingerprint (what dirs/files it lays down, how it renames things, what appsettings look like):
- `~/git/claude-shared-settings/commands/setup-project-lymestarter.md`
- `~/git/claude-shared-settings/commands/setup-project-simpleauth.md`

## Tasks

### 1. Discover candidate repos
Enumerate git repos under `~/git/` (each dir with a `.git`). For each, decide whether it was scaffolded from LymeStarter, from SimpleAuth4Net, or neither. **Attribution heuristics** (the user's tip — use directory shape + appsettings.json, don't rely on one signal):
- Presence/shape of `WebApi/SimpleAuthNet/`, `WebApi/WebApi/`, `ng-app/` (and whether react/vue apps are present, as in the SimpleAuth reference repo).
- Structure and keys of `WebApi/WebApi/appsettings.json` (e.g. `AuthSettings`, connection-string naming, SSO/mode keys) vs the two templates' appsettings.
- Namespaces / project names / renamed assemblies (the setup skills rename things — see the skill files).
- Any existing upstream marker in the repo's `CLAUDE.md` or README.
- `git log` root/oldest commit message (scaffolds often have a characteristic first commit).
Compare each candidate against the two templates' own trees to classify. When ambiguous, say so and give your best guess + the evidence.

### 2. Determine port-readiness per repo (assessment only — do NOT edit code)
For every repo classified as scaffolded-from-either, assess:
- Does it contain the auth code that needs the security port (a `SimpleAuthNet` lib / `AuthController.cs`)? 
- How far has its `AuthController.cs` / auth library **diverged** from `~/git/SimpleAuth4Net` (rough: near-identical, lightly modified, heavily diverged)? A quick `diff` of key files against the SimpleAuth4Net copy is the fastest signal.
- Which parts of the security changeset apply, and rough effort to port (low/med/high).
This becomes the porting backlog — capture it, don't act on it.

### 3. (a) Create the inventory file in LymeStarter
Write `~/git/lymestarter/lymestarter-instances.md` containing a table/list of every downstream repo found, with columns: **repo path**, **origin** (LymeStarter | SimpleAuth4Net | ambiguous), **evidence** (what fingerprint matched), **has auth code?**, **divergence from SimpleAuth4Net**, **security-port effort** (low/med/high/n-a), **notes**. Add a short header explaining what the file is and that it's the source of truth for downstream instances.
Also add a brief pointer in `~/git/lymestarter/CLAUDE.md` (near the top, a clearly-marked note) telling developers this file exists and what it tracks. Keep it minimal.

### 4. (b) Tag each downstream repo's CLAUDE.md with its upstream source
For each repo found, add a short source/upstream note near the top of its `CLAUDE.md`, e.g.:
- LymeStarter-scaffolded: `> **Upstream:** This project was scaffolded from the LymeStarter template (~/git/lymestarter).`
- SimpleAuth-scaffolded: `> **Upstream:** This project was scaffolded from the SimpleAuth4Net repo (~/git/SimpleAuth4Net).`
If a repo has no `CLAUDE.md`, note that in the inventory (do NOT create a full CLAUDE.md — just flag it; a one-line stub is acceptable if trivial, but ask nothing and don't over-build).

## Constraints

- **Discovery + documentation only. Do NOT port the security fixes or edit any application/auth code.** The code port is a separate later phase; your job is the map + the CLAUDE.md tags + the inventory file.
- **Do NOT commit or push anything.** Leave all edits in each repo's working tree for the user to review and commit per-repo. (You'll be editing `CLAUDE.md` in multiple repos + creating one new file in lymestarter — list every file touched, grouped by repo, so the user can review.)
- **Never touch** `appsettings.*.local.json` or any secrets; don't print connection strings.
- Don't modify `react-app/`/`vue-app/` inside any repo.
- Be honest about ambiguity — a wrong origin attribution is worse than a flagged "uncertain."

## When done
- Write a concise summary: the full list of repos found grouped by origin (LymeStarter / SimpleAuth4Net / ambiguous / not-scaffolded), the port-readiness backlog (which repos need the security port + rough effort each), every file you created/edited grouped by repo (so the user can review + commit), and anything notable (repos with no CLAUDE.md, heavily-diverged ones, anything found outside `~/git/`).
- Copy that summary to the clipboard with `pbcopy`.
- Then tell me the inventory is done and the summary is on the clipboard.
