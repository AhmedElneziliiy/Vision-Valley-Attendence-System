using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreProject.Migrations
{
    /// <inheritdoc />
    public partial class AddFaceVerificationFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "FaceEmbedding",
                schema: "security",
                table: "Users",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FaceEnrolledAt",
                schema: "security",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFaceVerificationEnabled",
                schema: "security",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFaceVerificationEnabled",
                table: "Branches",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaceEmbedding",
                schema: "security",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "FaceEnrolledAt",
                schema: "security",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsFaceVerificationEnabled",
                schema: "security",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsFaceVerificationEnabled",
                table: "Branches");
        }
    }
}
