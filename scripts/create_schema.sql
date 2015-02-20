-- settings
BEGIN TRANSACTION
GO

SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
GO

COMMIT

-- drop everything
BEGIN TRANSACTION
GO

-- drop all FK constraints
DECLARE @sql nvarchar(255)
WHILE EXISTS(select * from INFORMATION_SCHEMA.TABLE_CONSTRAINTS where CONSTRAINT_TYPE = 'FOREIGN KEY')
BEGIN
    select    @sql = 'ALTER TABLE ' + TABLE_NAME + ' DROP CONSTRAINT ' + CONSTRAINT_NAME 
    from    INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    where CONSTRAINT_TYPE = 'FOREIGN KEY'
    exec    sp_executesql @sql
END
GO
-- drop all tables
EXEC sp_MSforeachtable @command1 = "DROP TABLE ?"
GO

COMMIT

-- create everything
BEGIN TRANSACTION
GO

IF SCHEMA_ID(N'Jim') IS NULL EXECUTE(N'CREATE SCHEMA [Jim]');
GO

--User is a reserved ODBC keyword
CREATE TABLE Jim.JimUser
    (
    Id uniqueidentifier PRIMARY KEY,
    -- apparently there was once a UK government document that said names could reasonably be up to 70 characters
    Name nvarchar(70) NOT NULL,
    -- 254 characters is the maximum possible length of an email address
    Email nvarchar(254) NOT NULL,
    -- needs to store the number of iterations, the salt and hash base64 encoded (plus padding), and the separator chars.
    -- so it should always be 6 + 24 + 28 + 2 = 60, assuming 6 digit number of iterations, 16 byte salt, 20 byte hash, and two separator chars
    PasswordHash char(60) NOT NULL,
    CreationTime bigint NOT NULL
    )
GO

CREATE NONCLUSTERED INDEX IX_Email ON Jim.JimUser (Email)
GO
    
COMMIT
GO

