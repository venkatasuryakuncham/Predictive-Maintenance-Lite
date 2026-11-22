using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PredictiveMaintenance.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropSensorType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SensorType",
                table: "Sensors");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SensorType",
                table: "Sensors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
