using AmbulatoryCarePortal.Application.DTOs.Document;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AmbulatoryCarePortal.Application.Services;

public class DocumentGenerationService : IDocumentGenerationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DocumentGenerationService> _logger;

    public DocumentGenerationService(IUnitOfWork unitOfWork, ILogger<DocumentGenerationService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<GeneratedDocumentDto?> GenerateDocxAsync(int assignmentId, string userId)
    {
        var assignment = await _unitOfWork.Repository<ClinicTemplateAssignment>().GetByIdAsync(assignmentId);
        if (assignment == null) return null;

        var template = await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(assignment.DocumentTemplateId);
        if (template == null || string.IsNullOrEmpty(template.TemplateFilePath)) return null;

        var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(assignment.ClinicId);
        if (clinic == null) return null;

        var (textValues, imageValues) = await BuildVariableMapsAsync(assignment, template, clinic);

        var templatePath = ResolvePath(template.TemplateFilePath);
        if (!File.Exists(templatePath)) return null;

        byte[] fileBytes;
        using (var stream = new FileStream(templatePath, FileMode.Open, FileAccess.Read))
        using (var memStream = new MemoryStream())
        {
            stream.CopyTo(memStream);
            memStream.Position = 0;

            using (var wordDoc = WordprocessingDocument.Open(memStream, true))
            {
                ReplaceTextPlaceholders(wordDoc, textValues);
                ReplaceImagePlaceholders(wordDoc, imageValues);
            }

            fileBytes = memStream.ToArray();
        }

        var (safeFileName, relativePath) = SaveGeneratedFile(template, clinic, fileBytes, ".docx");

        var generatedDoc = new GeneratedDocument
        {
            ClinicTemplateAssignmentId = assignmentId,
            DocumentTemplateId = template.Id,
            ClinicId = clinic.Id,
            FileName = safeFileName,
            FilePath = relativePath,
            FileType = ".docx",
            FileSizeBytes = fileBytes.Length,
            GeneratedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<GeneratedDocument>().AddAsync(generatedDoc);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("DOCX generated for assignment {AssignmentId}: {FileName}", assignmentId, safeFileName);

        return MapToDto(generatedDoc);
    }

    public async Task<GeneratedDocumentDto?> GeneratePdfAsync(int assignmentId, string userId)
    {
        var assignment = await _unitOfWork.Repository<ClinicTemplateAssignment>().GetByIdAsync(assignmentId);
        if (assignment == null) return null;

        var template = await _unitOfWork.Repository<DocumentTemplate>().GetByIdAsync(assignment.DocumentTemplateId);
        if (template == null) return null;

        var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(assignment.ClinicId);
        if (clinic == null) return null;

        var (textValues, imageValues) = await BuildVariableMapsAsync(assignment, template, clinic);

        var pdfBytes = GeneratePdfDocument(template, clinic, textValues, imageValues);

        var (safeFileName, relativePath) = SaveGeneratedFile(template, clinic, pdfBytes, ".pdf");

        var generatedDoc = new GeneratedDocument
        {
            ClinicTemplateAssignmentId = assignmentId,
            DocumentTemplateId = template.Id,
            ClinicId = clinic.Id,
            FileName = safeFileName,
            FilePath = relativePath,
            FileType = ".pdf",
            FileSizeBytes = pdfBytes.Length,
            GeneratedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<GeneratedDocument>().AddAsync(generatedDoc);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("PDF generated for assignment {AssignmentId}: {FileName}", assignmentId, safeFileName);

        return MapToDto(generatedDoc);
    }

    private byte[] GeneratePdfDocument(DocumentTemplate template, Clinic clinic, Dictionary<string, string> textValues, Dictionary<string, string> imageValues)
    {
        var templatePath = ResolvePath(template.TemplateFilePath);
        string templateText;

        if (File.Exists(templatePath))
        {
            using (var wordDoc = WordprocessingDocument.Open(templatePath, false))
            {
                var body = wordDoc.MainDocumentPart?.Document?.Body;
                if (body != null)
                {
                    var texts = body.Descendants<Text>().Select(t => t.Text ?? "");
                    templateText = string.Join("", texts);
                }
                else
                {
                    templateText = $"{template.TitleEn} - {template.StandardCode}";
                }
            }
        }
        else
        {
            templateText = $"{template.TitleEn} - {template.StandardCode}";
        }

        foreach (var kvp in textValues)
            templateText = templateText.Replace(kvp.Key, kvp.Value);

        return QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header().Element(c => ComposeHeader(c, template, clinic, imageValues));
                page.Content().Element(c => ComposeContent(c, templateText));
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Generated by CBAHI Portal - ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private void ComposeHeader(IContainer container, DocumentTemplate template, Clinic clinic, Dictionary<string, string> imageValues)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                if (imageValues.TryGetValue("logo", out var logoPath) || imageValues.Values.Any())
                {
                    var firstImage = imageValues.Values.FirstOrDefault();
                    if (firstImage != null)
                    {
                        var fullPath = ResolvePath(firstImage);
                        if (File.Exists(fullPath))
                        {
                            row.ConstantItem(80).Image(fullPath, ImageScaling.FitArea);
                        }
                    }
                }

                row.RelativeItem().PaddingLeft(10).Column(c2 =>
                {
                    c2.Item().Text(template.TitleEn).Bold().FontSize(14);
                    c2.Item().Text($"{template.StandardCode} - {clinic.Name}").FontSize(10).FontColor(Colors.Grey.Medium);
                });
            });

            col.Item().PaddingVertical(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container, string text)
    {
        container.PaddingVertical(10).Column(col =>
        {
            var paragraphs = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var para in paragraphs)
            {
                col.Item().PaddingBottom(5).Text(para.Trim()).FontSize(11);
            }
        });
    }

    private async Task<(Dictionary<string, string> textValues, Dictionary<string, string> imageValues)> BuildVariableMapsAsync(
        ClinicTemplateAssignment assignment, DocumentTemplate template, Clinic clinic)
    {
        var variableValues = await _unitOfWork.Repository<ClinicTemplateValue>()
            .FindAsync(v => v.ClinicTemplateAssignmentId == assignment.Id);
        var variables = await _unitOfWork.Repository<TemplateVariable>()
            .FindAsync(v => v.DocumentTemplateId == template.Id);

        var varMap = variables.ToDictionary(v => v.Id, v => v);
        var textValues = new Dictionary<string, string>();
        var imageValues = new Dictionary<string, string>();

        foreach (var val in variableValues)
        {
            if (!varMap.TryGetValue(val.TemplateVariableId, out var v)) continue;

            if (v.IsImage && !string.IsNullOrEmpty(val.ImagePath))
                imageValues[v.Name.ToLowerInvariant()] = val.ImagePath;
            else if (!string.IsNullOrEmpty(val.Value))
                textValues[$"{{{{{v.Name}}}}}"] = val.Value;
        }

        textValues["{{ClinicName}}"] = clinic.Name;
        textValues["{{ClinicNameAr}}"] = clinic.NameAr ?? "";
        textValues["{{LicenseNumber}}"] = clinic.LicenseNumber ?? "";
        textValues["{{LicenseExpiry}}"] = clinic.LicenseExpiry?.ToString("yyyy-MM-dd") ?? "";
        textValues["{{CityEn}}"] = clinic.CityEn ?? "";
        textValues["{{CityAr}}"] = clinic.CityAr ?? "";
        textValues["{{CurrentDate}}"] = DateTime.Now.ToString("yyyy-MM-dd");
        textValues["{{CurrentYear}}"] = DateTime.Now.Year.ToString();

        if (!string.IsNullOrEmpty(clinic.LogoPath))
            imageValues["logo"] = clinic.LogoPath;

        return (textValues, imageValues);
    }

    private static string ResolvePath(string path)
    {
        if (Path.IsPathRooted(path)) return path;
        return Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", path.TrimStart('/'));
    }

    private static (string fileName, string relativePath) SaveGeneratedFile(DocumentTemplate template, Clinic clinic, byte[] fileBytes, string extension)
    {
        var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "generated");
        Directory.CreateDirectory(dir);

        var fileName = $"{template.StandardCode}_{clinic.Name}_{DateTime.Now:yyyyMMddHHmmss}{extension}";
        var safeFileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
        var relativePath = $"/uploads/generated/{safeFileName}";
        var fullPath = Path.Combine(dir, safeFileName);

        File.WriteAllBytes(fullPath, fileBytes);

        return (safeFileName, relativePath);
    }

    private static GeneratedDocumentDto MapToDto(GeneratedDocument doc)
    {
        return new GeneratedDocumentDto
        {
            Id = doc.Id,
            ClinicTemplateAssignmentId = doc.ClinicTemplateAssignmentId,
            DocumentTemplateId = doc.DocumentTemplateId,
            ClinicId = doc.ClinicId,
            FileName = doc.FileName,
            FilePath = doc.FilePath,
            FileType = doc.FileType,
            FileSizeBytes = doc.FileSizeBytes,
            GeneratedByUserId = doc.GeneratedByUserId,
            CreatedAt = doc.CreatedAt
        };
    }

    public async Task<byte[]?> DownloadGeneratedFileAsync(int generatedDocumentId)
    {
        var doc = await _unitOfWork.Repository<GeneratedDocument>().GetByIdAsync(generatedDocumentId);
        if (doc == null) return null;

        var fullPath = ResolvePath(doc.FilePath);
        return File.Exists(fullPath) ? await File.ReadAllBytesAsync(fullPath) : null;
    }

    public async Task<List<GeneratedDocumentDto>> GetGeneratedDocumentsAsync(int assignmentId)
    {
        var docs = await _unitOfWork.Repository<GeneratedDocument>()
            .FindAsync(g => g.ClinicTemplateAssignmentId == assignmentId);

        return docs.OrderByDescending(d => d.CreatedAt).Select(MapToDto).ToList();
    }

    public async Task<List<TemplateVariablePreviewDto>> ValidateVariablesAsync(int assignmentId)
    {
        var assignment = await _unitOfWork.Repository<ClinicTemplateAssignment>().GetByIdAsync(assignmentId);
        if (assignment == null) return new List<TemplateVariablePreviewDto>();

        var variables = await _unitOfWork.Repository<TemplateVariable>()
            .FindAsync(v => v.DocumentTemplateId == assignment.DocumentTemplateId && v.IsRequired);

        var values = await _unitOfWork.Repository<ClinicTemplateValue>()
            .FindAsync(v => v.ClinicTemplateAssignmentId == assignmentId);

        var valueByVarId = values.ToDictionary(v => v.TemplateVariableId, v => v);

        return variables.Select(v => new TemplateVariablePreviewDto
        {
            Name = v.Name,
            DisplayName = v.DisplayName,
            IsImage = v.IsImage,
            IsRequired = v.IsRequired,
            HasValue = valueByVarId.ContainsKey(v.Id) && !string.IsNullOrEmpty(
                v.IsImage ? valueByVarId[v.Id].ImagePath : valueByVarId[v.Id].Value),
            CurrentValue = valueByVarId.TryGetValue(v.Id, out var val)
                ? (val.ImagePath ?? val.Value)
                : null
        }).ToList();
    }

    private static void ReplaceTextPlaceholders(WordprocessingDocument doc, Dictionary<string, string> variables)
    {
        if (doc.MainDocumentPart?.Document?.Body == null) return;

        ReplaceInElement(doc.MainDocumentPart.Document.Body, variables);

        if (doc.MainDocumentPart.HeaderParts != null)
        {
            foreach (var headerPart in doc.MainDocumentPart.HeaderParts)
            {
                if (headerPart.RootElement != null)
                    ReplaceInElement(headerPart.RootElement, variables);
            }
        }

        if (doc.MainDocumentPart.FooterParts != null)
        {
            foreach (var footerPart in doc.MainDocumentPart.FooterParts)
            {
                if (footerPart.RootElement != null)
                    ReplaceInElement(footerPart.RootElement, variables);
            }
        }
    }

    private static void ReplaceInElement(OpenXmlElement element, Dictionary<string, string> variables)
    {
        foreach (var text in element.Descendants<Text>())
        {
            if (text.Text == null) continue;

            var modified = text.Text;
            foreach (var kvp in variables)
            {
                modified = modified.Replace(kvp.Key, kvp.Value);
            }

            if (modified != text.Text)
                text.Text = modified;
        }
    }

    private void ReplaceImagePlaceholders(WordprocessingDocument doc, Dictionary<string, string> imageMap)
    {
        if (imageMap.Count == 0 || doc.MainDocumentPart?.Document?.Body == null) return;

        var paragraphs = doc.MainDocumentPart.Document.Body.Descendants<Paragraph>().ToList();
        foreach (var para in paragraphs)
        {
            var fullText = string.Concat(para.Descendants<Text>().Select(t => t.Text));

            foreach (var kvp in imageMap)
            {
                if (!fullText.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase)) continue;

                var imagePath = kvp.Value;
                var fullImagePath = ResolvePath(imagePath);
                if (!File.Exists(fullImagePath)) continue;

                try
                {
                    var imageBytes = File.ReadAllBytes(fullImagePath);

                    var imagePart = doc.MainDocumentPart.AddImagePart(ImagePartType.Png);
                    using (var imgStream = new MemoryStream(imageBytes))
                    {
                        imagePart.FeedData(imgStream);
                    }

                    var relationshipId = doc.MainDocumentPart.GetIdOfPart(imagePart);

                    foreach (var text in para.Descendants<Text>().Where(t => t.Text.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase)).ToList())
                    {
                        text.Text = text.Text.Replace(kvp.Key, "", StringComparison.OrdinalIgnoreCase);

                        var run = text.Parent as Run ?? text.Ancestors<Run>().FirstOrDefault();
                        if (run == null) continue;

                        var drawing = new Drawing(
                            new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(
                                new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent { Cx = 914400L * 3, Cy = 914400L * 3 },
                                new DocumentFormat.OpenXml.Drawing.Wordprocessing.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                                new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties { Id = (uint)Math.Abs(kvp.Key.GetHashCode()), Name = "Image" },
                                new DocumentFormat.OpenXml.Drawing.Graphic(
                                    new DocumentFormat.OpenXml.Drawing.GraphicData(
                                        new DocumentFormat.OpenXml.Drawing.Pictures.Picture(
                                            new DocumentFormat.OpenXml.Drawing.Pictures.BlipFill(
                                                new DocumentFormat.OpenXml.Drawing.Blip { Embed = relationshipId },
                                                new DocumentFormat.OpenXml.Drawing.Stretch(new DocumentFormat.OpenXml.Drawing.FillRectangle())
                                            ),
                                            new DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties(
                                                new DocumentFormat.OpenXml.Drawing.Transform2D(
                                                    new DocumentFormat.OpenXml.Drawing.Offset { X = 0L, Y = 0L },
                                                    new DocumentFormat.OpenXml.Drawing.Extents { Cx = 914400L * 3, Cy = 914400L * 3 }
                                                ),
                                                new DocumentFormat.OpenXml.Drawing.PresetGeometry { Preset = DocumentFormat.OpenXml.Drawing.ShapeTypeValues.Rectangle }
                                            )
                                        )
                                    )
                                    { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                                )
                            )
                        );

                        run.Append(drawing);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to inject image {ImagePath} into document", imagePath);
                }
            }
        }
    }
}
