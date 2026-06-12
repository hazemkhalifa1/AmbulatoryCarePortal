using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmbulatoryCarePortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeDepartmentCodeStringAndRemoveUserDept : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Departments_DepartmentId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_Departments_ClinicId_DepartmentCode",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_DepartmentId",
                table: "AspNetUsers");

            // 1. Add Code as nullable first
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Departments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // 2. Migrate existing integer DepartmentCode values to string Code
            migrationBuilder.Sql(@"
                UPDATE dbo.Departments
                SET Code = CASE DepartmentCode
                    WHEN 0 THEN 'LD'
                    WHEN 1 THEN 'PC'
                    WHEN 2 THEN 'LB'
                    WHEN 3 THEN 'RD'
                    WHEN 4 THEN 'DN'
                    WHEN 5 THEN 'MM'
                    WHEN 6 THEN 'MOI'
                    WHEN 7 THEN 'IPC'
                    WHEN 8 THEN 'FMS'
                    WHEN 9 THEN 'DPU'
                    WHEN 10 THEN 'DA'
                    WHEN 11 THEN 'DL'
                    ELSE CAST(DepartmentCode AS NVARCHAR(50))
                END
                WHERE Code IS NULL
            ");

            // 3. Drop old DepartmentCode column
            migrationBuilder.DropColumn(
                name: "DepartmentCode",
                table: "Departments");

            // 4. Drop DepartmentId from AspNetUsers
            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "AspNetUsers");

            // 5. Make Code non-nullable now that data is migrated
            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Departments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false);

            // 6. Create unique index on (ClinicId, Code)
            migrationBuilder.CreateIndex(
                name: "IX_Departments_ClinicId_Code",
                table: "Departments",
                columns: new[] { "ClinicId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Departments_ClinicId_Code",
                table: "Departments");

            // Convert Code back to DepartmentCode int
            migrationBuilder.AddColumn<int>(
                name: "DepartmentCode",
                table: "Departments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                UPDATE dbo.Departments
                SET DepartmentCode = CASE Code
                    WHEN 'LD' THEN 0
                    WHEN 'PC' THEN 1
                    WHEN 'LB' THEN 2
                    WHEN 'RD' THEN 3
                    WHEN 'DN' THEN 4
                    WHEN 'MM' THEN 5
                    WHEN 'MOI' THEN 6
                    WHEN 'IPC' THEN 7
                    WHEN 'FMS' THEN 8
                    WHEN 'DPU' THEN 9
                    WHEN 'DA' THEN 10
                    WHEN 'DL' THEN 11
                    ELSE 0
                END
            ");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Departments");

            migrationBuilder.AddColumn<int>(
                name: "DepartmentId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ClinicId_DepartmentCode",
                table: "Departments",
                columns: new[] { "ClinicId", "DepartmentCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_DepartmentId",
                table: "AspNetUsers",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Departments_DepartmentId",
                table: "AspNetUsers",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
