using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class FloofstationLoadoutMetadataCustomization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "color_override",
                table: "profile_loadout",
                type: "TEXT",
                maxLength: 9,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description_override",
                table: "profile_loadout",
                type: "TEXT",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name_override",
                table: "profile_loadout",
                type: "TEXT",
                maxLength: 96,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "color_override",
                table: "profile_loadout");

            migrationBuilder.DropColumn(
                name: "description_override",
                table: "profile_loadout");

            migrationBuilder.DropColumn(
                name: "name_override",
                table: "profile_loadout");
        }
    }
}
