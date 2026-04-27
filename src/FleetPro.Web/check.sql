CREATE TABLE [AuditLogs] (
    [Id] int NOT NULL IDENTITY,
    [TenantId] int NULL,
    [UserId] int NULL,
    [Module] nvarchar(100) NOT NULL,
    [Action] nvarchar(50) NOT NULL,
    [Description] nvarchar(500) NULL,
    [IpAddress] nvarchar(45) NULL,
    [OldValues] nvarchar(max) NULL,
    [NewValues] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [ExpenseCategories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Icon] nvarchar(50) NULL,
    [Color] nvarchar(20) NULL,
    [SortOrder] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_ExpenseCategories] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [MenuItems] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(100) NOT NULL,
    [Icon] nvarchar(max) NULL,
    [Controller] nvarchar(max) NULL,
    [Action] nvarchar(max) NULL,
    [ParentId] int NULL,
    [SortOrder] int NOT NULL,
    [RequiredPermission] nvarchar(max) NULL,
    [SuperAdminOnly] bit NOT NULL,
    [TenantAdminOrAbove] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_MenuItems] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MenuItems_MenuItems_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [MenuItems] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Permissions] (
    [Id] int NOT NULL IDENTITY,
    [Module] nvarchar(100) NOT NULL,
    [Action] nvarchar(100) NOT NULL,
    [Key] nvarchar(150) NOT NULL,
    [Description] nvarchar(200) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Tenants] (
    [Id] int NOT NULL IDENTITY,
    [CompanyName] nvarchar(200) NOT NULL,
    [Subdomain] nvarchar(100) NOT NULL,
    [ContactPerson] nvarchar(200) NULL,
    [Email] nvarchar(150) NULL,
    [Phone] nvarchar(20) NULL,
    [GstNumber] nvarchar(30) NULL,
    [Address] nvarchar(500) NULL,
    [City] nvarchar(100) NULL,
    [State] nvarchar(100) NULL,
    [Plan] int NOT NULL,
    [Status] int NOT NULL,
    [MaxTrucks] int NOT NULL,
    [MaxUsers] int NOT NULL,
    [SubscriptionStartDate] datetime2 NULL,
    [SubscriptionEndDate] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id])
);
GO


CREATE TABLE [Alerts] (
    [Id] int NOT NULL IDENTITY,
    [Type] int NOT NULL,
    [Severity] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Message] nvarchar(500) NULL,
    [ReferenceId] int NULL,
    [ReferenceType] nvarchar(max) NULL,
    [ExpiryDate] datetime2 NOT NULL,
    [DaysRemaining] int NOT NULL,
    [IsRead] bit NOT NULL,
    [IsNotified] bit NOT NULL,
    [NotifiedAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    [TenantId] int NOT NULL,
    CONSTRAINT [PK_Alerts] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Alerts_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Drivers] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(100) NOT NULL,
    [Phone] nvarchar(20) NULL,
    [Email] nvarchar(150) NULL,
    [LicenseNumber] nvarchar(30) NOT NULL,
    [LicenseExpiry] datetime2 NULL,
    [LicenseType] nvarchar(50) NULL,
    [Address] nvarchar(500) NULL,
    [DateOfBirth] datetime2 NULL,
    [AadharNumber] nvarchar(30) NULL,
    [PanNumber] nvarchar(30) NULL,
    [Status] int NOT NULL,
    [MonthlySalary] decimal(18,2) NULL,
    [BankAccountNumber] nvarchar(200) NULL,
    [IFSC] nvarchar(20) NULL,
    [ProfileImage] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    [TenantId] int NOT NULL,
    CONSTRAINT [PK_Drivers] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Drivers_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Roles] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(200) NULL,
    [IsSystemRole] bit NOT NULL,
    [TenantId] int NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_Roles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Roles_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id])
);
GO


CREATE TABLE [Trucks] (
    [Id] int NOT NULL IDENTITY,
    [NumberPlate] nvarchar(20) NOT NULL,
    [Model] nvarchar(100) NOT NULL,
    [Make] nvarchar(50) NULL,
    [ManufacturingYear] int NULL,
    [EngineNumber] nvarchar(50) NULL,
    [ChassisNumber] nvarchar(50) NULL,
    [FitnessExpiry] datetime2 NULL,
    [InsuranceExpiry] datetime2 NULL,
    [TaxExpiry] datetime2 NULL,
    [PermitExpiry] datetime2 NULL,
    [InsurancePolicyNumber] nvarchar(50) NULL,
    [Status] int NOT NULL,
    [LoadCapacityTons] decimal(18,2) NULL,
    [Notes] nvarchar(200) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    [TenantId] int NOT NULL,
    CONSTRAINT [PK_Trucks] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Trucks_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(100) NOT NULL,
    [Email] nvarchar(150) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [Phone] nvarchar(20) NULL,
    [TenantId] int NULL,
    [Status] int NOT NULL,
    [LastLoginAt] datetime2 NULL,
    [ProfileImage] nvarchar(max) NULL,
    [RefreshToken] nvarchar(max) NULL,
    [RefreshTokenExpiry] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Users_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [RolePermissions] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] int NOT NULL,
    [PermissionId] int NOT NULL,
    [IsGranted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Trips] (
    [Id] int NOT NULL IDENTITY,
    [TripNumber] nvarchar(20) NOT NULL,
    [TruckId] int NOT NULL,
    [DriverId] int NOT NULL,
    [FromLocation] nvarchar(200) NOT NULL,
    [ToLocation] nvarchar(200) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NULL,
    [DistanceKm] decimal(18,2) NULL,
    [Revenue] decimal(18,2) NOT NULL,
    [CargoDescription] nvarchar(200) NULL,
    [CargoWeightTons] decimal(18,2) NULL,
    [ClientName] nvarchar(200) NULL,
    [LRNumber] nvarchar(100) NULL,
    [Status] int NOT NULL,
    [Notes] nvarchar(500) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    [TenantId] int NOT NULL,
    CONSTRAINT [PK_Trips] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Trips_Drivers_DriverId] FOREIGN KEY ([DriverId]) REFERENCES [Drivers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Trips_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Trips_Trucks_TruckId] FOREIGN KEY ([TruckId]) REFERENCES [Trucks] ([Id]) ON DELETE NO ACTION
);
GO


CREATE TABLE [UserPermissions] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [PermissionId] int NOT NULL,
    [IsGranted] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_UserPermissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserPermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [Permissions] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserPermissions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [UserRoles] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [RoleId] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [Expenses] (
    [Id] int NOT NULL IDENTITY,
    [TripId] int NOT NULL,
    [CategoryId] int NULL,
    [Category] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [ExpenseDate] datetime2 NOT NULL,
    [Description] nvarchar(500) NULL,
    [VendorName] nvarchar(100) NULL,
    [BillNumber] nvarchar(100) NULL,
    [ReceiptPath] nvarchar(max) NULL,
    [HasReceipt] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    [TenantId] int NOT NULL,
    CONSTRAINT [PK_Expenses] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Expenses_ExpenseCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [ExpenseCategories] ([Id]) ON DELETE SET NULL,
    CONSTRAINT [FK_Expenses_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Expenses_Trips_TripId] FOREIGN KEY ([TripId]) REFERENCES [Trips] ([Id]) ON DELETE CASCADE
);
GO


CREATE TABLE [TripDocuments] (
    [Id] int NOT NULL IDENTITY,
    [TripId] int NOT NULL,
    [FileName] nvarchar(200) NOT NULL,
    [FilePath] nvarchar(500) NOT NULL,
    [FileType] nvarchar(50) NOT NULL,
    [FileSizeBytes] bigint NOT NULL,
    [DocumentType] nvarchar(100) NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    [IsDeleted] bit NOT NULL,
    [CreatedBy] int NULL,
    [UpdatedBy] int NULL,
    [TenantId] int NOT NULL,
    CONSTRAINT [PK_TripDocuments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TripDocuments_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TripDocuments_Trips_TripId] FOREIGN KEY ([TripId]) REFERENCES [Trips] ([Id]) ON DELETE CASCADE
);
GO


CREATE INDEX [IX_Alerts_TenantId] ON [Alerts] ([TenantId]);
GO


CREATE INDEX [IX_Drivers_TenantId] ON [Drivers] ([TenantId]);
GO


CREATE UNIQUE INDEX [IX_ExpenseCategories_Name] ON [ExpenseCategories] ([Name]);
GO


CREATE INDEX [IX_Expenses_CategoryId] ON [Expenses] ([CategoryId]);
GO


CREATE INDEX [IX_Expenses_TenantId] ON [Expenses] ([TenantId]);
GO


CREATE INDEX [IX_Expenses_TripId] ON [Expenses] ([TripId]);
GO


CREATE INDEX [IX_MenuItems_ParentId] ON [MenuItems] ([ParentId]);
GO


CREATE UNIQUE INDEX [IX_Permissions_Key] ON [Permissions] ([Key]);
GO


CREATE INDEX [IX_RolePermissions_PermissionId] ON [RolePermissions] ([PermissionId]);
GO


CREATE UNIQUE INDEX [IX_RolePermissions_RoleId_PermissionId] ON [RolePermissions] ([RoleId], [PermissionId]);
GO


CREATE INDEX [IX_Roles_TenantId] ON [Roles] ([TenantId]);
GO


CREATE UNIQUE INDEX [IX_Tenants_Subdomain] ON [Tenants] ([Subdomain]);
GO


CREATE INDEX [IX_TripDocuments_TenantId] ON [TripDocuments] ([TenantId]);
GO


CREATE INDEX [IX_TripDocuments_TripId] ON [TripDocuments] ([TripId]);
GO


CREATE INDEX [IX_Trips_DriverId] ON [Trips] ([DriverId]);
GO


CREATE UNIQUE INDEX [IX_Trips_TenantId_TripNumber] ON [Trips] ([TenantId], [TripNumber]);
GO


CREATE INDEX [IX_Trips_TruckId] ON [Trips] ([TruckId]);
GO


CREATE UNIQUE INDEX [IX_Trucks_TenantId_NumberPlate] ON [Trucks] ([TenantId], [NumberPlate]);
GO


CREATE INDEX [IX_UserPermissions_PermissionId] ON [UserPermissions] ([PermissionId]);
GO


CREATE UNIQUE INDEX [IX_UserPermissions_UserId_PermissionId] ON [UserPermissions] ([UserId], [PermissionId]);
GO


CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
GO


CREATE UNIQUE INDEX [IX_UserRoles_UserId_RoleId] ON [UserRoles] ([UserId], [RoleId]);
GO


CREATE UNIQUE INDEX [IX_Users_Email] ON [Users] ([Email]);
GO


CREATE INDEX [IX_Users_TenantId] ON [Users] ([TenantId]);
GO


