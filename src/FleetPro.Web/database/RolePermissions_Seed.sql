-- ============================================================
--  FleetPro — Role Permissions Seed Script
--  Mirrors DataSeeder.SeedPermissionsAsync + SeedRolesAsync
--  Run on an empty database AFTER schema creation.
--  Safe to re-run: uses IF NOT EXISTS / MERGE patterns.
-- ============================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;

-- ─────────────────────────────────────────────
--  1. PERMISSIONS
-- ─────────────────────────────────────────────
PRINT '>> Seeding Permissions...';

DECLARE @perms TABLE (
    Module  NVARCHAR(50),
    Action  NVARCHAR(50),
    [Key]   NVARCHAR(100),
    [Desc]  NVARCHAR(200)
);

INSERT INTO @perms VALUES
-- Tenants
('Tenants',  'View',           'tenants.view',           'View Tenants'),
('Tenants',  'Create',         'tenants.create',         'Create Tenants'),
('Tenants',  'Edit',           'tenants.edit',           'Edit Tenants'),
('Tenants',  'Delete',         'tenants.delete',         'Delete Tenants'),
-- Users
('Users',    'View',           'users.view',             'View Users'),
('Users',    'Create',         'users.create',           'Create Users'),
('Users',    'Edit',           'users.edit',             'Edit Users'),
('Users',    'Delete',         'users.delete',           'Delete Users'),
('Users',    'AssignRoles',    'users.assignroles',      'AssignRoles Users'),
-- Trucks
('Trucks',   'View',           'trucks.view',            'View Trucks'),
('Trucks',   'Create',         'trucks.create',          'Create Trucks'),
('Trucks',   'Edit',           'trucks.edit',            'Edit Trucks'),
('Trucks',   'Delete',         'trucks.delete',          'Delete Trucks'),
-- Drivers
('Drivers',  'View',           'drivers.view',           'View Drivers'),
('Drivers',  'Create',         'drivers.create',         'Create Drivers'),
('Drivers',  'Edit',           'drivers.edit',           'Edit Drivers'),
('Drivers',  'Delete',         'drivers.delete',         'Delete Drivers'),
-- Trips
('Trips',    'View',           'trips.view',             'View Trips'),
('Trips',    'Create',         'trips.create',           'Create Trips'),
('Trips',    'Edit',           'trips.edit',             'Edit Trips'),
('Trips',    'Delete',         'trips.delete',           'Delete Trips'),
('Trips',    'AttachDocument', 'trips.attachdocument',   'AttachDocument Trips'),
-- Expenses
('Expenses', 'View',           'expenses.view',          'View Expenses'),
('Expenses', 'Create',         'expenses.create',        'Create Expenses'),
('Expenses', 'Edit',           'expenses.edit',          'Edit Expenses'),
('Expenses', 'Delete',         'expenses.delete',        'Delete Expenses'),
('Expenses', 'AttachBill',     'expenses.attachbill',    'AttachBill Expenses'),
-- Reports
('Reports',  'View',           'reports.view',           'View Reports'),
('Reports',  'Export',         'reports.export',         'Export Reports'),
-- Alerts
('Alerts',   'View',           'alerts.view',            'View Alerts'),
('Alerts',   'Manage',         'alerts.manage',          'Manage Alerts'),
-- Dashboard
('Dashboard','View',           'dashboard.view',         'View Dashboard');

-- Insert only missing permissions
INSERT INTO Permissions (Module, Action, [Key], Description, CreatedAt, UpdatedAt, IsDeleted)
SELECT p.Module, p.Action, p.[Key], p.[Desc], GETUTCDATE(), GETUTCDATE(), 0
FROM   @perms p
WHERE  NOT EXISTS (
    SELECT 1 FROM Permissions x WHERE x.[Key] = p.[Key]
);

PRINT '   Done. Total permissions: ' + CAST((SELECT COUNT(*) FROM Permissions) AS NVARCHAR);

-- ─────────────────────────────────────────────
--  2. ROLES
-- ─────────────────────────────────────────────
PRINT '>> Seeding Roles...';

-- SuperAdmin
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'SuperAdmin')
    INSERT INTO Roles (Name, Description, IsSystemRole, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('SuperAdmin', 'Full system access', 1, 0, GETUTCDATE(), GETUTCDATE());

-- TenantAdmin
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'TenantAdmin')
    INSERT INTO Roles (Name, Description, IsSystemRole, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('TenantAdmin', 'Full access within their tenant', 1, 0, GETUTCDATE(), GETUTCDATE());

-- DataEntryOperator
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'DataEntryOperator')
    INSERT INTO Roles (Name, Description, IsSystemRole, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('DataEntryOperator', 'Create and edit, no delete', 1, 0, GETUTCDATE(), GETUTCDATE());

-- Viewer
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Name = 'Viewer')
    INSERT INTO Roles (Name, Description, IsSystemRole, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('Viewer', 'Read-only access', 1, 0, GETUTCDATE(), GETUTCDATE());

PRINT '   Done.';

-- ─────────────────────────────────────────────
--  3. ROLE PERMISSIONS
-- ─────────────────────────────────────────────
PRINT '>> Seeding RolePermissions...';

-- Declare role ID variables
DECLARE @rSuperAdmin  INT;
DECLARE @rTenantAdmin INT;
DECLARE @rDataEntry   INT;
DECLARE @rViewer      INT;

SELECT @rSuperAdmin  = Id FROM Roles WHERE Name = 'SuperAdmin';
SELECT @rTenantAdmin = Id FROM Roles WHERE Name = 'TenantAdmin';
SELECT @rDataEntry   = Id FROM Roles WHERE Name = 'DataEntryOperator';
SELECT @rViewer      = Id FROM Roles WHERE Name = 'Viewer';

-- Helper: insert a RolePermission if not already present
-- We use a CTE + MERGE approach per role for clarity

-- ── SuperAdmin: ALL permissions ──────────────
INSERT INTO RolePermissions (RoleId, PermissionId, IsGranted, IsDeleted, CreatedAt, UpdatedAt)
SELECT @rSuperAdmin, p.Id, 1, 0, GETUTCDATE(), GETUTCDATE()
FROM   Permissions p
WHERE  NOT EXISTS (
    SELECT 1 FROM RolePermissions rp
    WHERE  rp.RoleId = @rSuperAdmin AND rp.PermissionId = p.Id
);
PRINT '   SuperAdmin: ' + CAST(@@ROWCOUNT AS NVARCHAR) + ' rows inserted';

-- ── TenantAdmin: all EXCEPT Tenants.Create + Tenants.Delete ──
INSERT INTO RolePermissions (RoleId, PermissionId, IsGranted, IsDeleted, CreatedAt, UpdatedAt)
SELECT @rTenantAdmin, p.Id, 1, 0, GETUTCDATE(), GETUTCDATE()
FROM   Permissions p
WHERE  NOT (p.Module = 'Tenants' AND p.Action IN ('Create','Delete'))
AND    NOT EXISTS (
    SELECT 1 FROM RolePermissions rp
    WHERE  rp.RoleId = @rTenantAdmin AND rp.PermissionId = p.Id
);
PRINT '   TenantAdmin: ' + CAST(@@ROWCOUNT AS NVARCHAR) + ' rows inserted';

-- ── DataEntryOperator: View + Create + Edit + AttachDocument + AttachBill ──
INSERT INTO RolePermissions (RoleId, PermissionId, IsGranted, IsDeleted, CreatedAt, UpdatedAt)
SELECT @rDataEntry, p.Id, 1, 0, GETUTCDATE(), GETUTCDATE()
FROM   Permissions p
WHERE  p.Action IN ('View','Create','Edit','AttachDocument','AttachBill')
AND    NOT EXISTS (
    SELECT 1 FROM RolePermissions rp
    WHERE  rp.RoleId = @rDataEntry AND rp.PermissionId = p.Id
);
PRINT '   DataEntryOperator: ' + CAST(@@ROWCOUNT AS NVARCHAR) + ' rows inserted';

-- ── Viewer: View only ──
INSERT INTO RolePermissions (RoleId, PermissionId, IsGranted, IsDeleted, CreatedAt, UpdatedAt)
SELECT @rViewer, p.Id, 1, 0, GETUTCDATE(), GETUTCDATE()
FROM   Permissions p
WHERE  p.Action = 'View'
AND    NOT EXISTS (
    SELECT 1 FROM RolePermissions rp
    WHERE  rp.RoleId = @rViewer AND rp.PermissionId = p.Id
);
PRINT '   Viewer: ' + CAST(@@ROWCOUNT AS NVARCHAR) + ' rows inserted';

COMMIT TRANSACTION;
PRINT '>> All done. Role permissions seeded successfully.';

-- ─────────────────────────────────────────────
--  VERIFICATION QUERY
-- ─────────────────────────────────────────────
SELECT
    r.Name              AS Role,
    p.Module,
    p.Action,
    p.[Key]             AS PermissionKey,
    rp.IsGranted
FROM RolePermissions rp
JOIN Roles       r  ON r.Id = rp.RoleId
JOIN Permissions p  ON p.Id = rp.PermissionId
ORDER BY
    CASE r.Name
        WHEN 'SuperAdmin'        THEN 1
        WHEN 'TenantAdmin'       THEN 2
        WHEN 'DataEntryOperator' THEN 3
        WHEN 'Viewer'            THEN 4
    END,
    p.Module, p.Action;
