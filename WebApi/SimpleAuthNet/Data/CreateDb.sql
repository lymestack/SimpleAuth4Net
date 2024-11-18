USE [AuthSample]
GO
/****** Object:  Table [dbo].[AppRefreshToken]    Script Date: 11/14/2024 12:04:46 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppRefreshToken](
	[AppUserId] [int] NOT NULL,
	[Token] [varchar](100) NOT NULL,
	[Created] [datetime] NOT NULL,
	[Expires] [datetime] NOT NULL,
 CONSTRAINT [PK_AppRefreshToken] PRIMARY KEY CLUSTERED 
(
	[AppUserId] ASC,
	[Token] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppRole]    Script Date: 11/14/2024 12:04:46 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
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
/****** Object:  Table [dbo].[AppUser]    Script Date: 11/14/2024 12:04:46 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
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
/****** Object:  Table [dbo].[AppUserCredential]    Script Date: 11/14/2024 12:04:46 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
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
/****** Object:  Table [dbo].[AppUserRole]    Script Date: 11/14/2024 12:04:46 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
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
ALTER TABLE [dbo].[AppRefreshToken]  WITH CHECK ADD  CONSTRAINT [FK_AppRefreshToken_AppUser] FOREIGN KEY([AppUserId])
REFERENCES [dbo].[AppUser] ([Id])
GO
ALTER TABLE [dbo].[AppRefreshToken] CHECK CONSTRAINT [FK_AppRefreshToken_AppUser]
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

CREATE TRIGGER trg_DeleteExpiredTokens
ON [dbo].[AppRefreshToken]
AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON

    -- Delete expired tokens
    DELETE FROM [dbo].[AppRefreshToken]
    WHERE [Expires] < GETDATE()
END
GO