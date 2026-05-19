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
        [OnClick] nvarchar(max) NULL,
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
    -- Add OnClick column if it doesn't exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'MenuItems') AND name = 'OnClick')
    BEGIN
        ALTER TABLE [MenuItems] ADD [OnClick] nvarchar(max) NULL;
        PRINT 'OnClick column added to MenuItems table.';
    END
    PRINT 'MenuItems table already exists.';
END
GO

-- Seed menu items if table is empty
IF NOT EXISTS (SELECT 1 FROM [MenuItems])
BEGIN
    SET IDENTITY_INSERT [MenuItems] ON;

    -- ═══════════════════════════════════════════════════════════════
    -- MENU STRUCTURE - Logical Grouping
    -- ═══════════════════════════════════════════════════════════════
    -- 1. Dashboard (standalone)
    -- 2. Operations (Trips, Expenses) - daily work
    -- 3. Fleet (Trucks, Drivers) - vehicle management
    -- 4. Partners (Agents) - external parties
    -- 5. Reports & Analytics
    -- 6. Alerts (standalone)
    -- 7. Administration (Companies, Users, Roles, Audit)
    -- ═══════════════════════════════════════════════════════════════

    -- Top-level menus (Id 1-7)
    INSERT INTO [MenuItems] ([Id],[Title],[Icon],[Controller],[Action],[ParentId],[SortOrder],[RequiredPermission],[SuperAdminOnly],[TenantAdminOrAbove],[IsActive],[CreatedAt],[IsDeleted])
    VALUES
        (1,  'Dashboard',      'fas fa-tachometer-alt', 'Dashboard', 'Index', NULL, 10, NULL,           0, 0, 1, GETUTCDATE(), 0),
        (2,  'Operations',     'fas fa-clipboard-list', NULL,        NULL,    NULL, 20, 'trips.view',   0, 0, 1, GETUTCDATE(), 0),
        (3,  'Fleet',          'fas fa-truck-moving',   NULL,        NULL,    NULL, 30, 'trucks.view',  0, 0, 1, GETUTCDATE(), 0),
        (4,  'Partners',       'fas fa-handshake',      NULL,        NULL,    NULL, 40, 'agents.view',  0, 0, 1, GETUTCDATE(), 0),
        (5,  'Reports',        'fas fa-chart-bar',      NULL,        NULL,    NULL, 50, 'reports.view', 0, 0, 1, GETUTCDATE(), 0),
        (6,  'Alerts',         'fas fa-bell',           'Alert',     'Index', NULL, 60, 'alerts.view',  0, 0, 1, GETUTCDATE(), 0),
        (7,  'Administration', 'fas fa-cog',            NULL,        NULL,    NULL, 100,NULL,           0, 1, 1, GETUTCDATE(), 0);

    -- Child menus
    INSERT INTO [MenuItems] ([Id],[Title],[Icon],[Controller],[Action],[ParentId],[SortOrder],[RequiredPermission],[SuperAdminOnly],[TenantAdminOrAbove],[IsActive],[CreatedAt],[IsDeleted])
    VALUES
        -- Operations children (Parent=2)
        (10, 'Trips',     'fas fa-route',   'Trip',    'Index',  2, 1, NULL,           0, 0, 1, GETUTCDATE(), 0),
        (11, 'New Trip',  'fas fa-plus-circle', 'Trip', 'Create', 2, 2, 'trips.create', 0, 0, 1, GETUTCDATE(), 0),
        (12, 'Expenses',  'fas fa-receipt', 'Expense', 'Index',  2, 3, 'expenses.view',0, 0, 1, GETUTCDATE(), 0),
        -- Add Expense is handled via modal popup on Expense Index page

        -- Fleet children (Parent=3)
        (20, 'Trucks',  'fas fa-truck',    'Truck',  'Index', 3, 1, NULL,            0, 0, 1, GETUTCDATE(), 0),
        -- Add Truck is handled via modal popup on Truck Index page
        (21, 'Drivers', 'fas fa-user-tie', 'Driver', 'Index', 3, 2, 'drivers.view',  0, 0, 1, GETUTCDATE(), 0),
        -- Add Driver is handled via modal popup on Driver Index page

        -- Partners children (Parent=4)
        (30, 'Agents', 'fas fa-users-cog', 'Agent', 'Index', 4, 1, NULL, 0, 0, 1, GETUTCDATE(), 0),
        -- Add Agent is handled via modal popup on Agent Index page

        -- Reports children (Parent=5)
        (40, 'P&L Summary',     'fas fa-chart-line', 'Report', 'Index',            5, 1, NULL,             0, 0, 1, GETUTCDATE(), 0),
        (41, 'Trip Report',     'fas fa-file-alt',   'Report', 'TripReport',       5, 2, NULL,             0, 0, 1, GETUTCDATE(), 0),
        (42, 'Export to Excel', 'fas fa-file-excel', 'Report', 'ExportTripsExcel', 5, 3, 'reports.export', 0, 0, 1, GETUTCDATE(), 0),

        -- Administration children (Parent=7)
        (50, 'Companies',           'fas fa-building',    'Tenant',          'Index',  7, 1, NULL,           1, 0, 1, GETUTCDATE(), 0),
        (51, 'Add Company',         'fas fa-plus-circle', 'Tenant',          'Create', 7, 2, NULL,           1, 0, 1, GETUTCDATE(), 0),
        (52, 'Users',               'fas fa-users',       'User',            'Index',  7, 3, 'users.view',   0, 0, 1, GETUTCDATE(), 0),
        (53, 'Add User',            'fas fa-user-plus',   'User',            'Create', 7, 4, 'users.create', 0, 0, 1, GETUTCDATE(), 0),
        (54, 'Roles & Permissions', 'fas fa-shield-alt',  'Role',            'Index',  7, 5, NULL,           1, 0, 1, GETUTCDATE(), 0),
        (55, 'Expense Categories',  'fas fa-tags',        'ExpenseCategory', 'Index',  7, 6, NULL,           1, 0, 1, GETUTCDATE(), 0),
        (56, 'Audit Logs',          'fas fa-history',     'Audit',           'Index',  7, 7, NULL,           0, 1, 1, GETUTCDATE(), 0);

    SET IDENTITY_INSERT [MenuItems] OFF;

    PRINT 'Seeded menu items with logical grouping.';
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
