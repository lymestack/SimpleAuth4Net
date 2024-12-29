
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

