using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmbulatoryCarePortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentTemplatesSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StandardCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DepartmentCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TemplateFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClinicDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicId = table.Column<int>(type: "int", nullable: false),
                    DocumentTemplateId = table.Column<int>(type: "int", nullable: false),
                    DocumentStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OfficialPdfPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicDocuments_Clinics_ClinicId",
                        column: x => x.ClinicId,
                        principalTable: "Clinics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicDocuments_DocumentTemplates_DocumentTemplateId",
                        column: x => x.DocumentTemplateId,
                        principalTable: "DocumentTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ClinicDocumentAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicDocumentId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UploadedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicDocumentAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicDocumentAttachments_AspNetUsers_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicDocumentAttachments_ClinicDocuments_ClinicDocumentId",
                        column: x => x.ClinicDocumentId,
                        principalTable: "ClinicDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicDocumentAttachments_ClinicDocumentId",
                table: "ClinicDocumentAttachments",
                column: "ClinicDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicDocumentAttachments_UploadedByUserId",
                table: "ClinicDocumentAttachments",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicDocuments_ClinicId_DocumentTemplateId",
                table: "ClinicDocuments",
                columns: new[] { "ClinicId", "DocumentTemplateId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicDocuments_DocumentTemplateId",
                table: "ClinicDocuments",
                column: "DocumentTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentTemplates_StandardCode",
                table: "DocumentTemplates",
                column: "StandardCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClinicDocumentAttachments");

            migrationBuilder.DropTable(
                name: "ClinicDocuments");

            migrationBuilder.DropTable(
                name: "DocumentTemplates");
        }
    }
}
