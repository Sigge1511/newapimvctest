using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api_carrental.Migrations
{
    /// <inheritdoc />
    public partial class addedRTfieldstousers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RefreshTokenValue",
                table: "AspNetUsers", // <-- Viktigt att det pekar på AspNetUsers
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefreshTokenExpiryTime",
                table: "AspNetUsers", // <-- Viktigt att det pekar på AspNetUsers
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
