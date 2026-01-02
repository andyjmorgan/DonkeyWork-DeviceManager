using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DonkeyWork.DeviceManager.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOSQueryAndAuditTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeviceAuditLogs",
                schema: "DeviceManager",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InitiatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Details = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Result = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceAuditLogs_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalSchema: "DeviceManager",
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OSQueryHistory",
                schema: "DeviceManager",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Query = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ExecutionCount = table.Column<int>(type: "integer", nullable: false),
                    LastExecutedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OSQueryHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OSQueryExecutions",
                schema: "DeviceManager",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QueryHistoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Query = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ExecutedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DeviceCount = table.Column<int>(type: "integer", nullable: false),
                    SuccessCount = table.Column<int>(type: "integer", nullable: false),
                    FailureCount = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OSQueryExecutions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OSQueryExecutions_OSQueryHistory_QueryHistoryId",
                        column: x => x.QueryHistoryId,
                        principalSchema: "DeviceManager",
                        principalTable: "OSQueryHistory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OSQueryExecutionResults",
                schema: "DeviceManager",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RawJson = table.Column<string>(type: "TEXT", nullable: true),
                    ExecutionTimeMs = table.Column<int>(type: "integer", nullable: false),
                    RowCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OSQueryExecutionResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OSQueryExecutionResults_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalSchema: "DeviceManager",
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OSQueryExecutionResults_OSQueryExecutions_ExecutionId",
                        column: x => x.ExecutionId,
                        principalSchema: "DeviceManager",
                        principalTable: "OSQueryExecutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAuditLogs_DeviceId_Timestamp",
                schema: "DeviceManager",
                table: "DeviceAuditLogs",
                columns: new[] { "DeviceId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceAuditLogs_TenantId",
                schema: "DeviceManager",
                table: "DeviceAuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_OSQueryExecutionResults_DeviceId",
                schema: "DeviceManager",
                table: "OSQueryExecutionResults",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_OSQueryExecutionResults_ExecutionId",
                schema: "DeviceManager",
                table: "OSQueryExecutionResults",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_OSQueryExecutions_ExecutedAt",
                schema: "DeviceManager",
                table: "OSQueryExecutions",
                column: "ExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OSQueryExecutions_QueryHistoryId",
                schema: "DeviceManager",
                table: "OSQueryExecutions",
                column: "QueryHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_OSQueryHistory_UserId",
                schema: "DeviceManager",
                table: "OSQueryHistory",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceAuditLogs",
                schema: "DeviceManager");

            migrationBuilder.DropTable(
                name: "OSQueryExecutionResults",
                schema: "DeviceManager");

            migrationBuilder.DropTable(
                name: "OSQueryExecutions",
                schema: "DeviceManager");

            migrationBuilder.DropTable(
                name: "OSQueryHistory",
                schema: "DeviceManager");
        }
    }
}
