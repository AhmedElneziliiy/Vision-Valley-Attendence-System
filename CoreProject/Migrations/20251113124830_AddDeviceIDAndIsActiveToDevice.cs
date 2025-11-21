using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreProject.Migrations
{
    /// <inheritdoc />
    public partial class AddDeviceIDAndIsActiveToDevice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceID",
                table: "Devices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Devices",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceID",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Devices");
        }
    }
}
