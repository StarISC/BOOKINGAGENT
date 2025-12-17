:setvar ROOT "D:\\STI\\BOOKING_AGENT\\BOOKINGAGENT"

USE [BookingAgentDB];
GO

-- Adjust ROOT above to your local repo path before running (SQLCMD mode).

BULK INSERT dbo.Ships
FROM '$(ROOT)\\document\\RCL Cruises Ltd - API TABLES\\Ship.csv'
WITH (FORMAT='CSV', FIRSTROW=2, CODEPAGE='65001', FIELDQUOTE = '\"');
GO

BULK INSERT dbo.Decks
FROM '$(ROOT)\\document\\RCL Cruises Ltd - API TABLES\\Deck.csv'
WITH (FORMAT='CSV', FIRSTROW=2, CODEPAGE='65001', FIELDQUOTE = '\"');
GO

BULK INSERT dbo.Regions
FROM '$(ROOT)\\document\\RCL Cruises Ltd - API TABLES\\Region.csv'
WITH (FORMAT='CSV', FIRSTROW=2, CODEPAGE='65001', FIELDQUOTE = '\"');
GO

BULK INSERT dbo.SubRegions
FROM '$(ROOT)\\document\\RCL Cruises Ltd - API TABLES\\Sub Region Code.csv'
WITH (FORMAT='CSV', FIRSTROW=2, CODEPAGE='65001', FIELDQUOTE = '\"');
GO

BULK INSERT dbo.Ports
FROM '$(ROOT)\\document\\RCL Cruises Ltd - API TABLES\\Ports.csv'
WITH (FORMAT='CSV', FIRSTROW=2, CODEPAGE='65001', FIELDQUOTE = '\"');
GO

BULK INSERT dbo.CabinCategories
FROM '$(ROOT)\\document\\RCL Cruises Ltd - API TABLES\\Cabin Category.csv'
WITH (FORMAT='CSV', FIRSTROW=2, CODEPAGE='65001', FIELDQUOTE = '\"');
GO

BULK INSERT dbo.CabinConfigs
FROM '$(ROOT)\\document\\RCL Cruises Ltd - API TABLES\\Cabin Config.csv'
WITH (FORMAT='CSV', FIRSTROW=2, CODEPAGE='65001', FIELDQUOTE = '\"');
GO

BULK INSERT dbo.BedTypes
FROM '$(ROOT)\\document\\RCL Cruises Ltd - API TABLES\\Bed Type.csv'
WITH (FORMAT='CSV', FIRSTROW=2, CODEPAGE='65001', FIELDQUOTE = '\"');
GO

BULK INSERT dbo.Languages
FROM '$(ROOT)\\document\\RCL Cruises Ltd - API TABLES\\Language.csv'
WITH (FORMAT='CSV', FIRSTROW=2, CODEPAGE='65001', FIELDQUOTE = '\"');
GO

BULK INSERT dbo.Gateways
FROM '$(ROOT)\\document\\RCL Cruises Ltd - API TABLES\\Gateway.csv'
WITH (FORMAT='CSV', FIRSTROW=2, CODEPAGE='65001', FIELDQUOTE = '\"');
GO

BULK INSERT dbo.Titles
FROM '$(ROOT)\\document\\RCL Cruises Ltd - API TABLES\\Title.csv'
WITH (FORMAT='CSV', FIRSTROW=2, CODEPAGE='65001', FIELDQUOTE = '\"');
GO
