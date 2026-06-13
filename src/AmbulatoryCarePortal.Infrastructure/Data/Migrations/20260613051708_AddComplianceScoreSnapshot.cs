using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmbulatoryCarePortal.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddComplianceScoreSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PolicyDocuments_ClinicId_StandardCode",
                table: "PolicyDocuments");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ClinicId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_HrDocuments_HrStaffId",
                table: "HrDocuments");

            migrationBuilder.DropIndex(
                name: "IX_Forms_ClinicId",
                table: "Forms");

            migrationBuilder.DropIndex(
                name: "IX_DocumentTemplates_StandardCode",
                table: "DocumentTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Departments_ClinicId_Code",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Clinics_LicenseNumber",
                table: "Clinics");

            migrationBuilder.DropIndex(
                name: "IX_Clinics_Name",
                table: "Clinics");

            migrationBuilder.DropIndex(
                name: "IX_ClinicDocuments_ClinicId_DocumentTemplateId",
                table: "ClinicDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ChecklistRounds_ClinicId",
                table: "ChecklistRounds");

            migrationBuilder.CreateTable(
                name: "ComplianceScoreSnapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicId = table.Column<int>(type: "int", nullable: false),
                    OverallScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PolicyScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    KpiScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ChecklistScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    HrScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DocumentScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PolicyWeight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    KpiWeight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ChecklistWeight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    HrWeight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    DocumentWeight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceScoreSnapshot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceScoreSnapshot_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyDocuments_ClinicId_DocumentStatus_ExpiryDate",
                table: "PolicyDocuments",
                columns: new[] { "ClinicId", "DocumentStatus", "ExpiryDate" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyDocuments_ClinicId_StandardCode",
                table: "PolicyDocuments",
                columns: new[] { "ClinicId", "StandardCode" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ClinicId_IsRead_CreatedAt",
                table: "Notifications",
                columns: new[] { "ClinicId", "IsRead", "CreatedAt" },
                descending: new[] { false, false, true },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_HrDocuments_HrStaffId_ExpiryDate",
                table: "HrDocuments",
                columns: new[] { "HrStaffId", "ExpiryDate" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Forms_ClinicId_Category",
                table: "Forms",
                columns: new[] { "ClinicId", "Category" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTemplates_StandardCode",
                table: "DocumentTemplates",
                column: "StandardCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ClinicId_Code",
                table: "Departments",
                columns: new[] { "ClinicId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_LicenseNumber",
                table: "Clinics",
                column: "LicenseNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_Name",
                table: "Clinics",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicDocuments_ClinicId_DocumentTemplateId",
                table: "ClinicDocuments",
                columns: new[] { "ClinicId", "DocumentTemplateId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistRounds_ClinicId_ChecklistTemplateId_ExecutedAt",
                table: "ChecklistRounds",
                columns: new[] { "ClinicId", "ChecklistTemplateId", "ExecutedAt" },
                descending: new[] { false, false, true },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceScoreSnapshot_ClinicId_CalculatedAt",
                table: "ComplianceScoreSnapshot",
                columns: new[] { "ClinicId", "CalculatedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ComplianceScoreSnapshot");

            migrationBuilder.DropIndex(
                name: "IX_PolicyDocuments_ClinicId_DocumentStatus_ExpiryDate",
                table: "PolicyDocuments");

            migrationBuilder.DropIndex(
                name: "IX_PolicyDocuments_ClinicId_StandardCode",
                table: "PolicyDocuments");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ClinicId_IsRead_CreatedAt",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_HrDocuments_HrStaffId_ExpiryDate",
                table: "HrDocuments");

            migrationBuilder.DropIndex(
                name: "IX_Forms_ClinicId_Category",
                table: "Forms");

            migrationBuilder.DropIndex(
                name: "IX_DocumentTemplates_StandardCode",
                table: "DocumentTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Departments_ClinicId_Code",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Clinics_LicenseNumber",
                table: "Clinics");

            migrationBuilder.DropIndex(
                name: "IX_Clinics_Name",
                table: "Clinics");

            migrationBuilder.DropIndex(
                name: "IX_ClinicDocuments_ClinicId_DocumentTemplateId",
                table: "ClinicDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ChecklistRounds_ClinicId_ChecklistTemplateId_ExecutedAt",
                table: "ChecklistRounds");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyDocuments_ClinicId_StandardCode",
                table: "PolicyDocuments",
                columns: new[] { "ClinicId", "StandardCode" },
                unique: true,
                filter: "[StandardCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ClinicId",
                table: "Notifications",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_HrDocuments_HrStaffId",
                table: "HrDocuments",
                column: "HrStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Forms_ClinicId",
                table: "Forms",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTemplates_StandardCode",
                table: "DocumentTemplates",
                column: "StandardCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_ClinicId_Code",
                table: "Departments",
                columns: new[] { "ClinicId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_LicenseNumber",
                table: "Clinics",
                column: "LicenseNumber",
                unique: true,
                filter: "[LicenseNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_Name",
                table: "Clinics",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicDocuments_ClinicId_DocumentTemplateId",
                table: "ClinicDocuments",
                columns: new[] { "ClinicId", "DocumentTemplateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistRounds_ClinicId",
                table: "ChecklistRounds",
                column: "ClinicId");
        }
    }
}
