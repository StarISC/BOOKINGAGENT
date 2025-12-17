IF DB_ID(N'BookingAgentDB') IS NULL
BEGIN
    CREATE DATABASE [BookingAgentDB];
END
GO

USE [BookingAgentDB];
GO

IF OBJECT_ID(N'dbo.Ships', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ships
    (
        ShipId INT IDENTITY(1,1) CONSTRAINT PK_Ships PRIMARY KEY,
        BrandCode NVARCHAR(10) NOT NULL,
        ShipCode NVARCHAR(10) NOT NULL CONSTRAINT UQ_Ships_ShipCode UNIQUE,
        ShipName NVARCHAR(200) NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Ships_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Ships_UpdatedAt DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID(N'dbo.Decks', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Decks
    (
        DeckId INT IDENTITY(1,1) CONSTRAINT PK_Decks PRIMARY KEY,
        BrandCode NVARCHAR(10) NOT NULL,
        ShipCode NVARCHAR(10) NOT NULL,
        DeckCode NVARCHAR(10) NOT NULL,
        DeckName NVARCHAR(200) NOT NULL,
        DeckNumber INT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Decks_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Decks_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_Decks_ShipCode_DeckCode UNIQUE (ShipCode, DeckCode)
    );
END
GO

IF OBJECT_ID(N'dbo.Regions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Regions
    (
        RegionCode NVARCHAR(20) NOT NULL CONSTRAINT PK_Regions PRIMARY KEY,
        RegionName NVARCHAR(200) NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Regions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Regions_UpdatedAt DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID(N'dbo.SubRegions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SubRegions
    (
        SubRegionCode NVARCHAR(20) NOT NULL CONSTRAINT PK_SubRegions PRIMARY KEY,
        Description NVARCHAR(200) NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_SubRegions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_SubRegions_UpdatedAt DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID(N'dbo.Ports', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Ports
    (
        PortCode NVARCHAR(10) NOT NULL CONSTRAINT PK_Ports PRIMARY KEY,
        PortName NVARCHAR(200) NOT NULL,
        CountryCode NVARCHAR(10) NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Ports_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Ports_UpdatedAt DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID(N'dbo.CabinCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CabinCategories
    (
        CabinCategoryId INT IDENTITY(1,1) CONSTRAINT PK_CabinCategories PRIMARY KEY,
        BrandCode NVARCHAR(10) NOT NULL,
        ShipCode NVARCHAR(10) NOT NULL,
        CategoryCode NVARCHAR(10) NOT NULL,
        Description NVARCHAR(200) NULL,
        InsideOutside NVARCHAR(5) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_CabinCategories_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_CabinCategories_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT UQ_CabinCategories_ShipCode_CategoryCode UNIQUE (ShipCode, CategoryCode)
    );
END
GO

IF OBJECT_ID(N'dbo.CabinConfigs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CabinConfigs
    (
        CabinConfigCode NVARCHAR(10) NOT NULL CONSTRAINT PK_CabinConfigs PRIMARY KEY,
        Description NVARCHAR(200) NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_CabinConfigs_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_CabinConfigs_UpdatedAt DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID(N'dbo.BedTypes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BedTypes
    (
        BedTypeCode NVARCHAR(10) NOT NULL CONSTRAINT PK_BedTypes PRIMARY KEY,
        Description NVARCHAR(200) NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_BedTypes_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_BedTypes_UpdatedAt DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID(N'dbo.Languages', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Languages
    (
        LanguageCode NVARCHAR(10) NOT NULL CONSTRAINT PK_Languages PRIMARY KEY,
        Description NVARCHAR(200) NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Languages_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Languages_UpdatedAt DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID(N'dbo.Gateways', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Gateways
    (
        AirportCode NVARCHAR(10) NOT NULL CONSTRAINT PK_Gateways PRIMARY KEY,
        AirportName NVARCHAR(200) NOT NULL,
        AirportCity NVARCHAR(100) NULL,
        AirportState NVARCHAR(50) NULL,
        AirportCountry NVARCHAR(50) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Gateways_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Gateways_UpdatedAt DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF OBJECT_ID(N'dbo.Titles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Titles
    (
        TitleCode NVARCHAR(20) NOT NULL CONSTRAINT PK_Titles PRIMARY KEY,
        Description NVARCHAR(200) NOT NULL,
        Dpcdty NVARCHAR(20) NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Titles_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Titles_UpdatedAt DEFAULT (SYSUTCDATETIME())
    );
END
GO
