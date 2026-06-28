# Phase 4D — UI Modernization: ClinicDocuments → ClinicTemplates

## Objective

Modernize visible UI naming from "Clinic Documents" to "Clinic Templates" to match the underlying `ClinicTemplateAssignment` implementation.

---

## Updated Pages

| Page | Old Label | New Label |
|---|---|---|
| Sidebar nav link | "Documents" (`Nav.Documents`) | "Clinic Templates" (`Nav.ClinicTemplates`) |
| Page title | "Documents" (`Page.ClinicDocuments`) | "Clinic Templates" (`Page.ClinicTemplates`) |
| List heading | "Documents List" (`Page.ClinicDocumentsList`) | "Clinic Templates List" (`Page.ClinicTemplatesList`) |
| Table aria-label | "Documents" | "Clinic Templates" |

---

## Updated Files

| # | File | Change Type | Details |
|---|---|---|---|
| 1 | `Areas/ClinicAdmin/Controllers/ClinicDocumentsController.cs` | Renamed class + file | `ClinicDocumentsController` → `ClinicTemplatesController` |
| | | | File: `ClinicDocumentsController.cs` → `ClinicTemplatesController.cs` |
| 2 | `Areas/ClinicAdmin/Views/ClinicDocuments/` | Renamed folder | `ClinicDocuments/` → `ClinicTemplates/` |
| 3 | `Areas/ClinicAdmin/Views/ClinicTemplates/Index.cshtml` | Updated translation keys | `Page.ClinicDocuments` → `Page.ClinicTemplates` |
| | | | `Page.ClinicDocumentsList` → `Page.ClinicTemplatesList` |
| 4 | `Areas/ClinicAdmin/Views/Dashboard/Index.cshtml` | Updated controller ref (×2) | `Url.Action("Index", "ClinicDocuments")` → `"ClinicTemplates"` |
| 5 | `Areas/ClinicAdmin/Views/PolicyDocuments/Index.cshtml` | Updated controller ref | `asp-controller="ClinicDocuments"` → `"ClinicTemplates"` |
| 6 | `Areas/ClinicAdmin/Controllers/PolicyDocumentsController.cs` | Updated controller ref | `RedirectToAction("Details", "ClinicDocuments"` → `"ClinicTemplates"` |
| 7 | `Views/Shared/_Layout.cshtml` | Updated nav link | `asp-controller="ClinicDocuments"` → `"ClinicTemplates"` |
| | | Updated nav label | `Nav.Documents` → `Nav.ClinicTemplates` |
| 8 | `Helpers/TranslationService.cs` | Added new key (EN) | `Nav.ClinicTemplates` = "Clinic Templates" |
| | | Added new key (AR) | `Nav.ClinicTemplates` = "قوالب العيادة" |
| | | Changed key value (EN) | `Page.ClinicTemplates` = "Clinic Templates" (was "Documents") |
| | | Changed key value (AR) | `Page.ClinicTemplates` = "قوالب العيادة" (was "المستندات") |
| | | Changed key value (EN) | `Page.ClinicTemplatesList` = "Clinic Templates List" (was "Documents List") |
| | | Changed key value (AR) | `Page.ClinicTemplatesList` = "قائمة قوالب العيادة" (was "قائمة المستندات") |

---

## Updated Navigation

| Location | Before | After |
|---|---|---|
| **Sidebar** | `Nav.Documents` → "Documents" → links to `ClinicDocuments` controller | `Nav.ClinicTemplates` → "Clinic Templates" → links to `ClinicTemplates` controller |
| **Dashboard card (×2)** | `Url.Action("Index", "ClinicDocuments")` | `Url.Action("Index", "ClinicTemplates")` |
| **PolicyDocuments list** | `asp-controller="ClinicDocuments"` (CBAHI Standard button) | `asp-controller="ClinicTemplates"` |
| **PolicyDocuments Details redirect** | `RedirectToAction("Details", "ClinicDocuments")` | `RedirectToAction("Details", "ClinicTemplates")` |

---

## Remaining Legacy Internal Names (not visible to users)

These are internal code references that remain unchanged. They are **not visible UI**:

| Reference | Location | Why It Remains |
|---|---|---|
| `ClinicDocuments` table | All migration snapshots | Historical artifact — cannot change past migrations |
| `ClinicDocument` entity | `Domain/Entities/ClinicDocument.cs` | Entity not yet removed (schema phase) |
| `ClinicDocumentAttachment` entity | `Domain/Entities/ClinicDocumentAttachment.cs` | Child entity (schema phase) |
| `ClinicDocumentStatus` enum | `Domain/Enums/ClinicDocumentStatus.cs` | Shared enum with `ClinicTemplateAssignment` |
| `ClinicDocumentExpiry` enum value | `Domain/Enums/ComplianceItemType.cs` | Shared enum |
| `GetClinicDocumentStatusClass()` | `Helpers/StatusBadgeHelper.cs` | Helper method name — operates on enum |
| `GetClinicDocumentExpiryItemsAsync()` | `ComplianceCalendarService.cs` | Method name — not visible to users |
| `"ClinicDocument"` string | `PolicyDocumentsController.cs:80,119` | Internal type classification tag |
| `Doc.ClinicDocuments` (×2) | `TranslationService.cs` | Dead translation key — not used in any view |
| `Doc.ClinicDocumentsTable` (×2) | `TranslationService.cs` | Dead translation key — not used in any view |

---

## Build Result

```
Build succeeded.
0 Error(s)
106 Warning(s)  [all pre-existing]
```

No new warnings or errors introduced.

---

## Summary

All user-visible "Clinic Documents" references have been updated to "Clinic Templates":

- **Controller**: `ClinicDocumentsController` → `ClinicTemplatesController`
- **View folder**: `ClinicDocuments/` → `ClinicTemplates/`
- **Sidebar**: "Documents" → "Clinic Templates"
- **Page titles/labels**: All updated
- **Navigation links**: All controller references updated
- **Redirects**: All updated

The remaining "ClinicDocument" references are internal code (entity names, enum values, migration artifacts, method names) that are invisible to end users.
