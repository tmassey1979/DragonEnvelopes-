using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DragonEnvelopes.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIntegrationOutboxMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "integration_outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FamilyId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RoutingKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EventName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SchemaVersion = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SourceService = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CausationId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastError = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DispatchedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_integration_outbox_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_integration_outbox_messages_families_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "families",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_integration_outbox_messages_DispatchedAtUtc_NextAttemptAtUt~",
                table: "integration_outbox_messages",
                columns: new[] { "DispatchedAtUtc", "NextAttemptAtUtc", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_integration_outbox_messages_EventId",
                table: "integration_outbox_messages",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_integration_outbox_messages_FamilyId_CreatedAtUtc",
                table: "integration_outbox_messages",
                columns: new[] { "FamilyId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "integration_outbox_messages");
        }
    }
}
