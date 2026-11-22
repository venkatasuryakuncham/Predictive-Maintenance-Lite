using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PredictiveMaintenance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Number",
                table: "Assets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Backfill: give every existing asset a per-user sequential
            // Number based on its current Id order. Idempotent — only runs
            // once per migration apply.
            migrationBuilder.Sql(@"
                WITH numbered AS (
                    SELECT [Id],
                           ROW_NUMBER() OVER (PARTITION BY [UserId] ORDER BY [Id]) AS rn
                    FROM [Assets]
                )
                UPDATE a
                SET a.[Number] = n.rn
                FROM [Assets] a
                INNER JOIN numbered n ON a.[Id] = n.[Id];
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Number",
                table: "Assets");
        }
    }
}
