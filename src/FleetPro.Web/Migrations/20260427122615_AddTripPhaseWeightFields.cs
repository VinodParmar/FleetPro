using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FleetPro.Migrations
{
    /// <inheritdoc />
    public partial class AddTripPhaseWeightFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CargoWeightTons",
                table: "TripPhases",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NetWeight",
                table: "TripPhases",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TareWeight",
                table: "TripPhases",
                type: "decimal(10,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CargoWeightTons",
                table: "TripPhases");

            migrationBuilder.DropColumn(
                name: "NetWeight",
                table: "TripPhases");

            migrationBuilder.DropColumn(
                name: "TareWeight",
                table: "TripPhases");
        }
    }
}
