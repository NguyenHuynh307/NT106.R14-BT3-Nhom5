USE [master];
GO

IF DB_ID(N'healthInforDB') IS NULL
BEGIN
    CREATE DATABASE [healthInforDB];
END
GO

USE [healthInforDB];
GO

IF OBJECT_ID(N'dbo.NguoiDung', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[NguoiDung] (
        [UserId]       INT            IDENTITY(1,1) NOT NULL,
        [Username]     NVARCHAR(50)   NOT NULL,
        [PasswordHash] NVARCHAR(64)   NOT NULL,
        [PasswordSalt] NVARCHAR(32)   NOT NULL,
        [Email]        NVARCHAR(100)  NULL,
        [FullName]     NVARCHAR(100)  NOT NULL,
        [Birthday]     DATE           NULL,
        CONSTRAINT [PK_NguoiDung] PRIMARY KEY CLUSTERED ([UserId] ASC),
        CONSTRAINT [UQ_NguoiDung_Username] UNIQUE NONCLUSTERED ([Username] ASC)
    );
END
GO

SELECT
    [UserId],
    [Username],
    [PasswordHash],
    [PasswordSalt],
    [Email],
    [FullName],
    [Birthday]
FROM [dbo].[NguoiDung]
ORDER BY [UserId] ASC;
GO