SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_NULLS ON;
GO
-- ============================================================
-- FleetPro Database Schema â€” SQL Server
-- ============================================================
USE master;
GO
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'FleetProDB')
    CREATE DATABASE FleetProDB;
GO
USE FleetProDB;
GO

-- Tenants
CREATE TABLE Tenants (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CompanyName NVARCHAR(200) NOT NULL, Subdomain NVARCHAR(100) NOT NULL,
    ContactPerson NVARCHAR(200) NULL, Email NVARCHAR(150) NULL, Phone NVARCHAR(20) NULL,
    GstNumber NVARCHAR(30) NULL, Address NVARCHAR(500) NULL, City NVARCHAR(100) NULL, State NVARCHAR(100) NULL,
    [Plan] INT NOT NULL DEFAULT 1, Status INT NOT NULL DEFAULT 2,
    MaxTrucks INT NOT NULL DEFAULT 10, MaxUsers INT NOT NULL DEFAULT 5,
    SubscriptionStartDate DATETIME2 NULL, SubscriptionEndDate DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, CreatedBy INT NULL, UpdatedBy INT NULL,
    CONSTRAINT UQ_Tenants_Subdomain UNIQUE (Subdomain)
);
GO

-- Users
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL, Email NVARCHAR(150) NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL, Phone NVARCHAR(20) NULL,
    TenantId INT NULL, Status INT NOT NULL DEFAULT 1,
    LastLoginAt DATETIME2 NULL, ProfileImage NVARCHAR(500) NULL,
    RefreshToken NVARCHAR(500) NULL, RefreshTokenExpiry DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, CreatedBy INT NULL, UpdatedBy INT NULL,
    CONSTRAINT UQ_Users_Email UNIQUE (Email),
    CONSTRAINT FK_Users_Tenant FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);
GO

-- Roles
CREATE TABLE Roles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL, Description NVARCHAR(200) NULL,
    IsSystemRole BIT NOT NULL DEFAULT 0, TenantId INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, CreatedBy INT NULL, UpdatedBy INT NULL
);
GO

-- UserRoles
CREATE TABLE UserRoles (
    Id INT IDENTITY(1,1) PRIMARY KEY, UserId INT NOT NULL, RoleId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, CreatedBy INT NULL, UpdatedBy INT NULL,
    CONSTRAINT UQ_UserRoles UNIQUE (UserId, RoleId),
    CONSTRAINT FK_UserRoles_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_Role FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
);
GO

-- Permissions
CREATE TABLE Permissions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Module NVARCHAR(100) NOT NULL, Action NVARCHAR(100) NOT NULL, [Key] NVARCHAR(150) NOT NULL,
    Description NVARCHAR(200) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, CreatedBy INT NULL, UpdatedBy INT NULL,
    CONSTRAINT UQ_Permissions_Key UNIQUE ([Key])
);
GO

-- RolePermissions
CREATE TABLE RolePermissions (
    Id INT IDENTITY(1,1) PRIMARY KEY, RoleId INT NOT NULL, PermissionId INT NOT NULL,
    IsGranted BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, CreatedBy INT NULL, UpdatedBy INT NULL,
    CONSTRAINT UQ_RolePermissions UNIQUE (RoleId, PermissionId),
    CONSTRAINT FK_RolePerms_Role FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE,
    CONSTRAINT FK_RolePerms_Perm FOREIGN KEY (PermissionId) REFERENCES Permissions(Id) ON DELETE CASCADE
);
GO

-- UserPermissions
CREATE TABLE UserPermissions (
    Id INT IDENTITY(1,1) PRIMARY KEY, UserId INT NOT NULL, PermissionId INT NOT NULL,
    IsGranted BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, CreatedBy INT NULL, UpdatedBy INT NULL,
    CONSTRAINT UQ_UserPermissions UNIQUE (UserId, PermissionId),
    CONSTRAINT FK_UserPerms_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserPerms_Perm FOREIGN KEY (PermissionId) REFERENCES Permissions(Id) ON DELETE CASCADE
);
GO

-- Trucks
CREATE TABLE Trucks (
    Id INT IDENTITY(1,1) PRIMARY KEY, TenantId INT NOT NULL,
    NumberPlate NVARCHAR(20) NOT NULL, Model NVARCHAR(100) NOT NULL,
    Make NVARCHAR(50) NULL, ManufacturingYear INT NULL,
    EngineNumber NVARCHAR(50) NULL, ChassisNumber NVARCHAR(50) NULL,
    FitnessExpiry DATE NULL, InsuranceExpiry DATE NULL, TaxExpiry DATE NULL, PermitExpiry DATE NULL,
    InsurancePolicyNumber NVARCHAR(50) NULL, Status INT NOT NULL DEFAULT 1,
    LoadCapacityTons DECIMAL(8,2) NULL, Notes NVARCHAR(200) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, CreatedBy INT NULL, UpdatedBy INT NULL,
    CONSTRAINT UQ_Trucks_Plate UNIQUE (TenantId, NumberPlate),
    CONSTRAINT FK_Trucks_Tenant FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);
GO

-- Drivers
CREATE TABLE Drivers (
    Id INT IDENTITY(1,1) PRIMARY KEY, TenantId INT NOT NULL,
    FullName NVARCHAR(100) NOT NULL, Phone NVARCHAR(20) NULL, Email NVARCHAR(150) NULL,
    LicenseNumber NVARCHAR(30) NOT NULL, LicenseExpiry DATE NULL, LicenseType NVARCHAR(50) NULL,
    Address NVARCHAR(500) NULL, DateOfBirth DATE NULL, AadharNumber NVARCHAR(30) NULL,
    PanNumber NVARCHAR(30) NULL, Status INT NOT NULL DEFAULT 1,
    MonthlySalary DECIMAL(10,2) NULL, BankAccountNumber NVARCHAR(200) NULL, IFSC NVARCHAR(20) NULL,
    ProfileImage NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, CreatedBy INT NULL, UpdatedBy INT NULL,
    CONSTRAINT FK_Drivers_Tenant FOREIGN KEY (TenantId) REFERENCES Tenants(Id)
);
GO

-- Trips
CREATE TABLE Trips (
    Id INT IDENTITY(1,1) PRIMARY KEY, TenantId INT NOT NULL,
    TripNumber NVARCHAR(20) NOT NULL, TruckId INT NOT NULL, DriverId INT NOT NULL,
    FromLocation NVARCHAR(200) NOT NULL, ToLocation NVARCHAR(200) NOT NULL,
    StartDate DATETIME2 NOT NULL, EndDate DATETIME2 NULL,
    DistanceKm DECIMAL(10,2) NULL, Revenue DECIMAL(18,2) NOT NULL DEFAULT 0,
    CargoDescription NVARCHAR(200) NULL, CargoWeightTons DECIMAL(8,2) NULL,
    ClientName NVARCHAR(200) NULL, LRNumber NVARCHAR(100) NULL,
    Status INT NOT NULL DEFAULT 1, Notes NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, CreatedBy INT NULL, UpdatedBy INT NULL,
    CONSTRAINT UQ_Trips_Number UNIQUE (TenantId, TripNumber),
    CONSTRAINT FK_Trips_Tenant FOREIGN KEY (TenantId) REFERENCES Tenants(Id),
    CONSTRAINT FK_Trips_Truck  FOREIGN KEY (TruckId)  REFERENCES Trucks(Id),
    CONSTRAINT FK_Trips_Driver FOREIGN KEY (DriverId) REFERENCES Drivers(Id)
);
GO

-- TripDocuments
CREATE TABLE TripDocuments (
    Id INT IDENTITY(1,1) PRIMARY KEY, TenantId INT NOT NULL, TripId INT NOT NULL,
    FileName NVARCHAR(200) NOT NULL, FilePath NVARCHAR(500) NOT NULL,
    FileType NVARCHAR(50) NOT NULL, FileSizeBytes BIGINT NOT NULL DEFAULT 0,
    DocumentType NVARCHAR(100) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, CreatedBy INT NULL, UpdatedBy INT NULL,
    CONSTRAINT FK_TripDocs_Trip FOREIGN KEY (TripId) REFERENCES Trips(Id) ON DELETE CASCADE
);
GO

-- Expenses
CREATE TABLE Expenses (
    Id INT IDENTITY(1,1) PRIMARY KEY, TenantId INT NOT NULL, TripId INT NOT NULL,
    Category INT NOT NULL, Amount DECIMAL(18,2) NOT NULL, ExpenseDate DATE NOT NULL,
    Description NVARCHAR(500) NULL, VendorName NVARCHAR(100) NULL,
    BillNumber NVARCHAR(100) NULL, ReceiptPath NVARCHAR(500) NULL, HasReceipt BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, CreatedBy INT NULL, UpdatedBy INT NULL,
    CONSTRAINT FK_Expenses_Trip FOREIGN KEY (TripId) REFERENCES Trips(Id) ON DELETE CASCADE
);
GO

-- Alerts
CREATE TABLE Alerts (
    Id INT IDENTITY(1,1) PRIMARY KEY, TenantId INT NOT NULL,
    Type INT NOT NULL, Severity INT NOT NULL,
    Title NVARCHAR(200) NOT NULL, Message NVARCHAR(500) NULL,
    ReferenceId INT NULL, ReferenceType NVARCHAR(50) NULL,
    ExpiryDate DATE NOT NULL, DaysRemaining INT NOT NULL DEFAULT 0,
    IsRead BIT NOT NULL DEFAULT 0, IsNotified BIT NOT NULL DEFAULT 0, NotifiedAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, CreatedBy INT NULL, UpdatedBy INT NULL,
    CONSTRAINT FK_Alerts_Tenant FOREIGN KEY (TenantId) REFERENCES Tenants(Id) ON DELETE CASCADE
);
GO

-- AuditLogs
CREATE TABLE AuditLogs (
    Id INT IDENTITY(1,1) PRIMARY KEY, TenantId INT NULL, UserId INT NULL,
    Module NVARCHAR(100) NOT NULL, Action NVARCHAR(50) NOT NULL,
    Description NVARCHAR(500) NULL, IpAddress NVARCHAR(45) NULL,
    OldValues NVARCHAR(MAX) NULL, NewValues NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(), UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0, CreatedBy INT NULL, UpdatedBy INT NULL
);
GO

-- Performance Indexes
CREATE INDEX IX_Trucks_TenantId   ON Trucks(TenantId)  WHERE IsDeleted=0;
CREATE INDEX IX_Drivers_TenantId  ON Drivers(TenantId) WHERE IsDeleted=0;
CREATE INDEX IX_Trips_TenantId    ON Trips(TenantId)   WHERE IsDeleted=0;
CREATE INDEX IX_Trips_StartDate   ON Trips(StartDate)  WHERE IsDeleted=0;
CREATE INDEX IX_Trips_Status      ON Trips(Status)     WHERE IsDeleted=0;
CREATE INDEX IX_Expenses_TripId   ON Expenses(TripId)  WHERE IsDeleted=0;
CREATE INDEX IX_Alerts_TenantId   ON Alerts(TenantId)  WHERE IsDeleted=0;
CREATE INDEX IX_Users_TenantId    ON Users(TenantId)   WHERE IsDeleted=0;
GO

PRINT 'FleetPro schema created. Run the app â€” DataSeeder populates initial data.';
GO
