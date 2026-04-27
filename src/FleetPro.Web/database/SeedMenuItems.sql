-- ============================================
-- FleetPro: Create MenuItems Table & Seed Data
-- Run this script on FleetProDB database
-- ============================================

-- Create MenuItems table if not exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MenuItems')
BEGIN
    CREATE TABLE [MenuItems] (
        [Id] int NOT NULL IDENTITY(1,1),
        [Title] nvarchar(100) NOT NULL,
        [Icon] nvarchar(max) NULL,
        [Controller] nvarchar(max) NULL,
        [Action] nvarchar(max) NULL,
        [ParentId] int NULL,
        [SortOrder] int NOT NULL DEFAULT 0,
        [RequiredPermission] nvarchar(max) NULL,
        [SuperAdminOnly] bit NOT NULL DEFAULT 0,
        [TenantAdminOrAbove] bit NOT NULL DEFAULT 0,
        [IsActive] bit NOT NULL DEFAULT 1,
        [CreatedAt] datetime2 NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt] datetime2 NULL,
        [IsDeleted] bit NOT NULL DEFAULT 0,
        [CreatedBy] int NULL,
        [UpdatedBy] int NULL,
        CONSTRAINT [PK_MenuItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MenuItems_MenuItems_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [MenuItems] ([Id]) ON DELETE NO ACTION
    );
    CREATE INDEX [IX_MenuItems_ParentId] ON [MenuItems] ([ParentId]);
    PRINT 'MenuItems table created.';
END
ELSE
BEGIN
    PRINT 'MenuItems table already exists.';
END
GO

-- Seed menu items if table is empty
IF NOT EXISTS (SELECT 1 FROM [MenuItems])
BEGIN
    SET IDENTITY_INSERT [MenuItems] ON;

    -- Top-level menus (Id 1-10)
    INSERT INTO [MenuItems] ([Id],[Title],[Icon],[Controller],[Action],[ParentId],[SortOrder],[RequiredPermission],[SuperAdminOnly],[TenantAdminOrAbove],[IsActive],[CreatedAt],[IsDeleted])
    VALUES
        (1,  'Dashboard',  'fas fa-tachometer-alt', 'Dashboard', 'Index', NULL, 10, NULL,           0, 0, 1, GETUTCDATE(), 0),
        (2,  'Companies',  'fas fa-building',       NULL,        NULL,    NULL, 20, NULL,           1, 0, 1, GETUTCDATE(), 0),
        (3,  'Roles',      'fas fa-shield-alt',     NULL,        NULL,    NULL, 30, NULL,           1, 0, 1, GETUTCDATE(), 0),
        (4,  'Users',      'fas fa-users',          NULL,        NULL,    NULL, 40, 'users.view',   0, 1, 1, GETUTCDATE(), 0),
        (5,  'Trucks',     'fas fa-truck',          NULL,        NULL,    NULL, 50, 'trucks.view',  0, 0, 1, GETUTCDATE(), 0),
        (6,  'Drivers',    'fas fa-user-tie',       NULL,        NULL,    NULL, 60, 'drivers.view', 0, 0, 1, GETUTCDATE(), 0),
        (7,  'Trips',      'fas fa-route',          NULL,        NULL,    NULL, 70, 'trips.view',   0, 0, 1, GETUTCDATE(), 0),
        (8,  'Expenses',   'fas fa-receipt',        NULL,        NULL,    NULL, 80, 'expenses.view',0, 0, 1, GETUTCDATE(), 0),
        (9,  'Reports',    'fas fa-chart-line',     NULL,        NULL,    NULL, 90, 'reports.view', 0, 0, 1, GETUTCDATE(), 0),
        (10, 'Alerts',     'fas fa-bell',           'Alert',     'Index', NULL, 100,'alerts.view',  0, 0, 1, GETUTCDATE(), 0);

    -- Child menus (Id 11-25)
    INSERT INTO [MenuItems] ([Id],[Title],[Icon],[Controller],[Action],[ParentId],[SortOrder],[RequiredPermission],[SuperAdminOnly],[TenantAdminOrAbove],[IsActive],[CreatedAt],[IsDeleted])
    VALUES
        -- Companies children (Parent=2)
        (11, 'Company List', 'far fa-circle', 'Tenant', 'Index',  2, 1, NULL, 0, 0, 1, GETUTCDATE(), 0),
        (12, 'Add Company',  'far fa-circle', 'Tenant', 'Create', 2, 2, NULL, 0, 0, 1, GETUTCDATE(), 0),
        -- Roles children (Parent=3)
        (13, 'Role List',    'far fa-circle', 'Role',   'Index',  3, 1, NULL, 0, 0, 1, GETUTCDATE(), 0),
        -- Users children (Parent=4)
        (14, 'User List',    'far fa-circle', 'User',   'Index',  4, 1, NULL,            0, 0, 1, GETUTCDATE(), 0),
        (15, 'Add User',     'far fa-circle', 'User',   'Create', 4, 2, 'users.create',  0, 0, 1, GETUTCDATE(), 0),
        -- Trucks children (Parent=5)
        (16, 'Truck List',   'far fa-circle', 'Truck',  'Index',  5, 1, NULL,            0, 0, 1, GETUTCDATE(), 0),
        (17, 'Add Truck',    'far fa-circle', 'Truck',  'Create', 5, 2, 'trucks.create', 0, 0, 1, GETUTCDATE(), 0),
        -- Drivers children (Parent=6)
        (18, 'Driver List',  'far fa-circle', 'Driver', 'Index',  6, 1, NULL,             0, 0, 1, GETUTCDATE(), 0),
        (19, 'Add Driver',   'far fa-circle', 'Driver', 'Create', 6, 2, 'drivers.create', 0, 0, 1, GETUTCDATE(), 0),
        -- Trips children (Parent=7)
        (20, 'Trip List',    'far fa-circle', 'Trip',   'Index',  7, 1, NULL,           0, 0, 1, GETUTCDATE(), 0),
        (21, 'New Trip',     'far fa-circle', 'Trip',   'Create', 7, 2, 'trips.create', 0, 0, 1, GETUTCDATE(), 0),
        -- Expenses children (Parent=8)
        (22, 'Expense List', 'far fa-circle', 'Expense','Index',  8, 1, NULL,              0, 0, 1, GETUTCDATE(), 0),
        (23, 'Add Expense',  'far fa-circle', 'Expense','Create', 8, 2, 'expenses.create', 0, 0, 1, GETUTCDATE(), 0),
        -- Reports children (Parent=9)
        (24, 'P&L Report',          'far fa-circle', 'Report', 'Index',            9, 1, NULL,             0, 0, 1, GETUTCDATE(), 0),
        (25, 'Export Trips (Excel)','far fa-circle', 'Report', 'ExportTripsExcel', 9, 2, 'reports.export', 0, 0, 1, GETUTCDATE(), 0);

    SET IDENTITY_INSERT [MenuItems] OFF;
    
    PRINT 'Seeded 25 menu items (10 top-level + 15 children).';
END
ELSE
BEGIN
    PRINT 'MenuItems table already has data. Skipping seed.';
END
GO

-- Verify
SELECT COUNT(*) AS TotalMenuItems FROM [MenuItems];
SELECT Id, Title, ParentId, Controller, [Action], SortOrder FROM [MenuItems] ORDER BY COALESCE(ParentId, Id), SortOrder;
GO
