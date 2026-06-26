using Microsoft.AspNetCore.Http;

namespace AmbulatoryCarePortal.Presentation.Helpers;

public static class FileUploadValidator
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".doc", ".docx", ".xlsx", ".xls", ".pptx", ".ppt" };

    private static readonly HashSet<string> TemplateExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".docx" };

    public const long DefaultMaxFileSize = 10 * 1024 * 1024; // 10 MB

    public static (bool IsValid, string ErrorMessage) Validate(
        IFormFile? file,
        HashSet<string> allowedExtensions,
        long maxSizeBytes = DefaultMaxFileSize)
    {
        if (file == null || file.Length == 0)
            return (false, "No file provided.");

        if (file.Length > maxSizeBytes)
            return (false, $"File size exceeds the maximum allowed size of {maxSizeBytes / (1024 * 1024)} MB.");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
            return (false, $"File type '{ext}' is not allowed. Accepted types: {string.Join(", ", allowedExtensions)}.");

        return (true, string.Empty);
    }

    public static (bool IsValid, string ErrorMessage) ValidateImage(IFormFile? file) =>
        Validate(file, ImageExtensions, 5 * 1024 * 1024);

    public static (bool IsValid, string ErrorMessage) ValidateDocument(IFormFile? file) =>
        Validate(file, DocumentExtensions, 20 * 1024 * 1024);

    public static (bool IsValid, string ErrorMessage) ValidateTemplate(IFormFile? file) =>
        Validate(file, TemplateExtensions, 10 * 1024 * 1024);
}
