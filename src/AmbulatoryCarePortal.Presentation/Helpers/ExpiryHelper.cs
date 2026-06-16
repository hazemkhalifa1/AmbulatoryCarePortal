namespace AmbulatoryCarePortal.Presentation.Helpers;

public static class ExpiryHelper
{
    public static bool IsExpired(DateTime? expiryDate) =>
        expiryDate.HasValue && expiryDate.Value <= DateTime.UtcNow;

    public static bool IsExpiringSoon(DateTime? expiryDate, int days = 30) =>
        expiryDate.HasValue && expiryDate.Value > DateTime.UtcNow && expiryDate.Value <= DateTime.UtcNow.AddDays(days);

    public static int? DaysUntilExpiry(DateTime? expiryDate) =>
        expiryDate.HasValue ? (int?)(expiryDate.Value - DateTime.UtcNow).Days : null;

    public static string GetExpiryStatusClass(DateTime? expiryDate) =>
        expiryDate.HasValue
            ? expiryDate.Value <= DateTime.UtcNow ? "text-danger fw-bold"
                : expiryDate.Value <= DateTime.UtcNow.AddDays(30) ? "text-warning fw-bold"
                    : ""
            : "";

    public static string GetExpiryLabelClass(DateTime? expiryDate) =>
        expiryDate.HasValue
            ? expiryDate.Value <= DateTime.UtcNow ? "compliance-badge non-compliant"
                : expiryDate.Value <= DateTime.UtcNow.AddDays(30) ? "compliance-badge needs-attention"
                    : "compliance-badge compliant"
            : "compliance-badge compliant";
}
