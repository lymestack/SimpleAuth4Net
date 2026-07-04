
CREATE TABLE [dbo].[AppRole](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](50) NOT NULL,
	[Description] [varchar](max) NULL,
 CONSTRAINT [PK_AppRole] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE TABLE [dbo].[AppUser](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Username] [varchar](100) NOT NULL,
	[EmailAddress] [varchar](100) NOT NULL,
	[FirstName] [varchar](50) NOT NULL,
	[LastName] [varchar](50) NOT NULL,
	[DateEntered] [datetime] NOT NULL,
	[LastSeen] [datetime] NULL,
 CONSTRAINT [PK_AppUser] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[AppUserCredential](
	[AppUserId] [int] NOT NULL,
	[PasswordSalt] [varbinary](128) NOT NULL,
	[PasswordHash] [varbinary](128) NOT NULL,
	[DateCreated] [smalldatetime] NOT NULL,
 CONSTRAINT [PK_AppUserCredential] PRIMARY KEY CLUSTERED 
(
	[AppUserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


CREATE TABLE [dbo].[AppUserRole](
	[AppUserId] [int] NOT NULL,
	[AppRoleId] [int] NOT NULL,
 CONSTRAINT [PK_AppUser_AppRole] PRIMARY KEY CLUSTERED 
(
	[AppUserId] ASC,
	[AppRoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AppUserCredential]  WITH CHECK ADD  CONSTRAINT [FK_AppUserCredential_AppUser] FOREIGN KEY([AppUserId])
REFERENCES [dbo].[AppUser] ([Id])
GO
ALTER TABLE [dbo].[AppUserCredential] CHECK CONSTRAINT [FK_AppUserCredential_AppUser]
GO
ALTER TABLE [dbo].[AppUserRole]  WITH CHECK ADD  CONSTRAINT [FK_AppUser_AppRole_AppRole] FOREIGN KEY([AppRoleId])
REFERENCES [dbo].[AppRole] ([Id])
GO
ALTER TABLE [dbo].[AppUserRole] CHECK CONSTRAINT [FK_AppUser_AppRole_AppRole]
GO
ALTER TABLE [dbo].[AppUserRole]  WITH CHECK ADD  CONSTRAINT [FK_AppUser_AppRole_AppUser] FOREIGN KEY([AppUserId])
REFERENCES [dbo].[AppUser] ([Id])
GO
ALTER TABLE [dbo].[AppUserRole] CHECK CONSTRAINT [FK_AppUser_AppRole_AppUser]
GO

ALTER TABLE dbo.AppUser ADD
	Verified bit NOT NULL CONSTRAINT DF_AppUser_Verified DEFAULT 0
GO

CREATE TABLE [dbo].[AppRefreshToken](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[AppUserId] [int] NOT NULL,
	[Token] [varchar](100) NOT NULL,
	[Created] [datetime] NOT NULL,
	[Expires] [datetime] NOT NULL,
	[DeviceId] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_AppRefreshToken_1] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [IX_AppRefreshToken] UNIQUE NONCLUSTERED 
(
	[AppUserId] ASC,
	[DeviceId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE dbo.AppRefreshToken ADD CONSTRAINT
	FK_AppRefreshToken_AppUser FOREIGN KEY
	(
	AppUserId
	) REFERENCES dbo.AppUser
	(
	Id
	) ON UPDATE  NO ACTION 
	 ON DELETE  NO ACTION 
	
GO

ALTER TABLE AppUserCredential
ADD VerifyToken NVARCHAR(50) NULL,
    VerifyTokenExpires DATETIME NULL,
    VerifyTokenUsed BIT DEFAULT 0

INSERT INTO AppRole (Name, Description) VALUES ('Admin', 'App Administrator Role')
GO

ALTER TABLE AppUser ADD Active BIT DEFAULT 1
GO

-------------------------------------------------------

ALTER TABLE AppUserCredential ADD PendingMfaLogin bit NOT NULL DEFAULT 0
GO

ALTER TABLE AppUser ADD Locked bit NOT NULL DEFAULT 0
GO

ALTER TABLE AppUserCredential ADD FailedLoginAttempts int NOT NULL DEFAULT 0
GO

ALTER TABLE AppUserCredential ADD LastFailedLoginAttempt DATETIME NULL
GO

ALTER TABLE AppUserCredential ADD LockoutEndTime DATETIME NULL
GO

-------------------------------------------------------

ALTER TABLE AppUser ADD PhoneNumber VARCHAR(50) NULL
GO

ALTER TABLE AppUser ADD PhoneNumberVerified BIT NULL
GO

ALTER TABLE AppUser ADD PreferredMfaMethod INT NULL
GO

-------------------------------------------------------

ALTER TABLE AppUserCredential ADD TotpSecret NVARCHAR(255) NULL
GO

-------------------------------------------------------

ALTER TABLE AppUserCredential ADD VerificationCooldownExpires DATETIME NULL
GO

-------------------------------------------------------

CREATE TABLE AppUserPasswordHistory (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AppUserId INT NOT NULL,
    HashedPassword VARBINARY(MAX) NOT NULL,
    DateCreated DATETIME NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (AppUserId) REFERENCES AppUser(Id)
)
GO

ALTER TABLE AppUserPasswordHistory ADD Salt VARBINARY(128) NOT NULL
GO

-------------------------------------------------------

ALTER TABLE AppUserCredential ALTER COLUMN PasswordSalt VARBINARY(128) NULL
ALTER TABLE AppUserCredential ALTER COLUMN PasswordHash VARBINARY(128) NULL

-------------------------------------------------------

-- Add unique index on Username column (required field)
CREATE UNIQUE NONCLUSTERED INDEX IX_AppUser_Username ON dbo.AppUser
    (Username) 
    WITH(STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) 
    ON [PRIMARY]

-- Make EmailAddress nullable to support optional emails
ALTER TABLE AppUser ALTER COLUMN EmailAddress VARCHAR(100) NULL

-- Clean up empty string emails (convert to NULL)
UPDATE dbo.AppUser
    SET EmailAddress = NULL
    WHERE EmailAddress = '';
GO

-------------------------------------------------------

-- Refresh-token rotation reuse detection: stores the hash of the immediately-preceding
-- (consumed) refresh token for each device so a replay of a rotated token can be detected.
ALTER TABLE dbo.AppRefreshToken ADD PreviousToken VARCHAR(100) NULL
GO

-- Add filtered unique index on EmailAddress (only for non-null values)
CREATE UNIQUE NONCLUSTERED INDEX IX_AppUser_Email ON dbo.AppUser
    (EmailAddress)
    WHERE EmailAddress IS NOT NULL
    WITH(STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
    ON [PRIMARY]
GO

-------------------------------------------------------

-- Per-account failed-attempt counter for short verification codes (email/SMS MFA and
-- password-reset codes) and TOTP codes. Enables per-account lockout/invalidation on the
-- code-verification paths (previously only per-IP rate limiting protected these).
ALTER TABLE AppUserCredential ADD FailedVerificationAttempts int NOT NULL DEFAULT 0
GO

-------------------------------------------------------

-- H1 remediation: passwords migrated from single-round HMAC-SHA512 to Argon2id.
-- Argon2id credentials are stored as a self-describing PHC-style string
-- ($argon2id$v=19$m=..,t=..,p=..$<salt>$<hash>) encoded as UTF-8 bytes, which is longer than the
-- 64-byte legacy HMAC output. Widen PasswordHash to hold the encoded value. Existing legacy rows
-- are upgraded transparently on next successful login (rehash-on-login); no forced password reset.
ALTER TABLE AppUserCredential ALTER COLUMN PasswordHash VARBINARY(256) NULL
GO
