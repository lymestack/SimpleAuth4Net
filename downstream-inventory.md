The live downstream inventory is `~/git/lymestarter/downstream-inventory.md`.

## Pending downstream ports

Fixes made in SimpleAuth4Net that apps seeded from it (via `/iadev:setup-project-simpleauth`, which clones the
repo's `master` at setup time) still need. LymeStarter apps are tracked separately in `~/git/lymestarter/core-fix-log.md`.
Seeded apps are the rows with Origin `SimpleAuth4Net` in the live inventory above; `lymesend` is a pre-SSO hybrid that
also carries SimpleAuth-style `core/_services/auth.service.ts` and is included here.

### NF2 (L1): logout revokes the refresh token

- **Source:** SimpleAuth4Net `9301a45` (origin: LymeStarter `bf9414b`, `core-fix-log.md` 2026-10-10, NF2). L1 half only.
- **Server:** in `WebApi/WebApi/Controllers/AuthController.cs`, `Logout()` deletes the `AppRefreshToken` row whose hashed
  `Token` matches the `X-Refresh-Token` cookie (via `HashToken`), then clears the cookies. Drop `await Task.CompletedTask;`.
- **Client:** remove `revokeToken()` from `ng-app/src/app/core/_services/auth.service.ts`. It called a nonexistent
  `Auth/RevokeToken` and has no callers. Logout already calls `Auth/Logout` with credentials, so no other client change.
- **Test:** copy `WebApi/WebApi.Tests/AuthLogoutTests.cs` where the app has the `AuthApiFactory` test project.
- **Check before porting:** any app code that runs on logout and needs the refresh-token owner must resolve it before
  `Auth/Logout` runs, because the row is gone afterwards (LymeStarter's `SupportSessionAuthEventFilter` was one). Checked
  2026-10-10: none of the apps below has an action filter or other logout hook outside `SimpleAuthNet`.
- **Divergence:** the `Logout()` body is byte-identical to SimpleAuth4Net's pre-fix version in all six apps, so the server
  patch applies cleanly. `revokeToken()` is present and uncalled in all six. No hand merge needed.
- **Skill:** `setup-project-simpleauth` clones live `master` and copies `WebApi`/`ng-app`, with no pinned copy of either
  file, so apps seeded after this lands get the fix; the skill needs no edit.

| Repo | Client | Logout() vs old | `revokeToken()` | Hand merge | Ported |
|---|---|---|---|---|---|
| `sawgrass-v2` | ng-app | identical | present, no callers | no | no |
| `paymentz` | ng-app | identical | present, no callers | no | no |
| `open-mic-night` | ng-app | identical | present, no callers | no | no |
| `qc-sod-ordering` | ng-app | identical | present, no callers | no | no |
| `lymesend` (hybrid) | ng-app | identical | present, no callers | no | no |
| `playmusiconline/pmo-app` | ng-app | identical | present, no callers | no | no (app deferred to its rebuild) |
