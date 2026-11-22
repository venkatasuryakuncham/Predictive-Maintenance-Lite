using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PredictiveMaintenance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersAndUserSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Wipe existing data — user opted for a fresh start. Order matters
            // because of FK constraints: tickets/readings/thresholds/sensors
            // before assets.
            migrationBuilder.Sql("DELETE FROM [Tickets];");
            migrationBuilder.Sql("DELETE FROM [Readings];");
            migrationBuilder.Sql("DELETE FROM [Thresholds];");
            migrationBuilder.Sql("DELETE FROM [Sensors];");
            migrationBuilder.Sql("DELETE FROM [Assets];");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Assets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordSalt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecurityQuestion1Id = table.Column<int>(type: "int", nullable: false),
                    SecurityAnswer1Hash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecurityAnswer1Salt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecurityQuestion2Id = table.Column<int>(type: "int", nullable: false),
                    SecurityAnswer2Hash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecurityAnswer2Salt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Assets_UserId",
                table: "Assets",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_UserId",
                table: "Users",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_Token",
                table: "UserSessions",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_Users_UserId",
                table: "Assets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_Users_UserId",
                table: "Assets");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Assets_UserId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Assets");

            migrationBuilder.InsertData(
                table: "Assets",
                columns: new[] { "Id", "Description", "Location", "Name" },
                values: new object[,]
                {
                    { 1, "Main coolant pump", "Plant 1", "Pump-A1" },
                    { 2, "Conveyor belt motor", "Plant 2", "Motor-B2" }
                });

            migrationBuilder.InsertData(
                table: "Sensors",
                columns: new[] { "Id", "AssetId", "SensorName", "SensorType" },
                values: new object[,]
                {
                    { 1, 1, "VibSensor-01", "Vibration" },
                    { 2, 2, "TempSensor-01", "Temperature" }
                });

            migrationBuilder.InsertData(
                table: "Thresholds",
                columns: new[] { "Id", "AssetId", "RmsMax", "RmsMin", "TempMax", "TempMin" },
                values: new object[,]
                {
                    { 1, 1, 3.0, 0.5, 80.0, 40.0 },
                    { 2, 2, 2.5, 0.29999999999999999, 75.0, 35.0 }
                });
        }
    }
}
