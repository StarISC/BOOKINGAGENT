-- Auth schema for roles/users (SQL Server 2019 compatible)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
CREATE TABLE dbo.Users (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARBINARY(512) NOT NULL,
    PasswordSalt VARBINARY(256) NOT NULL,
    Email NVARCHAR(255) NULL,
    EmailConfirmed BIT NOT NULL DEFAULT 0,
    Phone NVARCHAR(50) NULL,
    PhoneConfirmed BIT NOT NULL DEFAULT 0,
    DisplayName NVARCHAR(200) NULL,
    FirstName NVARCHAR(100) NULL,
    LastName NVARCHAR(100) NULL,
    DateOfBirth DATE NULL,
    Locale NVARCHAR(10) NULL,
    TimeZone NVARCHAR(100) NULL,
    AddressLine1 NVARCHAR(255) NULL,
    AddressLine2 NVARCHAR(255) NULL,
    City NVARCHAR(100) NULL,
    StateProvince NVARCHAR(100) NULL,
    PostalCode NVARCHAR(20) NULL,
    CountryCode NVARCHAR(10) NULL,
    DocumentType NVARCHAR(50) NULL,
    DocumentNumber NVARCHAR(100) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    FailedAccessCount INT NOT NULL DEFAULT 0,
    LockoutEnd DATETIME2(3) NULL,
    LastLoginAt DATETIME2(3) NULL,
    PasswordChangedAt DATETIME2(3) NULL,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2(3) NULL,
    CreatedBy NVARCHAR(100) NULL,
    UpdatedBy NVARCHAR(100) NULL
);
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Roles' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
CREATE TABLE dbo.Roles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Code NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255) NULL,
    CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME()
);
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserRoles' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
CREATE TABLE dbo.UserRoles (
    UserId BIGINT NOT NULL,
    RoleId INT NOT NULL,
    AssignedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
    AssignedBy NVARCHAR(100) NULL,
    CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id)
);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID('dbo.Users'))
    CREATE INDEX IX_Users_Email ON dbo.Users(Email);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Phone' AND object_id = OBJECT_ID('dbo.Users'))
    CREATE INDEX IX_Users_Phone ON dbo.Users(Phone);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserRoles_RoleId' AND object_id = OBJECT_ID('dbo.UserRoles'))
    CREATE INDEX IX_UserRoles_RoleId ON dbo.UserRoles(RoleId);

-- Optional: permissions (leave commented until needed)
-- CREATE TABLE dbo.Permissions (
--     Id INT IDENTITY(1,1) PRIMARY KEY,
--     Code NVARCHAR(100) NOT NULL UNIQUE,
--     Description NVARCHAR(255) NULL,
--     CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME()
-- );
-- CREATE TABLE dbo.RolePermissions (
--     RoleId INT NOT NULL,
--     PermissionId INT NOT NULL,
--     GrantedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
--     GrantedBy NVARCHAR(100) NULL,
--     CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleId, PermissionId),
--     CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id),
--     CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES dbo.Permissions(Id)
-- );

-- Seed roles (adjust as needed)
IF NOT EXISTS (SELECT 1 FROM dbo.Roles)
BEGIN
    INSERT INTO dbo.Roles (Name, Code, Description) VALUES
    ('Customer', 'CUSTOMER', 'Customer-facing user'),
    ('Agent', 'AGENT', 'Agency agent'),
    ('Supervisor', 'SUPERVISOR', 'Supervisory role'),
    ('Admin', 'ADMIN', 'System administrator');
END

-- Seed default admin user (password hash/salt must be set by deployment; placeholder NULL to be updated securely)
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = 'admin')
BEGIN
    INSERT INTO dbo.Users (Username, PasswordHash, PasswordSalt, Email, DisplayName, IsActive, CreatedAt, CreatedBy)
    VALUES ('admin', 0x, 0x, 'admin@example.com', 'System Admin', 1, SYSUTCDATETIME(), 'seed');

    INSERT INTO dbo.UserRoles (UserId, RoleId)
    SELECT u.Id, r.Id
    FROM dbo.Users u
    CROSS JOIN dbo.Roles r
    WHERE u.Username = 'admin' AND r.Code = 'ADMIN';
END
