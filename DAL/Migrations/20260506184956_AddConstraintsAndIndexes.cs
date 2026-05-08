using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddConstraintsAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Новые индексы ───────────────────────────────────────────────

            migrationBuilder.CreateIndex(
                name: "IX_Inspections_InspectorId",
                table: "Inspections",
                column: "InspectorId");

            migrationBuilder.CreateIndex(
                name: "IX_RepairRequests_RequestedById",
                table: "RepairRequests",
                column: "RequestedById");

            migrationBuilder.CreateIndex(
                name: "IX_StudentPoints_Source",
                table: "StudentPoints",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventParticipants_EventId",
                table: "EventParticipants",
                column: "EventId");

            // ── CHECK constraints ───────────────────────────────────────────

            migrationBuilder.Sql(
                "ALTER TABLE [Inspections] ADD CONSTRAINT [CK_Inspections_Score] CHECK ([Score] >= 0 AND [Score] <= 10)");

            migrationBuilder.Sql(
                "ALTER TABLE [RepairRequests] ADD CONSTRAINT [CK_RepairRequests_Status] CHECK ([Status] IN ('Pending', 'InProgress', 'Completed', 'Cancelled'))");

            migrationBuilder.Sql(
                "ALTER TABLE [RepairRequests] ADD CONSTRAINT [CK_RepairRequests_Priority] CHECK ([Priority] IN ('Low', 'Normal', 'High'))");

            migrationBuilder.Sql(
                "ALTER TABLE [StudentPoints] ADD CONSTRAINT [CK_StudentPoints_PointsType] CHECK ([PointsType] IN ('Award', 'Penalty'))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Inspections_InspectorId",      table: "Inspections");
            migrationBuilder.DropIndex(name: "IX_RepairRequests_RequestedById",  table: "RepairRequests");
            migrationBuilder.DropIndex(name: "IX_StudentPoints_Source",          table: "StudentPoints");
            migrationBuilder.DropIndex(name: "IX_EventParticipants_EventId",     table: "EventParticipants");

            migrationBuilder.Sql("ALTER TABLE [Inspections]     DROP CONSTRAINT [CK_Inspections_Score]");
            migrationBuilder.Sql("ALTER TABLE [RepairRequests]  DROP CONSTRAINT [CK_RepairRequests_Status]");
            migrationBuilder.Sql("ALTER TABLE [RepairRequests]  DROP CONSTRAINT [CK_RepairRequests_Priority]");
            migrationBuilder.Sql("ALTER TABLE [StudentPoints]   DROP CONSTRAINT [CK_StudentPoints_PointsType]");
        }
    }
}
