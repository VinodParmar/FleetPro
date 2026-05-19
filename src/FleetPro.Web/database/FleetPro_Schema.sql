-- ═══════════════════════════════════════════════════════════════════════════════════════
--  FLEETPRO DATABASE SCHEMA — SQL Server
--  Version: 3.0 (Clean UP/DOWN Phase with Rate & DealAmount)
-- ═══════════════════════════════════════════════════════════════════════════════════════
USE master;
GO
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'FleetPro_Alpha')
    CREATE DATABASE FleetPro_Alpha;
GO
USE FleetPro_Alpha;
GO

-- Drop existing tables (in correct order due to FK constraints)
IF OBJECT_ID('dbo.AuditLogs', 'U') IS NOT NULL DROP TABLE dbo.AuditLogs;
IF OBJECT_ID('dbo.Alerts', 'U') IS NOT NULL DROP TABLE dbo.Alerts;
IF OBJECT_ID('dbo.Expenses', 'U') IS NOT NULL DROP TABLE dbo.Expenses;
IF OBJECT_ID('dbo.TripDocuments', 'U') IS NOT NULL DROP TABLE dbo.TripDocuments;
IF OBJECT_ID('dbo.TripPayments', 'U') IS NOT NULL DROP TABLE dbo.TripPayments;
IF OBJECT_ID('dbo.TripPhases', 'U') IS NOT NULL DROP TABLE dbo.TripPhases;
IF OBJECT_ID('dbo.Trips', 'U') IS NOT NULL DROP TABLE dbo.Trips;
IF OBJECT_ID('dbo.Agents', 'U') IS NOT NULL DROP TABLE dbo.Agents;
IF OBJECT_ID('dbo.Drivers', 'U') IS NOT NULL DROP TABLE dbo.Drivers;
IF OBJECT_ID('dbo.Trucks', 'U') IS NOT NULL DROP TABLE dbo.Trucks;
IF OBJECT_ID('dbo.ExpenseCategories', 'U') IS NOT NULL DROP TABLE dbo.ExpenseCategories;
IF OBJECT_ID('dbo.MenuItems', 'U') IS NOT NULL DROP TABLE dbo.MenuItems;
IF OBJECT_ID('dbo.UserPermissions', 'U') IS NOT NULL DROP TABLE dbo.UserPermissions;
IF OBJECT_ID('dbo.RolePermissions', 'U') IS NOT NULL DROP TABLE dbo.RolePermissions;
IF OBJECT_ID('dbo.Permissions', 'U') IS NOT NULL DROP TABLE dbo.Permissions;
IF OBJECT_ID('dbo.UserRoles', 'U') IS NOT NULL DROP TABLE dbo.UserRoles;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
IF OBJECT_ID('dbo.Tenants', 'U') IS NOT NULL DROP TABLE dbo.Tenants;
GO

PRINT 'Creating FleetPro tables...';

-- ═══════════════════════════════════════════════════
--  TENANTS
-- ═══════════════════════════════════════════════════
CREATE TABLE Tenants (
    Id                      INT IDENTITY(1,1) PRIMARY KEY,
    CompanyName             NVARCHAR(200) NOT NULL,
    Subdomain               NVARCHAR(100) NOT NULL,
    ContactPerson           NVARCHAR(200) NULL,
    Email                   NVARCHAR(150) NULL,
    Phone                   NVARCHAR(20) NULL,
    GstNumber               NVARCHAR(30) NULL,
    Address                 NVARCHAR(500) NULL,
    City                    NVARCHAR(100) NULL,
    State                   NVARCHAR(100) NULL,
    [Plan]                  INT NOT NULL DEFAULT 1,
    Status                  INT NOT NULL DEFAULT 1,
    MaxTrucks               INT NOT NULL DEFAULT 10,
    MaxUsers                INT NOT NULL DEFAULT 5,
    SubscriptionStartDate   DATETIME2 NULL,
    SubscriptionEndDate     DATETIME2 NULL,
    CreatedAt               DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt               DATETIME2 NULL,
    IsDeleted               BIT NOT NULL DEFAULT 0,
    CreatedBy               INT NULL,
    UpdatedBy               INT NULL,
    CONSTRAINT UQ_Tenants_Subdomain UNIQUE (Subdomain)
);
GO

-- ═══════════════════════════════════════════════════
--  ROLES
-- ═══════════════════════════════════════════════════
CREATE TABLE Roles (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(100) NOT NULL,
    Description     NVARCHAR(200) NULL,
    IsSystemRole    BIT NOT NULL DEFAULT 0,
    TenantId        INT NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NULL,
    IsDeleted       BIT NOT NULL DEFAULT 0,
    CreatedBy       INT NULL,
    UpdatedBy       INT NULL
);
GO

-- ═══════════════════════════════════════════════════
--  USERS
-- ═══════════════════════════════════════════════════
CREATE TABLE Users (
    Id                  INT IDENTITY(1,1) PRIMARY KEY,
    TenantId            INT NULL,
    FullName            NVARCHAR(100) NOT NULL,
    Email               NVARCHAR(150) NOT NULL,
    PasswordHash        NVARCHAR(MAX) NOT NULL,
    Phone               NVARCHAR(20) NULL,
    Status              INT NOT NULL DEFAULT 1,
    LastLoginAt         DATETIME2 NULL,
    ProfileImage        NVARCHAR(500) NULL,
    RefreshToken        NVARCHAR(500) NULL,
    RefreshTokenExpiry  DATETIME2 NULL,
    CreatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    CreatedBy           INT NULL,
    UpdatedBy           INT NULL,
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT FK_Users_Tenant FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);
GO

-- ═══════════════════════════════════════════════════
--  USER ROLES
-- ═══════════════════════════════════════════════════
CREATE TABLE UserRoles (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    UserId      INT NOT NULL,
    RoleId      INT NOT NULL,
    CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2 NULL,
    IsDeleted   BIT NOT NULL DEFAULT 0,
    CreatedBy   INT NULL,
    UpdatedBy   INT NULL,
    CONSTRAINT UQ_UserRoles UNIQUE (UserId, RoleId),
    CONSTRAINT FK_UserRoles_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_Role FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
);
GO

-- ═══════════════════════════════════════════════════
--  PERMISSIONS
-- ═══════════════════════════════════════════════════
CREATE TABLE Permissions (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    [Key]       NVARCHAR(100) NOT NULL,
    Module      NVARCHAR(50) NOT NULL,
    Action      NVARCHAR(50) NOT NULL,
    Description NVARCHAR(200) NULL,
    CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2 NULL,
    IsDeleted   BIT NOT NULL DEFAULT 0,
    CreatedBy   INT NULL,
    UpdatedBy   INT NULL,
    CONSTRAINT UQ_Permissions_Key UNIQUE ([Key])
);
GO

-- ═══════════════════════════════════════════════════
--  ROLE PERMISSIONS
-- ═══════════════════════════════════════════════════
CREATE TABLE RolePermissions (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    RoleId          INT NOT NULL,
    PermissionId    INT NOT NULL,
    IsGranted       BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NULL,
    IsDeleted       BIT NOT NULL DEFAULT 0,
    CreatedBy       INT NULL,
    UpdatedBy       INT NULL,
    CONSTRAINT UQ_RolePermissions UNIQUE (RoleId, PermissionId),
    CONSTRAINT FK_RolePerms_Role FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE,
    CONSTRAINT FK_RolePerms_Perm FOREIGN KEY (PermissionId) REFERENCES Permissions(Id) ON DELETE CASCADE
);
GO

-- ═══════════════════════════════════════════════════
--  USER PERMISSIONS (Overrides)
-- ═══════════════════════════════════════════════════
CREATE TABLE UserPermissions (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    UserId          INT NOT NULL,
    PermissionId    INT NOT NULL,
    IsGranted       BIT NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NULL,
    IsDeleted       BIT NOT NULL DEFAULT 0,
    CreatedBy       INT NULL,
    UpdatedBy       INT NULL,
    CONSTRAINT UQ_UserPermissions UNIQUE (UserId, PermissionId),
    CONSTRAINT FK_UserPerms_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserPerms_Perm FOREIGN KEY (PermissionId) REFERENCES Permissions(Id) ON DELETE CASCADE
);
GO

-- ═══════════════════════════════════════════════════
--  MENU ITEMS
-- ═══════════════════════════════════════════════════
CREATE TABLE MenuItems (
    Id                  INT IDENTITY(1,1) PRIMARY KEY,
    ParentId            INT NULL,
    Title               NVARCHAR(100) NOT NULL,
    TitleHi             NVARCHAR(100) NULL,
    Icon                NVARCHAR(50) NULL,
    Url                 NVARCHAR(200) NULL,
    Controller          NVARCHAR(50) NULL,
    Action              NVARCHAR(50) NULL,
    RequiredPermission  NVARCHAR(100) NULL,
    SuperAdminOnly      BIT NOT NULL DEFAULT 0,
    TenantAdminOrAbove  BIT NOT NULL DEFAULT 0,
    SortOrder           INT NOT NULL DEFAULT 0,
    IsActive            BIT NOT NULL DEFAULT 1,
    CssClass            NVARCHAR(100) NULL,
    BadgeText           NVARCHAR(50) NULL,
    BadgeClass          NVARCHAR(50) NULL,
    CreatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    CreatedBy           INT NULL,
    UpdatedBy           INT NULL,
    CONSTRAINT FK_MenuItems_Parent FOREIGN KEY (ParentId) REFERENCES MenuItems(Id)
);
GO

-- ═══════════════════════════════════════════════════
--  EXPENSE CATEGORIES (Global Master)
-- ═══════════════════════════════════════════════════
CREATE TABLE ExpenseCategories (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL,
    Icon        NVARCHAR(50) NULL,
    Color       NVARCHAR(20) NULL,
    SortOrder   INT NOT NULL DEFAULT 0,
    IsActive    BIT NOT NULL DEFAULT 1,
    CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2 NULL,
    IsDeleted   BIT NOT NULL DEFAULT 0,
    CreatedBy   INT NULL,
    UpdatedBy   INT NULL
);
GO

-- ═══════════════════════════════════════════════════
--  TRUCKS
-- ═══════════════════════════════════════════════════
CREATE TABLE Trucks (
    Id                      INT IDENTITY(1,1) PRIMARY KEY,
    TenantId                INT NOT NULL,
    NumberPlate             NVARCHAR(20) NOT NULL,
    Model                   NVARCHAR(100) NOT NULL,
    Make                    NVARCHAR(50) NULL,
    ManufacturingYear       INT NULL,
    EngineNumber            NVARCHAR(50) NULL,
    ChassisNumber           NVARCHAR(50) NULL,
    FitnessExpiry           DATE NULL,
    InsuranceExpiry         DATE NULL,
    TaxExpiry               DATE NULL,
    PermitExpiry            DATE NULL,
    AuthorizationExpiry     DATE NULL,
    PUCExpiry               DATE NULL,
    InsurancePolicyNumber   NVARCHAR(50) NULL,
    Status                  INT NOT NULL DEFAULT 1,
    LoadCapacityTons        DECIMAL(10,2) NULL,
    Notes                   NVARCHAR(200) NULL,
    CreatedAt               DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt               DATETIME2 NULL,
    IsDeleted               BIT NOT NULL DEFAULT 0,
    CreatedBy               INT NULL,
    UpdatedBy               INT NULL,
    CONSTRAINT UQ_Trucks_Plate UNIQUE (TenantId, NumberPlate),
    CONSTRAINT FK_Trucks_Tenant FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);
GO

-- ═══════════════════════════════════════════════════
--  DRIVERS
-- ═══════════════════════════════════════════════════
CREATE TABLE Drivers (
    Id                  INT IDENTITY(1,1) PRIMARY KEY,
    TenantId            INT NOT NULL,
    FullName            NVARCHAR(100) NOT NULL,
    Phone               NVARCHAR(20) NULL,
    Email               NVARCHAR(150) NULL,
    LicenseNumber       NVARCHAR(30) NOT NULL,
    LicenseExpiry       DATE NULL,
    LicenseType         NVARCHAR(50) NULL,
    Address             NVARCHAR(500) NULL,
    DateOfBirth         DATE NULL,
    AadharNumber        NVARCHAR(30) NULL,
    PanNumber           NVARCHAR(30) NULL,
    Status              INT NOT NULL DEFAULT 1,
    MonthlySalary       DECIMAL(12,2) NULL,
    BankAccountNumber   NVARCHAR(200) NULL,
    IFSC                NVARCHAR(20) NULL,
    ProfileImage        NVARCHAR(500) NULL,
    CreatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    CreatedBy           INT NULL,
    UpdatedBy           INT NULL,
    CONSTRAINT FK_Drivers_Tenant FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);
GO

-- ═══════════════════════════════════════════════════
--  AGENTS (Brokers)
-- ═══════════════════════════════════════════════════
CREATE TABLE Agents (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    TenantId    INT NOT NULL,
    Name        NVARCHAR(100) NOT NULL,
    Phone       NVARCHAR(20) NULL,
    Email       NVARCHAR(150) NULL,
    CompanyName NVARCHAR(200) NULL,
    Address     NVARCHAR(500) NULL,
    GSTNumber   NVARCHAR(30) NULL,
    PanNumber   NVARCHAR(30) NULL,
    Status      INT NOT NULL DEFAULT 1,
    Notes       NVARCHAR(500) NULL,
    CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2 NULL,
    IsDeleted   BIT NOT NULL DEFAULT 0,
    CreatedBy   INT NULL,
    UpdatedBy   INT NULL,
    CONSTRAINT FK_Agents_Tenant FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);
GO

-- ═══════════════════════════════════════════════════
--  TRIPS (Container Only - Clean Structure)
-- ═══════════════════════════════════════════════════
CREATE TABLE Trips (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    TenantId    INT NOT NULL,
    TripNumber  NVARCHAR(20) NOT NULL,
    TruckId     INT NOT NULL,
    DriverId    INT NOT NULL,
    Status      INT NOT NULL DEFAULT 1,
    Notes       NVARCHAR(500) NULL,
    CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2 NULL,
    IsDeleted   BIT NOT NULL DEFAULT 0,
    CreatedBy   INT NULL,
    UpdatedBy   INT NULL,
    CONSTRAINT UQ_Trips_Number UNIQUE (TenantId, TripNumber),
    CONSTRAINT FK_Trips_Tenant FOREIGN KEY (TenantId) REFERENCES Tenants(Id),
    CONSTRAINT FK_Trips_Truck FOREIGN KEY (TruckId) REFERENCES Trucks(Id),
    CONSTRAINT FK_Trips_Driver FOREIGN KEY (DriverId) REFERENCES Drivers(Id)
);
GO

-- ═══════════════════════════════════════════════════
--  TRIP PHASES (UP / DOWN with Rate & DealAmount)
-- ═══════════════════════════════════════════════════
CREATE TABLE TripPhases (
    Id                  INT IDENTITY(1,1) PRIMARY KEY,
    TenantId            INT NOT NULL,
    TripId              INT NOT NULL,
    PhaseType           INT NOT NULL,
    FromLocation        NVARCHAR(200) NOT NULL,
    ToLocation          NVARCHAR(200) NOT NULL,
    StartMeterReading   DECIMAL(12,2) NOT NULL DEFAULT 0,
    EndMeterReading     DECIMAL(12,2) NULL,
    StartDate           DATETIME2 NOT NULL,
    EndDate             DATETIME2 NULL,
    AgentId             INT NULL,
    LRNumber            NVARCHAR(100) NULL,
    CargoDescription    NVARCHAR(200) NULL,
    TareWeight          DECIMAL(10,2) NULL,
    NetWeight           DECIMAL(10,2) NULL,
    Rate                DECIMAL(18,2) NOT NULL DEFAULT 0,
    DealAmount          DECIMAL(18,2) NOT NULL DEFAULT 0,
    Status              INT NOT NULL DEFAULT 1,
    Notes               NVARCHAR(500) NULL,
    CreatedAt           DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt           DATETIME2 NULL,
    IsDeleted           BIT NOT NULL DEFAULT 0,
    CreatedBy           INT NULL,
    UpdatedBy           INT NULL,
    CONSTRAINT FK_TripPhases_Trip FOREIGN KEY (TripId) REFERENCES Trips(Id) ON DELETE CASCADE,
    CONSTRAINT FK_TripPhases_Agent FOREIGN KEY (AgentId) REFERENCES Agents(Id) ON DELETE SET NULL
);
GO

-- ═══════════════════════════════════════════════════
--  TRIP PAYMENTS (Ledger)
-- ═══════════════════════════════════════════════════
CREATE TABLE TripPayments (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    TenantId        INT NOT NULL,
    TripId          INT NOT NULL,
    PaymentType     INT NOT NULL,
    Amount          DECIMAL(18,2) NOT NULL,
    PaymentDate     DATETIME2 NOT NULL,
    PaymentMode     INT NOT NULL DEFAULT 1,
    ReferenceNumber NVARCHAR(100) NULL,
    PayerPayee      NVARCHAR(200) NULL,
    Description     NVARCHAR(500) NULL,
    ReceiptPath     NVARCHAR(500) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NULL,
    IsDeleted       BIT NOT NULL DEFAULT 0,
    CreatedBy       INT NULL,
    UpdatedBy       INT NULL,
    CONSTRAINT FK_TripPayments_Trip FOREIGN KEY (TripId) REFERENCES Trips(Id) ON DELETE CASCADE
);
GO

-- ═══════════════════════════════════════════════════
--  TRIP DOCUMENTS
-- ═══════════════════════════════════════════════════
CREATE TABLE TripDocuments (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    TenantId        INT NOT NULL,
    TripId          INT NOT NULL,
    FileName        NVARCHAR(200) NOT NULL,
    FilePath        NVARCHAR(500) NOT NULL,
    FileType        NVARCHAR(50) NOT NULL,
    FileSizeBytes   BIGINT NOT NULL DEFAULT 0,
    DocumentType    NVARCHAR(100) NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NULL,
    IsDeleted       BIT NOT NULL DEFAULT 0,
    CreatedBy       INT NULL,
    UpdatedBy       INT NULL,
    CONSTRAINT FK_TripDocs_Trip FOREIGN KEY (TripId) REFERENCES Trips(Id) ON DELETE CASCADE
);
GO

-- ═══════════════════════════════════════════════════
--  EXPENSES
-- ═══════════════════════════════════════════════════
CREATE TABLE Expenses (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    TenantId        INT NOT NULL,
    TripId          INT NOT NULL,
    TripPhaseId     INT NULL,
    CategoryId      INT NULL,
    Category        INT NOT NULL DEFAULT 9,
    Amount          DECIMAL(18,2) NOT NULL,
    ExpenseDate     DATETIME2 NOT NULL,
    Description     NVARCHAR(500) NULL,
    VendorName      NVARCHAR(100) NULL,
    BillNumber      NVARCHAR(100) NULL,
    ReceiptPath     NVARCHAR(500) NULL,
    HasReceipt      BIT NOT NULL DEFAULT 0,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NULL,
    IsDeleted       BIT NOT NULL DEFAULT 0,
    CreatedBy       INT NULL,
    UpdatedBy       INT NULL,
    CONSTRAINT FK_Expenses_Trip FOREIGN KEY (TripId) REFERENCES Trips(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Expenses_Phase FOREIGN KEY (TripPhaseId) REFERENCES TripPhases(Id),
    CONSTRAINT FK_Expenses_Category FOREIGN KEY (CategoryId) REFERENCES ExpenseCategories(Id)
);
GO

-- ═══════════════════════════════════════════════════
--  ALERTS
-- ═══════════════════════════════════════════════════
CREATE TABLE Alerts (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    TenantId        INT NOT NULL,
    Type            INT NOT NULL,
    Severity        INT NOT NULL,
    Title           NVARCHAR(200) NOT NULL,
    Message         NVARCHAR(500) NULL,
    ReferenceId     INT NULL,
    ReferenceType   NVARCHAR(50) NULL,
    ExpiryDate      DATE NOT NULL,
    DaysRemaining   INT NOT NULL DEFAULT 0,
    IsRead          BIT NOT NULL DEFAULT 0,
    IsNotified      BIT NOT NULL DEFAULT 0,
    NotifiedAt      DATETIME2 NULL,
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt       DATETIME2 NULL,
    IsDeleted       BIT NOT NULL DEFAULT 0,
    CreatedBy       INT NULL,
    UpdatedBy       INT NULL,
    CONSTRAINT FK_Alerts_Tenant FOREIGN KEY (TenantId) REFERENCES Tenants(Id) ON DELETE CASCADE
);
GO

-- ═══════════════════════════════════════════════════
--  AUDIT LOGS
-- ═══════════════════════════════════════════════════
CREATE TABLE AuditLogs (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    TenantId    INT NULL,
    UserId      INT NULL,
    Module      NVARCHAR(100) NOT NULL,
    Action      NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500) NULL,
    IpAddress   NVARCHAR(45) NULL,
    OldValues   NVARCHAR(MAX) NULL,
    NewValues   NVARCHAR(MAX) NULL,
    CreatedAt   DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt   DATETIME2 NULL,
    IsDeleted   BIT NOT NULL DEFAULT 0,
    CreatedBy   INT NULL,
    UpdatedBy   INT NULL
);
GO

-- ═══════════════════════════════════════════════════
--  INDEXES
-- ═══════════════════════════════════════════════════
CREATE INDEX IX_Users_TenantId ON Users(TenantId) WHERE IsDeleted=0;
CREATE INDEX IX_Trucks_TenantId ON Trucks(TenantId) WHERE IsDeleted=0;
CREATE INDEX IX_Drivers_TenantId ON Drivers(TenantId) WHERE IsDeleted=0;
CREATE INDEX IX_Agents_TenantId ON Agents(TenantId) WHERE IsDeleted=0;
CREATE INDEX IX_Trips_TenantId ON Trips(TenantId) WHERE IsDeleted=0;
CREATE INDEX IX_TripPhases_TripId ON TripPhases(TripId) WHERE IsDeleted=0;
CREATE INDEX IX_TripPhases_AgentId ON TripPhases(AgentId);
CREATE INDEX IX_TripPayments_TripId ON TripPayments(TripId) WHERE IsDeleted=0;
CREATE INDEX IX_Expenses_TripId ON Expenses(TripId) WHERE IsDeleted=0;
CREATE INDEX IX_Alerts_TenantId ON Alerts(TenantId) WHERE IsDeleted=0;
CREATE INDEX IX_AuditLogs_TenantId ON AuditLogs(TenantId, CreatedAt DESC);
GO

PRINT 'Tables and indexes created successfully.';
PRINT 'Run the application — DataSeeder will populate master data.';
GO
