using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmbulatoryCarePortal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedStandardToClinic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedStandards",
                table: "Clinics",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedStandards",
                table: "Clinics");
        }
    }
}
