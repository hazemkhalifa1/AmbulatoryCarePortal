using System.Text.Json;
using System.Text.Json.Serialization;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AmbulatoryCarePortal.Application.Services;

public class DataExportService : IDataExportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DataExportService> _logger;

    public DataExportService(IUnitOfWork unitOfWork, ILogger<DataExportService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<byte[]> ExportToExcelAsync<T>(List<T> data, string sheetName) where T : class
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(sheetName ?? "Export");

        if (data.Count > 0)
        {
            var properties = typeof(T).GetProperties();
            for (int i = 0; i < properties.Length; i++)
            {
                ws.Cell(1, i + 1).Value = properties[i].Name;
                ws.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.Blue;
                ws.Cell(1, i + 1).Style.Font.FontColor = XLColor.White;
                ws.Cell(1, i + 1).Style.Font.Bold = true;
            }

            for (int row = 0; row < data.Count; row++)
            {
                for (int col = 0; col < properties.Length; col++)
                {
                    var value = properties[col].GetValue(data[row]);
                    ws.Cell(row + 2, col + 1).Value = value?.ToString() ?? "";
                }
            }
        }

        ws.Columns().AdjustToContents();
        return await Task.FromResult(GetBytes(workbook));
    }

    public async Task<byte[]> ExportToPdfAsync<T>(List<T> data, string reportTitle) where T : class
    {
        return await Task.FromResult(Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);

                page.Header().AlignCenter().Text(reportTitle ?? "Report").FontSize(18).Bold();

                page.Content().Column(col =>
                {
                    col.Item().PaddingTop(10).LineHorizontal(1);

                    if (data.Count > 0)
                    {
                        var properties = typeof(T).GetProperties();

                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                foreach (var _ in properties)
                                    c.RelativeColumn();
                            });

                            table.Header(h =>
                            {
                                foreach (var prop in properties)
                                {
                                    h.Cell().Background(Colors.Blue.Darken2).Padding(3)
                                        .Text(prop.Name).FontColor(Colors.White).Bold().FontSize(8);
                                }
                            });

                            foreach (var item in data)
                            {
                                foreach (var prop in properties)
                                {
                                    var value = prop.GetValue(item)?.ToString() ?? "";
                                    table.Cell().Padding(2).Text(value).FontSize(7);
                                }
                            }
                        });

                        col.Item().PaddingTop(5).Text($"Total Records: {data.Count}").FontSize(10);
                    }

                    col.Item().PaddingTop(10).Text($"Generated: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC").FontSize(8).FontColor(Colors.Grey.Medium);
                });

                page.Footer().AlignCenter().Text(x => { x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
            });
        }).GeneratePdf());
    }

    public async Task<byte[]> ExportToCsvAsync<T>(List<T> data) where T : class
    {
        var sb = new System.Text.StringBuilder();
        if (data.Count > 0)
        {
            var properties = typeof(T).GetProperties();
            sb.AppendLine(string.Join(",", properties.Select(p => $"\"{p.Name}\"")));

            foreach (var item in data)
                sb.AppendLine(string.Join(",", properties.Select(p => $"\"{p.GetValue(item)?.ToString() ?? ""}\"")));
        }

        _logger.LogInformation($"CSV export generated for {typeof(T).Name}");
        return await Task.FromResult(System.Text.Encoding.UTF8.GetBytes(sb.ToString()));
    }

    public async Task<string> ExportToJsonAsync<T>(List<T> data) where T : class
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        _logger.LogInformation($"JSON export generated for {typeof(T).Name}");
        return await Task.FromResult(JsonSerializer.Serialize(data, options));
    }

    public async Task<byte[]> ExportPoliciesAsync(int clinicId, string format)
    {
        var policies = await _unitOfWork.Repository<PolicyDocument>().FindAsync(p => p.ClinicId == clinicId);
        var data = policies.Select(p => new
        {
            p.Id, p.Title, p.StandardCode,
            Status = p.DocumentStatus.ToString(),
            p.ExpiryDate, p.CreatedAt, Version = p.VersionNumber
        }).ToList();

        return format.ToLower() switch
        {
            "pdf" => await ExportToPdfAsync(data, "Policy Export"),
            "xlsx" => await ExportToExcelAsync(data, "Policies"),
            "csv" => await ExportToCsvAsync(data),
            _ => await ExportToExcelAsync(data, "Policies")
        };
    }

    public async Task<byte[]> ExportKPIsAsync(int clinicId, string format)
    {
        var kpis = await _unitOfWork.Repository<KPI>().FindAsync(k => k.ClinicId == clinicId);
        var data = kpis.Select(k => new
        {
            k.Id, k.Name, k.TargetValue,
            Frequency = k.Frequency.ToString(), k.CreatedAt
        }).ToList();

        return format.ToLower() switch
        {
            "pdf" => await ExportToPdfAsync(data, "KPI Export"),
            "xlsx" => await ExportToExcelAsync(data, "KPIs"),
            "csv" => await ExportToCsvAsync(data),
            _ => await ExportToExcelAsync(data, "KPIs")
        };
    }

    public async Task<byte[]> ExportStaffAsync(int clinicId, string format)
    {
        var staff = await _unitOfWork.Repository<HrStaff>().FindAsync(s => s.ClinicId == clinicId);
        var data = staff.Select(s => new
        {
            s.Id, s.FullNameEn, s.FullNameAr, s.Email, s.Phone,
            StaffType = s.StaffType.ToString(),
            Status = s.IsActive ? "Active" : "Inactive"
        }).ToList();

        return format.ToLower() switch
        {
            "pdf" => await ExportToPdfAsync(data, "Staff Export"),
            "xlsx" => await ExportToExcelAsync(data, "Staff"),
            "csv" => await ExportToCsvAsync(data),
            _ => await ExportToExcelAsync(data, "Staff")
        };
    }

    public async Task<byte[]> ExportAuditLogsAsync(int clinicId, DateTime startDate, DateTime endDate, string format)
    {
        var auditLogs = await _unitOfWork.Repository<AuditTrail>().FindAsync(
            a => a.ClinicId == clinicId && a.ActionDate >= startDate && a.ActionDate <= endDate
        );
        var data = auditLogs.Select(a => new
        {
            a.Id, Action = a.ActionType.ToString(), a.Description,
            a.CreatedBy, a.ActionDate, a.IpAddress
        }).ToList();

        return format.ToLower() switch
        {
            "pdf" => await ExportToPdfAsync(data, "Audit Log Export"),
            "xlsx" => await ExportToExcelAsync(data, "Audit Logs"),
            "csv" => await ExportToCsvAsync(data),
            _ => await ExportToExcelAsync(data, "Audit Logs")
        };
    }

    private static byte[] GetBytes(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
