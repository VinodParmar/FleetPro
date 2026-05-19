using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetPro.Migrations
{
    /// <inheritdoc />
    public partial class TripEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ClientName",
                table: "Trips",
                newName: "AgentName");

            migrationBuilder.AddColumn<DateTime>(
                name: "AuthorizationExpiry",
                table: "Trucks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PUCExpiry",
                table: "Trucks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NetWeight",
                table: "Trips",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TareWeight",
                table: "Trips",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TripPhaseId",
                table: "Expenses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TripPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TripId = table.Column<int>(type: "int", nullable: false),
                    PaymentType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentMode = table.Column<int>(type: "int", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PayerPayee = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReceiptPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripPayments_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TripPayments_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TripPhases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TripId = table.Column<int>(type: "int", nullable: false),
                    PhaseType = table.Column<int>(type: "int", nullable: false),
                    FromLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ToLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartMeterReading = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    EndMeterReading = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AgentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LRNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CargoDescription = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Revenue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    UpdatedBy = table.Column<int>(type: "int", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripPhases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TripPhases_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TripPhases_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_TripPhaseId",
                table: "Expenses",
                column: "TripPhaseId");

            migrationBuilder.CreateIndex(
                name: "IX_TripPayments_TenantId",
                table: "TripPayments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TripPayments_TripId",
                table: "TripPayments",
                column: "TripId");

            migrationBuilder.CreateIndex(
                name: "IX_TripPhases_TenantId",
                table: "TripPhases",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TripPhases_TripId",
                table: "TripPhases",
                column: "TripId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_TripPhases_TripPhaseId",
                table: "Expenses",
                column: "TripPhaseId",
                principalTable: "TripPhases",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_TripPhases_TripPhaseId",
                table: "Expenses");

            migrationBuilder.DropTable(
                name: "TripPayments");

            migrationBuilder.DropTable(
                name: "TripPhases");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_TripPhaseId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "AuthorizationExpiry",
                table: "Trucks");

            migrationBuilder.DropColumn(
                name: "PUCExpiry",
                table: "Trucks");

            migrationBuilder.DropColumn(
                name: "NetWeight",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "TareWeight",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "TripPhaseId",
                table: "Expenses");

            migrationBuilder.RenameColumn(
                name: "AgentName",
                table: "Trips",
                newName: "ClientName");
        }
    }
}
