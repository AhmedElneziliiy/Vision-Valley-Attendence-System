using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreProject.Migrations
{
    /// <inheritdoc />
    public partial class AddLampAccessRequestSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LampAccessRequests",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LampID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RespondedByUserID = table.Column<int>(type: "int", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResponseNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TimeoutAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsAutoClosed = table.Column<bool>(type: "bit", nullable: false),
                    AutoClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LampAccessRequests", x => x.ID);
                    table.ForeignKey(
                        name: "FK_LampAccessRequests_Lamps_LampID",
                        column: x => x.LampID,
                        principalTable: "Lamps",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_LampAccessRequests_Users_RespondedByUserID",
                        column: x => x.RespondedByUserID,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LampAccessRequests_Users_UserID",
                        column: x => x.UserID,
                        principalSchema: "security",
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LampAccessRequests_ApprovedUntil",
                table: "LampAccessRequests",
                column: "ApprovedUntil",
                filter: "[Status] = 'Approved' AND [IsAutoClosed] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LampAccessRequests_LampID_RequestedAt",
                table: "LampAccessRequests",
                columns: new[] { "LampID", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LampAccessRequests_RespondedByUserID",
                table: "LampAccessRequests",
                column: "RespondedByUserID");

            migrationBuilder.CreateIndex(
                name: "IX_LampAccessRequests_Status",
                table: "LampAccessRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_LampAccessRequests_TimeoutAt",
                table: "LampAccessRequests",
                column: "TimeoutAt",
                filter: "[Status] = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_LampAccessRequests_UserID_RequestedAt",
                table: "LampAccessRequests",
                columns: new[] { "UserID", "RequestedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LampAccessRequests");
        }
    }
}
