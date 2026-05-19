using Microsoft.EntityFrameworkCore.Migrations;

namespace FleetPro.Migrations;

/// <summary>
/// Adds OnClick column to MenuItems table for modal popup support.
/// </summary>
public partial class AddOnClickToMenuItem : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'MenuItems') AND name = 'OnClick')
            BEGIN
                ALTER TABLE [MenuItems] ADD [OnClick] nvarchar(max) NULL;
            END
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'MenuItems') AND name = 'OnClick')
            BEGIN
                ALTER TABLE [MenuItems] DROP COLUMN [OnClick];
            END
        ");
    }
}
