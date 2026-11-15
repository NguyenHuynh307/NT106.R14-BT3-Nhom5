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

-- Bản ghi mẫu 1: 'thu_nghiem' (Kế thừa từ BT2.2, mật khẩu gốc giả định "123456789" băm PBKDF2 600.000 vòng)
IF NOT EXISTS (SELECT 1 FROM [dbo].[NguoiDung] WHERE [Username] = N'thu_nghiem')
BEGIN
    INSERT INTO [dbo].[NguoiDung] (
        [Username],
        [PasswordHash],
        [PasswordSalt],
        [Email],
        [FullName],
        [Birthday]
    )
END
GO

-- Bản ghi mẫu 2: 'user_o''connor' (Kiểm chứng Username có dấu nháy đơn - Test Case #22
-- và cùng mật khẩu gốc "123456789" nhưng ra chuỗi PasswordHash khác hoàn toàn nhờ Salt riêng - Test Case #20)
IF NOT EXISTS (SELECT 1 FROM [dbo].[NguoiDung] WHERE [Username] = N'user_o''connor')
BEGIN
    INSERT INTO [dbo].[NguoiDung] (
        [Username],
        [PasswordHash],
        [PasswordSalt],
        [Email],
        [FullName],
        [Birthday]
    )
END
GO

-- Truy vấn kiểm tra toàn bộ 7 cột bảng NguoiDung
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

SELECT @@SERVERNAME
