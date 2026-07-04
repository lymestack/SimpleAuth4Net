-------------------------------------------------------------------------------
-- Migration: 2026-07 Security Hardening
--
-- Brings an EXISTING SimpleAuth database up to the schema introduced by the
-- security-hardening changeset (Argon2id migration, refresh-token reuse
-- detection, per-account verification-code lockout).
--
-- New databases created from CreateDb.sql already include these changes; this
-- script exists to upgrade databases provisioned before the changeset.
--
-- Idempotent: every statement is guarded, so it is safe to run more than once
-- and safe to run against a database that already has some of the changes.
--
-- Run via pymssql on macOS (SQLEXPRESS named instances don't resolve through
-- the Go sqlcmd) — see CLAUDE.md "Connecting to the Database".
-------------------------------------------------------------------------------

-------------------------------------------------------
-- 1. Refresh-token rotation reuse detection.
--    Stores the hash of the immediately-preceding (consumed) refresh token for
--    each device so a replay of a rotated token can be detected.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AppRefreshToken') AND name = N'PreviousToken')
BEGIN
    ALTER TABLE dbo.AppRefreshToken ADD PreviousToken VARCHAR(100) NULL;
END
GO

-------------------------------------------------------
-- 2. Per-account failed-attempt counter for short verification codes
--    (email/SMS MFA, password-reset) and TOTP codes. Enables per-account
--    lockout/invalidation on the code-verification paths.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AppUserCredential') AND name = N'FailedVerificationAttempts')
BEGIN
    ALTER TABLE dbo.AppUserCredential ADD FailedVerificationAttempts int NOT NULL DEFAULT 0;
END
GO

-------------------------------------------------------
-- 3. Widen PasswordHash for Argon2id (H1 remediation).
--    Argon2id credentials are stored as a self-describing PHC-style string
--    ($argon2id$v=19$m=..,t=..,p=..$<salt>$<hash>) encoded as UTF-8 bytes, which
--    is longer than the 64-byte legacy HMAC output. Existing legacy rows are
--    upgraded transparently on next successful login (rehash-on-login); no
--    forced password reset.
--
--    Guard: only widen when the column is narrower than 256 bytes and is not
--    already VARBINARY(MAX) (max_length = -1).
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.AppUserCredential') AND name = N'PasswordHash'
      AND max_length <> -1 AND max_length < 256)
BEGIN
    ALTER TABLE dbo.AppUserCredential ALTER COLUMN PasswordHash VARBINARY(256) NULL;
END
GO
