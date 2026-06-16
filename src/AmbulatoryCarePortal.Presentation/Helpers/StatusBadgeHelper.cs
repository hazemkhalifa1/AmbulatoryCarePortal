using AmbulatoryCarePortal.Domain.Enums;

namespace AmbulatoryCarePortal.Presentation.Helpers;

public static class StatusBadgeHelper
{
    public static string GetDocumentStatusClass(DocumentStatus status) => status switch
    {
        DocumentStatus.Approved or DocumentStatus.Complete => "active",
        DocumentStatus.Expired or DocumentStatus.MissingAttachment => "inactive",
        _ => "needs-review"
    };

    public static string GetActiveStatusClass(bool isActive) =>
        isActive ? "active" : "inactive";

    public static string GetClinicDocumentStatusClass(ClinicDocumentStatus status) => status switch
    {
        ClinicDocumentStatus.Complete => "active",
        ClinicDocumentStatus.Expired or ClinicDocumentStatus.MissingAttachment => "inactive",
        _ => "needs-review"
    };

    public static string GetDocumentStatusClass(string status) => status switch
    {
        "Approved" or "Complete" => "active",
        "Expired" or "MissingAttachment" => "inactive",
        _ => "needs-review"
    };
}
