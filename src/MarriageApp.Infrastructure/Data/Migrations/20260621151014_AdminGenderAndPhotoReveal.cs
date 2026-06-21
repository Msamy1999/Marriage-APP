using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarriageApp.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdminGenderAndPhotoReveal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PhotosRevealedAt",
                table: "Matches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotosRevealedByAdminId",
                table: "Matches",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PhotosRevealedToGroom",
                table: "Matches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "AspNetUsers",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotosRevealedAt",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "PhotosRevealedByAdminId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "PhotosRevealedToGroom",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "AspNetUsers");
        }
    }
}
