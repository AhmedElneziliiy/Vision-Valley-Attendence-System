using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreProject.Migrations
{
    /// <inheritdoc />
    public partial class AddLampsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Lamps",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeviceID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BranchID = table.Column<int>(type: "int", nullable: false),
                    TimetableID = table.Column<int>(type: "int", nullable: false),
                    CurrentState = table.Column<int>(type: "int", nullable: false),
                    LastStateChange = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ManualOverride = table.Column<bool>(type: "bit", nullable: false),
                    ManualOverrideState = table.Column<int>(type: "int", nullable: true),
                    IsConnected = table.Column<bool>(type: "bit", nullable: false),
                    LastConnectionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastDisconnectionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConnectionID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lamps", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Lamps_Branches_BranchID",
                        column: x => x.BranchID,
                        principalTable: "Branches",
                        principalColumn: "ID");
                    table.ForeignKey(
                        name: "FK_Lamps_Timetables_TimetableID",
                        column: x => x.TimetableID,
                        principalTable: "Timetables",
                        principalColumn: "ID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Lamps_BranchID",
                table: "Lamps",
                column: "BranchID");

            migrationBuilder.CreateIndex(
                name: "IX_Lamps_DeviceID",
                table: "Lamps",
                column: "DeviceID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lamps_TimetableID",
                table: "Lamps",
                column: "TimetableID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Lamps");
        }
    }
}
