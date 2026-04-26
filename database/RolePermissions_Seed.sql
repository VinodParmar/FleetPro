
-- -- CLEANUP DUPLICATE RolePermissions (run if 500 error on Role/Index) --
WITH CTE AS (
    SELECT Id, ROW_NUMBER() OVER (PARTITION BY RoleId, PermissionId ORDER BY Id) AS rn
    FROM RolePermissions
)
DELETE FROM CTE WHERE rn > 1;
PRINT 'Duplicates removed: ' + CAST(@@ROWCOUNT AS NVARCHAR);
