using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PredictiveMaintenance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMinThresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "RmsMin",
                table: "Thresholds",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TempMin",
                table: "Thresholds",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.UpdateData(
                table: "Thresholds",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "RmsMin", "TempMin" },
                values: new object[] { 0.5, 40.0 });

            migrationBuilder.UpdateData(
                table: "Thresholds",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "RmsMin", "TempMin" },
                values: new object[] { 0.29999999999999999, 35.0 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RmsMin",
                table: "Thresholds");

            migrationBuilder.DropColumn(
                name: "TempMin",
                table: "Thresholds");
        }
    }
}
