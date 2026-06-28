using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AmbulatoryCarePortal.Application.Services;

public class ReportingService : IReportingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(IUnitOfWork unitOfWork, ILogger<ReportingService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<byte[]> GenerateComplianceReportAsync(int clinicId, DateTime startDate, DateTime endDate, string format)
    {
        var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(clinicId)
            ?? throw new InvalidOperationException("Clinic not found");

        var kpis = await _unitOfWork.Repository<KPI>().FindAsync(k => k.ClinicId == clinicId);
        var rounds = await _unitOfWork.Repository<ChecklistRound>().FindAsync(
            r => r.ClinicId == clinicId && r.ExecutedAt >= startDate && r.ExecutedAt <= endDate
        );
        var staff = await _unitOfWork.Repository<HrStaff>().FindAsync(s => s.ClinicId == clinicId);

        return format.ToLower() switch
        {
            "pdf" => GenerateCompliancePdf(clinic, kpis, rounds, staff, startDate, endDate),
            "xlsx" => GenerateComplianceExcel(clinic, kpis, rounds, staff, startDate, endDate),
            _ => GenerateComplianceExcel(clinic, kpis, rounds, staff, startDate, endDate)
        };
    }

    public async Task<byte[]> GenerateKPIReportAsync(int clinicId, DateTime startDate, DateTime endDate, string format)
    {
        var kpis = await _unitOfWork.Repository<KPI>().FindAsync(k => k.ClinicId == clinicId);
        var entries = await _unitOfWork.Repository<KPIEntry>().FindAsync(
            e => kpis.Select(k => k.Id).Contains(e.KPIId) && e.PeriodYear >= startDate.Year && e.PeriodYear <= endDate.Year
        );

        return format.ToLower() switch
        {
            "pdf" => GenerateKPIPdf(kpis.ToList(), entries.ToList(), startDate, endDate),
            "xlsx" => GenerateKPIExcel(kpis.ToList(), entries.ToList(), startDate, endDate),
            _ => GenerateKPIExcel(kpis.ToList(), entries.ToList(), startDate, endDate)
        };
    }

    public async Task<byte[]> GenerateAuditReportAsync(int clinicId, DateTime startDate, DateTime endDate, string format)
    {
        var auditLogs = await _unitOfWork.Repository<AuditTrail>().FindAsync(
            a => a.ClinicId == clinicId && a.ActionDate >= startDate && a.ActionDate <= endDate
        );

        return format.ToLower() switch
        {
            "pdf" => GenerateAuditPdf(auditLogs.ToList(), startDate, endDate),
            "xlsx" => GenerateAuditExcel(auditLogs.ToList(), startDate, endDate),
            _ => GenerateAuditExcel(auditLogs.ToList(), startDate, endDate)
        };
    }

    public async Task<byte[]> GenerateChecklistReportAsync(int clinicId, DateTime startDate, DateTime endDate, string format)
    {
        var rounds = await _unitOfWork.Repository<ChecklistRound>().FindAsync(
            r => r.ClinicId == clinicId && r.ExecutedAt >= startDate && r.ExecutedAt <= endDate
        );

        return format.ToLower() switch
        {
            "pdf" => GenerateChecklistPdf(rounds.ToList(), startDate, endDate),
            "xlsx" => GenerateChecklistExcel(rounds.ToList(), startDate, endDate),
            _ => GenerateChecklistExcel(rounds.ToList(), startDate, endDate)
        };
    }

    public async Task<byte[]> GenerateHRReportAsync(int clinicId, DateTime startDate, DateTime endDate, string format)
    {
        var staff = await _unitOfWork.Repository<HrStaff>().FindAsync(
            s => s.ClinicId == clinicId && s.CreatedAt >= startDate && s.CreatedAt <= endDate
        );
        var allStaffIds = staff.Select(s => s.Id).ToList();
        var documents = await _unitOfWork.Repository<HrDocument>().FindAsync(
            d => allStaffIds.Contains(d.HrStaffId)
        );

        return format.ToLower() switch
        {
            "pdf" => GenerateHRPdf(staff.ToList(), documents.ToList(), startDate, endDate),
            "xlsx" => GenerateHRExcel(staff.ToList(), documents.ToList(), startDate, endDate),
            _ => GenerateHRExcel(staff.ToList(), documents.ToList(), startDate, endDate)
        };
    }

    public Task<List<string>> GetAvailableReportsAsync(string userRole)
    {
        var availableReports = userRole switch
        {
            "SuperAdmin" => new List<string>
            {
                "Compliance Report", "KPI Report", "Audit Report",
                "Checklist Report", "HR Report", "System Report"
            },
            "ClinicAdmin" => new List<string>
            {
                "Compliance Report", "KPI Report", "Checklist Report", "HR Report"
            },
            _ => new List<string>()
        };

        return Task.FromResult(availableReports);
    }

    private byte[] GenerateCompliancePdf(Clinic clinic, IEnumerable<KPI> kpis, IEnumerable<ChecklistRound> rounds, IEnumerable<HrStaff> staff, DateTime start, DateTime end)
    {
        var kpiList = kpis.ToList();
        var roundList = rounds.ToList();
        var staffList = staff.ToList();

        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                page.Header().AlignCenter().Text($"Compliance Report").FontSize(20).Bold();
                page.Header().AlignCenter().Text($"{clinic.Name}").FontSize(14);
                page.Header().AlignCenter().Text($"{start:dd/MM/yyyy} - {end:dd/MM/yyyy}").FontSize(10).FontColor(Colors.Grey.Medium);

                page.Content().Column(col =>
                {
                    col.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Blue.Medium);

                    col.Item().PaddingTop(15).Text("Executive Summary").FontSize(14).Bold();
                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Metric").FontColor(Colors.White).Bold();
                            h.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Value").FontColor(Colors.White).Bold();
                        });

                        AddRow(table, "Compliance Score", $"{clinic.ComplianceScore:F1}%");
                        AddRow(table, "Total KPIs", kpiList.Count.ToString());
                        AddRow(table, "Checklist Rounds", roundList.Count.ToString());
                        AddRow(table, "Total Staff", staffList.Count.ToString());
                        AddRow(table, "Active Staff", staffList.Count(s => s.IsActive).ToString());
                    });

                    col.Item().PaddingTop(20).Text($"Generated: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC").FontSize(8).FontColor(Colors.Grey.Medium);
                    col.Item().Text("CBAHI Ambulatory Care Portal").FontSize(8).FontColor(Colors.Grey.Medium);
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.CurrentPageNumber().FontSize(10);
                    text.Span(" / ").FontSize(10);
                    text.TotalPages().FontSize(10);
                });
            });
        }).GeneratePdf();
    }

    private byte[] GenerateComplianceExcel(Clinic clinic, IEnumerable<KPI> kpis, IEnumerable<ChecklistRound> rounds, IEnumerable<HrStaff> staff, DateTime start, DateTime end)
    {
        using var workbook = new XLWorkbook();

        var summary = workbook.Worksheets.Add("Summary");
        summary.Cell("A1").Value = "Compliance Report";
        summary.Cell("A2").Value = clinic.Name;
        summary.Cell("A3").Value = $"{start:dd/MM/yyyy} - {end:dd/MM/yyyy}";
        summary.Cell("A4").Value = $"Generated: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC";

        summary.Cell("A6").Value = "Metric";
        summary.Cell("B6").Value = "Value";
        summary.Range("A6:B6").Style.Fill.BackgroundColor = XLColor.Blue;
        summary.Range("A6:B6").Style.Font.FontColor = XLColor.White;
        summary.Range("A6:B6").Style.Font.Bold = true;

        summary.Cell("A7").Value = "Compliance Score";
        summary.Cell("B7").Value = $"{clinic.ComplianceScore:F1}%";
        summary.Cell("A8").Value = "Total KPIs";
        summary.Cell("B8").Value = kpis.Count();
        summary.Cell("A9").Value = "Checklist Rounds";
        summary.Cell("B9").Value = rounds.Count();
        summary.Cell("A10").Value = "Total Staff";
        summary.Cell("B10").Value = staff.Count();
        summary.Cell("A11").Value = "Active Staff";
        summary.Cell("B11").Value = staff.Count(s => s.IsActive);
        summary.Columns().AdjustToContents();

        return GetBytes(workbook);
    }

    private byte[] GenerateKPIPdf(List<KPI> kpis, List<KPIEntry> entries, DateTime start, DateTime end)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                page.Header().AlignCenter().Text("KPI Report").FontSize(20).Bold();
                page.Header().AlignCenter().Text($"{start:dd/MM/yyyy} - {end:dd/MM/yyyy}").FontSize(10).FontColor(Colors.Grey.Medium);

                page.Content().Column(col =>
                {
                    col.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Green.Medium);
                    col.Item().PaddingTop(10).Text($"Total KPIs: {kpis.Count}").FontSize(12);

                    foreach (var kpi in kpis)
                    {
                        col.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                            });

                            table.Header(h =>
                            {
                                h.Cell().Background(Colors.Green.Darken2).Padding(3).Text("Name").FontColor(Colors.White).Bold();
                                h.Cell().Background(Colors.Green.Darken2).Padding(3).Text("Target").FontColor(Colors.White).Bold();
                                h.Cell().Background(Colors.Green.Darken2).Padding(3).Text("Period").FontColor(Colors.White).Bold();
                                h.Cell().Background(Colors.Green.Darken2).Padding(3).Text("Actual").FontColor(Colors.White).Bold();
                            });

                            var kpiEntries = entries.Where(e => e.KPIId == kpi.Id).ToList();
                            if (kpiEntries.Count == 0)
                            {
                                table.Cell().Padding(2).Text(kpi.Name ?? "-").FontSize(9);
                                table.Cell().Padding(2).Text($"{kpi.TargetValue:F2}").FontSize(9);
                                table.Cell().Padding(2).Text("-").FontSize(9);
                                table.Cell().Padding(2).Text("No data").FontSize(9).FontColor(Colors.Red.Medium);
                            }
                            else
                            {
                                foreach (var entry in kpiEntries)
                                {
                                    table.Cell().Padding(2).Text(kpi.Name ?? "-").FontSize(9);
                                    table.Cell().Padding(2).Text($"{kpi.TargetValue:F2}").FontSize(9);
                                    table.Cell().Padding(2).Text($"{entry.PeriodMonth}/{entry.PeriodYear}").FontSize(9);
                                    table.Cell().Padding(2).Text($"{entry.ActualValue:F2}").FontSize(9);
                                }
                            }
                        });
                    }

                    col.Item().PaddingTop(20).Text($"Generated: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC").FontSize(8).FontColor(Colors.Grey.Medium);
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private byte[] GenerateKPIExcel(List<KPI> kpis, List<KPIEntry> entries, DateTime start, DateTime end)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("KPIs");

        ws.Cell("A1").Value = "KPI Name";
        ws.Cell("B1").Value = "Target";
        ws.Cell("C1").Value = "Period";
        ws.Cell("D1").Value = "Actual";
        ws.Cell("E1").Value = "Achievement %";
        ws.Range("A1:E1").Style.Fill.BackgroundColor = XLColor.Green;
        ws.Range("A1:E1").Style.Font.FontColor = XLColor.White;
        ws.Range("A1:E1").Style.Font.Bold = true;

        var row = 2;
        foreach (var kpi in kpis)
        {
            var kpiEntries = entries.Where(e => e.KPIId == kpi.Id).ToList();
            if (kpiEntries.Count == 0)
            {
                ws.Cell(row, 1).Value = kpi.Name ?? "";
                ws.Cell(row, 2).Value = kpi.TargetValue;
                ws.Cell(row, 3).Value = "";
                ws.Cell(row, 4).Value = "";
                ws.Cell(row, 5).Value = "";
                row++;
            }
            else
            {
                foreach (var entry in kpiEntries)
                {
                    ws.Cell(row, 1).Value = kpi.Name ?? "";
                    ws.Cell(row, 2).Value = kpi.TargetValue;
                    ws.Cell(row, 3).Value = $"{entry.PeriodMonth}/{entry.PeriodYear}";
                    ws.Cell(row, 4).Value = entry.ActualValue;
                    ws.Cell(row, 5).Value = kpi.TargetValue > 0 ? $"{entry.ActualValue / kpi.TargetValue * 100:F1}" : "0";
                    row++;
                }
            }
        }

        ws.Columns().AdjustToContents();
        return GetBytes(workbook);
    }

    private byte[] GenerateAuditPdf(List<AuditTrail> logs, DateTime start, DateTime end)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);

                page.Header().AlignCenter().Text("Audit Trail Report").FontSize(20).Bold();
                page.Header().AlignCenter().Text($"{start:dd/MM/yyyy} - {end:dd/MM/yyyy}").FontSize(10).FontColor(Colors.Grey.Medium);

                page.Content().Column(col =>
                {
                    col.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Orange.Medium);
                    col.Item().PaddingTop(5).Text($"Total Actions: {logs.Count}").FontSize(12);

                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Orange.Darken2).Padding(3).Text("Date").FontColor(Colors.White).Bold();
                            h.Cell().Background(Colors.Orange.Darken2).Padding(3).Text("Description").FontColor(Colors.White).Bold();
                            h.Cell().Background(Colors.Orange.Darken2).Padding(3).Text("Action").FontColor(Colors.White).Bold();
                            h.Cell().Background(Colors.Orange.Darken2).Padding(3).Text("Target").FontColor(Colors.White).Bold();
                            h.Cell().Background(Colors.Orange.Darken2).Padding(3).Text("User").FontColor(Colors.White).Bold();
                        });

                        foreach (var log in logs.Take(500))
                        {
                            table.Cell().Padding(2).Text(log.ActionDate.ToString("dd/MM/yyyy HH:mm")).FontSize(8);
                            table.Cell().Padding(2).Text(log.Description ?? "-").FontSize(8);
                            table.Cell().Padding(2).Text(log.ActionType.ToString()).FontSize(8);
                            table.Cell().Padding(2).Text(log.TargetObjectType).FontSize(8);
                            table.Cell().Padding(2).Text(log.UserId ?? "system").FontSize(8);
                        }
                    });

                    col.Item().PaddingTop(10).Text($"* Showing up to 500 entries").FontSize(8).FontColor(Colors.Grey.Medium);
                });

                page.Footer().AlignCenter().Text(x => { x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
            });
        }).GeneratePdf();
    }

    private byte[] GenerateAuditExcel(List<AuditTrail> logs, DateTime start, DateTime end)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Audit Logs");

        ws.Cell("A1").Value = "Date";
        ws.Cell("B1").Value = "Description";
        ws.Cell("C1").Value = "Action";
        ws.Cell("D1").Value = "Target Type";
        ws.Cell("E1").Value = "Target ID";
        ws.Cell("F1").Value = "User";
        ws.Cell("G1").Value = "IP Address";
        ws.Range("A1:G1").Style.Fill.BackgroundColor = XLColor.Orange;
        ws.Range("A1:G1").Style.Font.FontColor = XLColor.White;
        ws.Range("A1:G1").Style.Font.Bold = true;

        var row = 2;
        foreach (var log in logs)
        {
            ws.Cell(row, 1).Value = log.ActionDate.ToString("yyyy-MM-dd HH:mm");
            ws.Cell(row, 2).Value = log.Description ?? "";
            ws.Cell(row, 3).Value = log.ActionType.ToString();
            ws.Cell(row, 4).Value = log.TargetObjectType;
            ws.Cell(row, 5).Value = log.TargetObjectId;
            ws.Cell(row, 6).Value = log.UserId ?? "";
            ws.Cell(row, 7).Value = log.IpAddress ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();
        return GetBytes(workbook);
    }

    private byte[] GenerateChecklistPdf(List<ChecklistRound> rounds, DateTime start, DateTime end)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                page.Header().AlignCenter().Text("Checklist Report").FontSize(20).Bold();
                page.Header().AlignCenter().Text($"{start:dd/MM/yyyy} - {end:dd/MM/yyyy}").FontSize(10).FontColor(Colors.Grey.Medium);

                page.Content().Column(col =>
                {
                    col.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Purple.Medium);
                    col.Item().PaddingTop(5).Text($"Total Rounds: {rounds.Count}").FontSize(12);

                    col.Item().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Purple.Darken2).Padding(3).Text("Date").FontColor(Colors.White).Bold();
                            h.Cell().Background(Colors.Purple.Darken2).Padding(3).Text("Template").FontColor(Colors.White).Bold();
                            h.Cell().Background(Colors.Purple.Darken2).Padding(3).Text("Executed By").FontColor(Colors.White).Bold();
                        });

                        foreach (var r in rounds)
                        {
                            table.Cell().Padding(2).Text(r.ExecutedAt.ToString("dd/MM/yyyy")).FontSize(9);
                            table.Cell().Padding(2).Text($"Template #{r.ChecklistTemplateId}").FontSize(9);
                            table.Cell().Padding(2).Text(r.ExecutedByUserId ?? "-").FontSize(9);
                        }
                    });
                });

                page.Footer().AlignCenter().Text(x => { x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
            });
        }).GeneratePdf();
    }

    private byte[] GenerateChecklistExcel(List<ChecklistRound> rounds, DateTime start, DateTime end)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Checklist Rounds");

        ws.Cell("A1").Value = "Date";
        ws.Cell("B1").Value = "Template ID";
        ws.Cell("C1").Value = "Executed By";
        ws.Cell("D1").Value = "Notes";
        ws.Range("A1:D1").Style.Fill.BackgroundColor = XLColor.Purple;
        ws.Range("A1:D1").Style.Font.FontColor = XLColor.White;
        ws.Range("A1:D1").Style.Font.Bold = true;

        var row = 2;
        foreach (var r in rounds)
        {
            ws.Cell(row, 1).Value = r.ExecutedAt.ToString("yyyy-MM-dd HH:mm");
            ws.Cell(row, 2).Value = r.ChecklistTemplateId;
            ws.Cell(row, 3).Value = r.ExecutedByUserId ?? "";
            ws.Cell(row, 4).Value = r.Notes ?? "";
            row++;
        }

        ws.Columns().AdjustToContents();
        return GetBytes(workbook);
    }

    private byte[] GenerateHRPdf(List<HrStaff> staff, List<HrDocument> documents, DateTime start, DateTime end)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);

                page.Header().AlignCenter().Text("HR Report").FontSize(20).Bold();

                page.Content().Column(col =>
                {
                    col.Item().PaddingTop(15).LineHorizontal(1).LineColor(Colors.Red.Medium);
                    col.Item().PaddingTop(5).Text($"Total Staff: {staff.Count} | Active: {staff.Count(s => s.IsActive)} | Documents: {documents.Count}").FontSize(12);

                    col.Item().PaddingTop(15).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                            c.RelativeColumn(2);
                        });

                        table.Header(h =>
                        {
                            h.Cell().Background(Colors.Red.Darken2).Padding(3).Text("Name").FontColor(Colors.White).Bold();
                            h.Cell().Background(Colors.Red.Darken2).Padding(3).Text("Type").FontColor(Colors.White).Bold();
                            h.Cell().Background(Colors.Red.Darken2).Padding(3).Text("Status").FontColor(Colors.White).Bold();
                            h.Cell().Background(Colors.Red.Darken2).Padding(3).Text("Documents").FontColor(Colors.White).Bold();
                        });

                        foreach (var s in staff)
                        {
                            var docCount = documents.Count(d => d.HrStaffId == s.Id);
                            table.Cell().Padding(2).Text(s.FullNameEn ?? "-").FontSize(9);
                            table.Cell().Padding(2).Text(s.StaffType.ToString()).FontSize(9);
                            table.Cell().Padding(2).Text(s.IsActive ? "Active" : "Inactive").FontSize(9);
                            table.Cell().Padding(2).Text(docCount.ToString()).FontSize(9);
                        }
                    });

                    col.Item().PaddingTop(15).Text($"Generated: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC").FontSize(8).FontColor(Colors.Grey.Medium);
                });

                page.Footer().AlignCenter().Text(x => { x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
            });
        }).GeneratePdf();
    }

    private byte[] GenerateHRExcel(List<HrStaff> staff, List<HrDocument> documents, DateTime start, DateTime end)
    {
        using var workbook = new XLWorkbook();

        var staffWs = workbook.Worksheets.Add("Staff");
        staffWs.Cell("A1").Value = "Name";
        staffWs.Cell("B1").Value = "Type";
        staffWs.Cell("C1").Value = "Status";
        staffWs.Cell("D1").Value = "Email";
        staffWs.Cell("E1").Value = "Phone";
        staffWs.Cell("F1").Value = "Documents";
        staffWs.Range("A1:F1").Style.Fill.BackgroundColor = XLColor.Red;
        staffWs.Range("A1:F1").Style.Font.FontColor = XLColor.White;
        staffWs.Range("A1:F1").Style.Font.Bold = true;

        var row = 2;
        foreach (var s in staff)
        {
            staffWs.Cell(row, 1).Value = s.FullNameEn ?? "";
            staffWs.Cell(row, 2).Value = s.StaffType.ToString();
            staffWs.Cell(row, 3).Value = s.IsActive ? "Active" : "Inactive";
            staffWs.Cell(row, 4).Value = s.Email ?? "";
            staffWs.Cell(row, 5).Value = s.Phone ?? "";
            staffWs.Cell(row, 6).Value = documents.Count(d => d.HrStaffId == s.Id);
            row++;
        }
        staffWs.Columns().AdjustToContents();

        if (documents.Any())
        {
            var docWs = workbook.Worksheets.Add("Documents");
            docWs.Cell("A1").Value = "Document Name";
            docWs.Cell("B1").Value = "Staff ID";
            docWs.Cell("C1").Value = "Type";
            docWs.Cell("D1").Value = "Expiry";
            docWs.Cell("E1").Value = "Verified";
            docWs.Range("A1:E1").Style.Fill.BackgroundColor = XLColor.Red;
            docWs.Range("A1:E1").Style.Font.FontColor = XLColor.White;
            docWs.Range("A1:E1").Style.Font.Bold = true;

            row = 2;
            foreach (var d in documents)
            {
                docWs.Cell(row, 1).Value = d.DocumentName ?? "";
                docWs.Cell(row, 2).Value = d.HrStaffId;
                docWs.Cell(row, 3).Value = d.DocumentType.ToString();
                docWs.Cell(row, 4).Value = d.ExpiryDate?.ToString("dd/MM/yyyy") ?? "";
                docWs.Cell(row, 5).Value = d.IsVerified ? "Yes" : "No";
                row++;
            }
            docWs.Columns().AdjustToContents();
        }

        return GetBytes(workbook);
    }

    private static void AddRow(TableDescriptor table, string metric, string value)
    {
        table.Cell().Padding(3).Text(metric).FontSize(10);
        table.Cell().Padding(3).Text(value).FontSize(10).Bold();
    }

    private static byte[] GetBytes(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
