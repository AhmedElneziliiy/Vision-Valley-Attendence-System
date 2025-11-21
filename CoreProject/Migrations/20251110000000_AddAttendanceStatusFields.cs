using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreProject.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Status column (enum stored as int)
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Attendances",
                type: "int",
                nullable: false,
                defaultValue: 0); // Default to Absent

            // Add MinutesLate column (nullable int)
            migrationBuilder.AddColumn<int>(
                name: "MinutesLate",
                table: "Attendances",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "MinutesLate",
                table: "Attendances");
        }
    }
}
