using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PredictiveMaintenance.Data.Migrations
{
    /// <inheritdoc />
    public partial class CompositeAssetSensorKeys : Migration
    {
        // Hand-written because SQL Server can't ALTER COLUMN to remove the
        // IDENTITY property — affected columns are dropped and recreated.
        // Data is wiped first (user opted for a clean slate).
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM [Readings];
                DELETE FROM [Tickets];
                DELETE FROM [Thresholds];
                DELETE FROM [Sensors];
                DELETE FROM [Assets];
                DELETE FROM [Users];
            ");

            // Drop existing FKs and indexes that reference columns we're
            // about to change.
            migrationBuilder.Sql(@"
                ALTER TABLE [Readings]   DROP CONSTRAINT [FK_Readings_Sensors_SensorId];
                ALTER TABLE [Sensors]    DROP CONSTRAINT [FK_Sensors_Assets_AssetId];
                ALTER TABLE [Thresholds] DROP CONSTRAINT [FK_Thresholds_Assets_AssetId];
                ALTER TABLE [Tickets]    DROP CONSTRAINT [FK_Tickets_Assets_AssetId];

                DROP INDEX [IX_Tickets_AssetId]    ON [Tickets];
                DROP INDEX [IX_Thresholds_AssetId] ON [Thresholds];
                DROP INDEX [IX_Sensors_AssetId]    ON [Sensors];
                DROP INDEX [IX_Readings_SensorId]  ON [Readings];
                DROP INDEX [IX_Assets_UserId]      ON [Assets];

                ALTER TABLE [Assets]     DROP CONSTRAINT [PK_Assets];
                ALTER TABLE [Sensors]    DROP CONSTRAINT [PK_Sensors];
                ALTER TABLE [Thresholds] DROP CONSTRAINT [PK_Thresholds];
            ");

            // Some columns carry auto-named DEFAULT constraints (e.g.
            // DF__Assets__Number__...). DROP COLUMN fails while a default
            // constraint references the column, so drop them by lookup
            // first.
            migrationBuilder.Sql(@"
                DECLARE @sql NVARCHAR(MAX) = N'';
                SELECT @sql = @sql + 'ALTER TABLE [' + OBJECT_NAME(parent_object_id) +
                       '] DROP CONSTRAINT [' + name + ']; '
                FROM sys.default_constraints
                WHERE parent_object_id IN (
                    OBJECT_ID('Assets'), OBJECT_ID('Sensors'), OBJECT_ID('Thresholds')
                );
                IF LEN(@sql) > 0 EXEC sp_executesql @sql;
            ");

            // Drop & recreate IDENTITY columns to remove the IDENTITY
            // property (SQL Server has no in-place way to do this).
            migrationBuilder.Sql(@"
                ALTER TABLE [Assets]     DROP COLUMN [Id];
                ALTER TABLE [Assets]     DROP COLUMN [Number];
                ALTER TABLE [Sensors]    DROP COLUMN [Id];
                ALTER TABLE [Sensors]    DROP COLUMN [AssetId];
                ALTER TABLE [Thresholds] DROP COLUMN [Id];
                ALTER TABLE [Thresholds] DROP COLUMN [AssetId];
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE [Assets]     ADD [Id]      INT NOT NULL;
                ALTER TABLE [Sensors]    ADD [Id]      INT NOT NULL;
                ALTER TABLE [Sensors]    ADD [UserId]  INT NOT NULL;
                ALTER TABLE [Thresholds] ADD [AssetId] INT NOT NULL;
                ALTER TABLE [Thresholds] ADD [UserId]  INT NOT NULL;
                ALTER TABLE [Tickets]    ADD [UserId]  INT NOT NULL CONSTRAINT DF_Tickets_UserId DEFAULT 0;
                ALTER TABLE [Readings]   ADD [UserId]  INT NOT NULL CONSTRAINT DF_Readings_UserId DEFAULT 0;
            ");

            // Drop the temp defaults that let us add the column on a
            // (notionally empty) table.
            migrationBuilder.Sql(@"
                ALTER TABLE [Tickets]  DROP CONSTRAINT [DF_Tickets_UserId];
                ALTER TABLE [Readings] DROP CONSTRAINT [DF_Readings_UserId];
            ");

            // Composite PKs.
            migrationBuilder.Sql(@"
                ALTER TABLE [Assets]     ADD CONSTRAINT [PK_Assets]
                    PRIMARY KEY ([UserId], [Id]);
                ALTER TABLE [Sensors]    ADD CONSTRAINT [PK_Sensors]
                    PRIMARY KEY ([UserId], [Id]);
                ALTER TABLE [Thresholds] ADD CONSTRAINT [PK_Thresholds]
                    PRIMARY KEY ([UserId], [AssetId]);
            ");

            // Indexes on FK columns (Tickets/Readings have surrogate PKs).
            migrationBuilder.Sql(@"
                CREATE INDEX [IX_Tickets_UserId_AssetId]    ON [Tickets]  ([UserId], [AssetId]);
                CREATE INDEX [IX_Readings_UserId_SensorId]  ON [Readings] ([UserId], [SensorId]);
            ");

            // Composite FKs. Sensors→Assets and Thresholds→Assets and
            // Tickets→Assets all cascade from one direction (Assets);
            // Readings→Sensors cascades from Sensors. No converging paths.
            migrationBuilder.Sql(@"
                ALTER TABLE [Sensors] ADD CONSTRAINT [FK_Sensors_Assets_UserId_Id]
                    FOREIGN KEY ([UserId], [Id])
                    REFERENCES [Assets] ([UserId], [Id])
                    ON DELETE CASCADE;

                ALTER TABLE [Thresholds] ADD CONSTRAINT [FK_Thresholds_Assets_UserId_AssetId]
                    FOREIGN KEY ([UserId], [AssetId])
                    REFERENCES [Assets] ([UserId], [Id])
                    ON DELETE CASCADE;

                ALTER TABLE [Tickets] ADD CONSTRAINT [FK_Tickets_Assets_UserId_AssetId]
                    FOREIGN KEY ([UserId], [AssetId])
                    REFERENCES [Assets] ([UserId], [Id])
                    ON DELETE CASCADE;

                ALTER TABLE [Readings] ADD CONSTRAINT [FK_Readings_Sensors_UserId_SensorId]
                    FOREIGN KEY ([UserId], [SensorId])
                    REFERENCES [Sensors] ([UserId], [Id])
                    ON DELETE CASCADE;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down is left empty — this migration is destructive (wipes
            // data) and a clean rollback isn't meaningful. If you really
            // need to roll back, restore from a backup.
            migrationBuilder.Sql("-- intentionally empty");
        }
    }
}
