# Document Templates Module — Complete Analysis

> **Project:** CBAHI Ambulatory Care Portal  
> **Date:** 2026-06-17  
> **Scope:** Full technical & business analysis of the Document Templates module and all related components.  
> **Mode:** Read-only analysis — no code modification, no suggestions, no redesign.

---

## 1. BUSINESS ANALYSIS

### Purpose
The Document Templates module is the **authoring and governance layer** for CBAHI (Saudi Center for Healthcare Accreditation) compliance documents. It allows SuperAdmins to define standardized `.docx` templates that represent official CBAHI standard documents (e.g., Leadership, Provision of Care, Infection Control). These templates serve as the **master blueprint** from which clinic-specific documents are created.

### System Workflow Fit
```
SuperAdmin creates DocumentTemplate (with .docx file + metadata)
         │
         ▼
AssignToAllClinics → creates ClinicDocument records for each active Clinic
         │
         ▼
ClinicAdmin downloads → fills placeholders → uploads evidence → marks Complete
         │
         ▼
ComplianceScoreService reads ClinicDocument statuses to compute compliance scores
```

### Users & Roles
| Role | Access Level |
|---|---|
| **SuperAdmin** | Full CRUD on templates; assign to clinics; configure settings |
| **ClinicAdmin** | Views/downloads assigned documents; uploads evidence; changes status |
| **ClinicViewer** | Read-only access to documents list and details |

### Clinic & Compliance Connection
- Templates define **what documents a clinic must have** for CBAHI accreditation
- `ClinicDocument` records link each template to a clinic with a status (`NeedsReview`, `Complete`, `Expired`, etc.)
- The compliance score engine queries `ClinicDocument` statuses to determine clinic-level compliance
- CBAHI standards (LD, PC, FMS, etc.) are hardcoded in `ClinicTypeStandards.cs`

### Business Problem Solved
**Problem:** Healthcare clinics must maintain a specific set of CBAHI-standard documents for accreditation. Without templates, each clinic would independently author documents, leading to inconsistency, missing documents, and failed audits.

**Solution:** A centralized template repository where SuperAdmins define the authoritative version of each required document. Clinics receive assignments and only need to **fill, attach evidence, and mark complete**. This ensures standardization, audit-readiness, and measurable compliance.

---

## 2. UI ANALYSIS

### Page 1: Template List (Index)

| Attribute | Value |
|---|---|
| **Route** | `GET /SuperAdmin/DocumentTemplates` |
| **Controller** | `DocumentTemplatesController` |
| **Action** | `Index` |
| **View** | `Areas/SuperAdmin/Views/DocumentTemplates/Index.cshtml` |
| **Model** | `PagedResult<DocumentTemplateDto>` |
| **Purpose** | Browse, filter, and manage document templates by clinic type and CBAHI standard |

**User Workflow:**
1. User lands on page → sees two large buttons: **AMB CBAHI** and **Dental CBAHI**
2. Selects a clinic type → standards cards appear (LD, PC, FMS, etc.) with document counts
3. Clicks a standard card → table of templates for that standard loads
4. Can search within the table, paginate, edit, delete, or assign templates

**Buttons:**
- `AMB CBAHI` / `Dental CBAHI` — clinic type selection (large hero buttons)
- `Add Document` — navigates to Create page
- Per-row dropdown: `Edit`, `Assign to All Clinics`, `Delete`
- `View` — opens template file in new tab
- Pagination controls (first, prev, page numbers, next, last)

**Filters:**
- Clinic type (via URL query `clinicType`)
- CBAHI standard (via URL query `standard`)
- Search term (by code, title, department) — server-side filtering

**Search:** Text search on `StandardCode`, `TitleEn`, `TitleAr`, `DepartmentCategory` (server-side with `Contains`)

**Table columns:** StandardCode, Title, DepartmentCategory, View link, Active badge, Actions dropdown

**Empty states:**
- No clinic type selected → "Select Clinic Type" hero prompt
- Standard selected but no templates → "No Document Templates Found" with link to Create
- Search with no results → "No data found" message

### Page 2: Create Template

| Attribute | Value |
|---|---|
| **Route** | `GET/POST /SuperAdmin/DocumentTemplates/Create` |
| **Controller** | `DocumentTemplatesController` |
| **Action** | `Create` |
| **View** | `Areas/SuperAdmin/Views/DocumentTemplates/Create.cshtml` |
| **Model** | `CreateDocumentTemplateDto` |

**Form Sections:**
1. **Standard Identification** — ClinicType (dropdown: AMB/Dental), Standard/DepartmentCategory (dynamic dropdown via AJAX), StandardCode (text)
2. **Title Information** — TitleEn (required), TitleAr (optional)
3. **Description** — free textarea
4. **Template File** — drag-and-drop upload area for `.docx` files

**Validation:**
- Client-side `<span asp-validation-for>` + `_ValidationScriptsPartial`
- Server-side `CreateDocumentTemplateDtoValidator` (FluentValidation):
  - `StandardCode`: Required, max 50 chars
  - `TitleEn`: Required, max 255 chars
  - `TitleAr`: max 255 chars
  - `Description`: max 1000 chars
  - `DepartmentCategory`: max 100 chars
  - `ClinicType`: must be valid enum

**JavaScript Interaction:**
- `clinicTypeSelect` change → AJAX call to `GetStandards` → populates `departmentCategorySelect`
- File upload preview with remove functionality
- Standards cache for performance

### Page 3: Edit Template

| Attribute | Value |
|---|---|
| **Route** | `GET/POST /SuperAdmin/DocumentTemplates/Edit/{id}` |
| **Controller** | `DocumentTemplatesController` |
| **Action** | `Edit` |
| **View** | `Areas/SuperAdmin/Views/DocumentTemplates/Edit.cshtml` |
| **Model** | `UpdateDocumentTemplateDto` |

**Form Sections:**
- Same as Create but includes **Status** toggle (`IsActive`) and shows current file link
- Standards dropdown is pre-populated via AJAX on load

### Page 4: Clinic Documents List (ClinicAdmin)

| Attribute | Value |
|---|---|
| **Route** | `GET /ClinicAdmin/ClinicDocuments` |
| **Controller** | `ClinicDocumentsController` |
| **Action** | `Index` |
| **View** | `Areas/ClinicAdmin/Views/ClinicDocuments/Index.cshtml` |
| **Model** | `List<ClinicDocumentDto>` |
| **Auth** | `Permission.documents.manage` |

**Workflow:** ClinicAdmin lands here → sees all templates assigned to their clinic → can download filled `.docx` or view details.

**Filters:** Standard tab pills, status dropdown (Complete/NeedsReview/MissingAttachment/Expired/Draft), search text

**Table columns:** StandardCode, Title (EN/AR), DepartmentCategory, Download button, Attachment count link, Status pill, Expiry Date, Actions (Details, Download)

### Page 5: Clinic Document Details (ClinicAdmin)

| Attribute | Value |
|---|---|
| **Route** | `GET /ClinicAdmin/ClinicDocuments/Details/{id}` |
| **Controller** | `ClinicDocumentsController` |
| **Action** | `Details` |
| **View** | `Areas/ClinicAdmin/Views/ClinicDocuments/Details.cshtml` |
| **Model** | `ClinicDocumentDetailDto` |

**Sections:**
- Document Information card (StandardCode, Title, Status badge, ExpiryDate, Notes)
- Attachments table (FileName, FileType, UploadedAt, Notes, Delete button)
- Upload Attachment form (file input + notes)
- Status action card (Mark Complete, Needs Review, Missing Attachment buttons)
- Download button

**Business Rules:**
- Only `ClinicAdmin` role can upload/delete attachments and change status
- `ClinicViewer` sees "Uploaded" badge instead of delete button
- Clinic-scoped access enforced: verifies `ClinicId` matches user's claim

### Page 6: Settings — Document Template Tab (SuperAdmin)

| Attribute | Value |
|---|---|
| **Route** | `GET /SuperAdmin/Settings?tab=documenttemplate` |
| **Controller** | `SettingsController` |
| **Action** | `Index` (partial via `UpdateDocumentTemplate`) |
| **View** | `Areas/SuperAdmin/Views/Settings/_DocumentTemplateSettingsPartial.cshtml` |

**Form fields:**
- `DocumentTemplate_DefaultExpiryWarningDays` (number, min 1, placeholder 30)
- `DocumentTemplate_MaxFileSizeMB` (number, min 1, placeholder 20)
- `DocumentTemplate_AutoAssignToNewClinics` (boolean toggle)
- `DocumentTemplate_AllowedFileExtensions` (text, comma-separated)

No validation attributes in the ViewModel; values can be null.

---

## 3. CONTROLLER ANALYSIS

### DocumentTemplatesController

| Attribute | Value |
|---|---|
| **Area** | `SuperAdmin` |
| **Auth** | `[Authorize(Policy = "Permission.system.configure")]` |
| **Route base** | `/SuperAdmin/DocumentTemplates` |

**Actions:**

| Method | Action | Route | Parameters | Returns |
|---|---|---|---|---|
| `GET` | `Index` | `/SuperAdmin/DocumentTemplates` | `page, pageSize, searchTerm, clinicType, standard` | View |
| `GET` | `GetStandards` | `/SuperAdmin/DocumentTemplates/GetStandards` | `clinicType` | JSON |
| `GET` | `ByTypeAndStandard` | `/SuperAdmin/DocumentTemplates/ByTypeAndStandard` | `clinicType, standard` | JSON |
| `GET` | `Create` | `/SuperAdmin/DocumentTemplates/Create` | — | View |
| `POST` | `Create` | `/SuperAdmin/DocumentTemplates/Create` | `CreateDocumentTemplateDto, IFormFile?` | Redirect |
| `GET` | `Edit` | `/SuperAdmin/DocumentTemplates/Edit/{id}` | `id` | View |
| `POST` | `Edit` | `/SuperAdmin/DocumentTemplates/Edit/{id}` | `id, UpdateDocumentTemplateDto, IFormFile?` | Redirect |
| `POST` | `Delete` | `/SuperAdmin/DocumentTemplates/Delete` | `id` | Redirect |
| `POST` | `AssignToAllClinics` | `/SuperAdmin/DocumentTemplates/AssignToAllClinics` | `id` | Redirect |

**Dependencies Injected:**
- `IDocumentTemplateService`
- `IUnitOfWork`
- `ILogger<DocumentTemplatesController>`
- `ITranslationService`

**Business Flow:**
1. `Index` — if clinicType+standard provided, queries repository directly for standards-filtered paged results; otherwise delegates to `GetAllTemplatesAsync` for unfiltered search
2. `GetStandards` — AJAX endpoint, returns string array of CBAHI standards for given clinic type
3. `ByTypeAndStandard` — JSON endpoint for template data (unused in current views)
4. `Create` — saves template entity, optionally saves file to `wwwroot/uploads/templates/`, creates **audit log**
5. `Edit` — updates template, optionally deletes old file and saves new one, creates **audit log**
6. `Delete` — soft-deletes via `SoftDelete`, creates **audit log**
7. `AssignToAllClinics` — bulk creates ClinicDocument records for all active clinics

### ClinicDocumentsController

| Attribute | Value |
|---|---|
| **Area** | `ClinicAdmin` |
| **Auth** | `[Authorize(Policy = "Permission.documents.manage")]` |
| **Route base** | `/ClinicAdmin/ClinicDocuments` |

**Actions:**

| Method | Action | Route | Parameters | Returns |
|---|---|---|---|---|
| `GET` | `Index` | `/ClinicAdmin/ClinicDocuments` | `searchTerm, statusFilter, standardFilter` | View |
| `GET` | `Details` | `/ClinicAdmin/ClinicDocuments/Details/{id}` | `id` | View |
| `GET` | `Download` | `/ClinicAdmin/ClinicDocuments/Download/{id}` | `id` | File |
| `POST` | `UploadEvidence` | `/ClinicAdmin/ClinicDocuments/UploadEvidence` | `clinicDocumentId, evidenceFile, notes` | Redirect |
| `POST` | `DeleteAttachment` | `/ClinicAdmin/ClinicDocuments/DeleteAttachment` | `attachmentId` | Redirect |
| `POST` | `UpdateStatus` | `/ClinicAdmin/ClinicDocuments/UpdateStatus` | `id, ClinicDocumentStatus` | Redirect |

**Dependencies Injected:**
- `IClinicDocumentService`
- `IUnitOfWork`
- `ILogger<ClinicDocumentsController>`
- `ITranslationService`

**Business Flow:**
1. All actions extract `ClinicId` from `User.FindFirst("ClinicId")` claim
2. Download action: calls `DownloadDocumentAsync` which opens `.docx`, replaces `{{ClinicName}}`, `{{LicenseNumber}}` placeholders, returns filled file
3. UploadEvidence: saves file to `wwwroot/uploads/document-evidence/`, creates `ClinicDocumentAttachment` record, audit log
4. UpdateStatus: changes `ClinicDocument.DocumentStatus`, audit log
5. Clinic-scoped access enforced manually in each action

### SettingsController (DocumentTemplate-related)

| Attribute | Value |
|---|---|
| **Area** | `SuperAdmin` |
| **Auth** | `[Authorize(Policy = "Permission.system.configure")]` |
| **Action** | `UpdateDocumentTemplate` |
| **Route** | `POST /SuperAdmin/Settings/UpdateDocumentTemplate` |

Saves 5 settings keys (see Settings Analysis below). No validation on the ViewModel.

---

## 4. SERVICE ANALYSIS

### 4.1 DocumentTemplateService

| Attribute | Value |
|---|---|
| **Interface** | `IDocumentTemplateService` |
| **Location** | `Application/Services/DocumentTemplateService.cs` |
| **Responsibilities** | CRUD for templates, file path management, bulk clinic assignment |

**Public Methods:**

| Method | Return | Description |
|---|---|---|
| `GetAllTemplatesAsync(pageNumber, pageSize, searchTerm?)` | `PagedResult<DocumentTemplateDto>` | Paged listing with optional search (StandardCode, TitleEn, TitleAr, DepartmentCategory) |
| `GetTemplateByIdAsync(id)` | `DocumentTemplateDto?` | Single template lookup |
| `CreateTemplateAsync(dto)` | `int` (new ID) | Maps DTO → entity, saves, sets `IsActive=true` |
| `UpdateTemplateAsync(dto)` | `bool` | Maps DTO → existing entity, sets `UpdatedAt` |
| `DeleteTemplateAsync(id)` | `bool` | Soft-delete (sets `IsDeleted=true`) |
| `UploadTemplateFileAsync(id, filePath)` | `bool` | Updates `TemplateFilePath` on entity |
| `GetTemplatesByTypeAndStandardAsync(clinicType, standard)` | `List<DocumentTemplateDto>` | Filters by ClinicType and DepartmentCategory |
| `AssignToAllClinicsAsync(templateId)` | `void` | Creates ClinicDocument records for all active clinics not already assigned |

**Internal Logic:**
- All methods use `IUnitOfWork.Repository<DocumentTemplate>()`
- Uses `AutoMapper` for entity ↔ DTO conversion
- `AssignToAllClinicsAsync` queries all active clinics, finds existing assignments, creates only new ones (`ClinicDocumentStatus.NeedsReview`)

**Dependencies:** `IUnitOfWork`, `IMapper`, `ILogger<DocumentTemplateService>`

### 4.2 ClinicDocumentService

| Attribute | Value |
|---|---|
| **Interface** | `IClinicDocumentService` |
| **Location** | `Application/Services/ClinicDocumentService.cs` |
| **Responsibilities** | Clinic-level document management, placeholder replacement, attachment management, status transitions |

**Public Methods:**

| Method | Return | Description |
|---|---|---|
| `GetClinicDocumentsAsync(clinicId, searchTerm?, statusFilter?, standardFilter?)` | `List<ClinicDocumentDto>` | Joins ClinicDocument + DocumentTemplate in memory, applies filters |
| `GetClinicDocumentDetailsAsync(id)` | `ClinicDocumentDetailDto?` | Single doc with template metadata + attachments |
| `UploadEvidenceAsync(clinicDocumentId, fileName, filePath, fileType, uploadedByUserId, notes?)` | `bool` | Creates ClinicDocumentAttachment |
| `DeleteAttachmentAsync(attachmentId)` | `bool` | Soft-deletes attachment |
| `UpdateStatusAsync(clinicDocumentId, status)` | `bool` | Changes DocumentStatus, sets UpdatedAt |
| `DownloadDocumentAsync(clinicDocumentId)` | `(byte[], string)?` | Opens .docx, replaces placeholders, returns file bytes |

**Placeholder Replacement (`ReplacePlaceholders`):**
Uses `DocumentFormat.OpenXml` to open the `.docx` and replace these tokens:
```
{{ClinicName}}, {{ClinicNameAr}}, {{LogoPath}}, {{LicenseNumber}},
{{LicenseExpiry}}, {{CityEn}}, {{CityAr}}, {{CurrentDate}}, {{CurrentYear}}
```
Replacement runs on body, headers, and footers.

**Dependencies:** `IUnitOfWork`, `IMapper`, `ILogger<ClinicDocumentService>`

### 4.3 SettingsService

| Attribute | Value |
|---|---|
| **Interface** | `ISettingsService` |
| **Location** | `Application/Services/SettingsService.cs` |
| **Responsibilities** | Key-value settings storage with caching and encryption |

**Relevant Public Methods:**

| Method | Return | Description |
|---|---|---|
| `GetValueAsync(key)` | `string?` | Gets raw value (decrypts if `IsEncrypted`) |
| `GetValueAsync<T>(key, default?)` | `T?` | Type-coerced value |
| `SetValueAsync(key, value)` | `void` | Upserts SystemSetting record, invalidates cache |
| `GetByCategoryAsync(category)` | `List<SystemSettingDto>` | All settings in a category |

**Dependencies:** `IUnitOfWork`, `IEncryptionService`, `IMapper`, `ILogger<SettingsService>`, `ICacheService`

---

## 5. DATABASE ANALYSIS

### Table: `DocumentTemplates`

| Column | Type | Constraints |
|---|---|---|
| `Id` | `int` | PK, auto-increment |
| `StandardCode` | `nvarchar(50)` | Required, Unique (filtered: IsDeleted=0) |
| `TitleEn` | `nvarchar(255)` | Required |
| `TitleAr` | `nvarchar(255)` | Nullable |
| `Description` | `nvarchar(1000)` | Nullable |
| `DepartmentCategory` | `nvarchar(100)` | Nullable |
| `ClinicType` | `nvarchar(50)` | Required, stored as string (enum) |
| `TemplateFilePath` | `nvarchar(500)` | Nullable |
| `IsActive` | `bit` | Default true |
| `CreatedAt` | `datetime2` | Inherited from BaseEntity |
| `UpdatedAt` | `datetime2` | Nullable |
| `IsDeleted` | `bit` | Default false |
| `CreatedBy` | `nvarchar(?)` | Nullable |
| `UpdatedBy` | `nvarchar(?)` | Nullable |

**Indexes:**
- PK on `Id`
- Unique filtered index on `StandardCode` WHERE `IsDeleted = 0`

**Relationships:**
- One-to-Many: `DocumentTemplate` → `ClinicDocuments` (via `DocumentTemplateId` FK)
- The FK has `OnDelete(DeleteBehavior.Restrict)` (overridden globally)

### Table: `ClinicDocuments`

| Column | Type | Constraints |
|---|---|---|
| `Id` | `int` | PK, auto-increment |
| `ClinicId` | `int` | FK → Clinics, Restrict |
| `DocumentTemplateId` | `int` | FK → DocumentTemplates, Restrict |
| `DocumentStatus` | `nvarchar(50)` | Required, stored as string (enum) |
| `ExpiryDate` | `datetime2` | Nullable |
| `OfficialPdfPath` | `nvarchar(500)` | Nullable |
| `Notes` | `nvarchar(1000)` | Nullable |
| BaseEntity columns | — | CreatedAt, UpdatedAt, IsDeleted, CreatedBy, UpdatedBy |

**Indexes:**
- PK on `Id`
- Unique filtered index on `(ClinicId, DocumentTemplateId)` WHERE `IsDeleted = 0` (prevents duplicate assignments)

**Relationships:**
- `ClinicId` → `Clinics.Id` (Restrict)
- `DocumentTemplateId` → `DocumentTemplates.Id` (Restrict)
- One-to-Many: `ClinicDocument` → `ClinicDocumentAttachments` (Cascade)

### Table: `ClinicDocumentAttachments`

| Column | Type | Constraints |
|---|---|---|
| `Id` | `int` | PK, auto-increment |
| `ClinicDocumentId` | `int` | FK → ClinicDocuments, Cascade |
| `FileName` | `nvarchar(255)` | Required |
| `FilePath` | `nvarchar(500)` | Nullable |
| `FileType` | `nvarchar(50)` | Nullable |
| `UploadedByUserId` | `nvarchar(?)` | FK → AspNetUsers, Restrict |
| `UploadedAt` | `datetime2` | Default `DateTime.UtcNow` |
| `Notes` | `nvarchar(1000)` | Nullable |
| BaseEntity columns | — | Standard |

### Table: `SystemSettings`

| Column | Type | Constraints |
|---|---|---|
| `Id` | `int` | PK |
| `Key` | `nvarchar(?)` | Required |
| `Value` | `nvarchar(?)` | Nullable |
| `Category` | `int` (enum) | Stored as int |
| `ValueType` | `int` (enum) | Stored as int |
| `Description` | `nvarchar(?)` | Nullable |
| `IsEncrypted` | `bit` | Default false |
| BaseEntity columns | — | Standard |

### Entity Relationship Diagram

```
┌───────────────────┐       ┌──────────────────────┐
│   DocumentTemplate │       │        Clinic         │
├───────────────────┤       ├──────────────────────┤
│ PK: Id            │       │ PK: Id               │
│ StandardCode      │       │ Name, NameAr          │
│ TitleEn           │       │ ClinicType            │
│ TitleAr           │       │ LicenseNumber         │
│ DepartmentCategory│       │ IsActive              │
│ ClinicType        │       └──────────┬───────────┘
│ TemplateFilePath  │                  │
│ IsActive          │                  │
└────────┬──────────┘                  │
         │ 1                          │ 1
         │                            │
         │ *                          │ *
         ▼                            ▼
┌──────────────────────────────────────────────┐
│              ClinicDocument                    │
├──────────────────────────────────────────────┤
│ PK: Id                                        │
│ FK: ClinicId                                  │
│ FK: DocumentTemplateId                        │
│ DocumentStatus (enum)                         │
│ ExpiryDate                                    │
│ OfficialPdfPath                               │
│ Notes                                         │
│ UNIQUE(ClinicId, DocumentTemplateId)          │
└──────────────────────┬───────────────────────┘
                       │ 1
                       │
                       │ *
                       ▼
┌──────────────────────────────────────────────┐
│          ClinicDocumentAttachment              │
├──────────────────────────────────────────────┤
│ PK: Id                                        │
│ FK: ClinicDocumentId                          │
│ FileName, FilePath, FileType                  │
│ FK: UploadedByUserId → AspNetUsers            │
│ UploadedAt, Notes                             │
└──────────────────────────────────────────────┘
```

---

## 6. ENTITY ANALYSIS

### `DocumentTemplate : BaseEntity`

| Property | Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `int` | PK, auto-increment | Inherited |
| `StandardCode` | `string` | Required, max 50, unique filtered | e.g., "LD1", "FMS07" |
| `TitleEn` | `string` | Required, max 255 | English title |
| `TitleAr` | `string?` | Max 255 | Arabic title |
| `Description` | `string?` | Max 1000 | Free-text description |
| `DepartmentCategory` | `string?` | Max 100 | Maps to CBAHI standard code |
| `ClinicType` | `ClinicType` (enum) | Required, stored as string | `AMB` or `Dental` |
| `TemplateFilePath` | `string?` | Max 500 | Relative path to `.docx` |
| `IsActive` | `bool` | Default true | Toggle for soft enable/disable |
| `CreatedAt` | `DateTime` | Default UtcNow | Inherited |
| `UpdatedAt` | `DateTime?` | — | Inherited |
| `IsDeleted` | `bool` | Default false | Soft delete flag |
| `CreatedBy` | `string?` | — | Inherited |
| `UpdatedBy` | `string?` | — | Inherited |

**Navigation Properties:**
- `ClinicDocuments` : `ICollection<ClinicDocument>` — One-to-many

### `ClinicDocument : BaseEntity`

| Property | Type | Constraints | Notes |
|---|---|---|---|
| `ClinicId` | `int` | FK → Clinic, Restrict | Target clinic |
| `DocumentTemplateId` | `int` | FK → DocumentTemplate, Restrict | Source template |
| `DocumentStatus` | `ClinicDocumentStatus` | Required, stored as string | Draft, NeedsReview, Complete, MissingAttachment, Expired |
| `ExpiryDate` | `DateTime?` | Nullable | When document expires |
| `OfficialPdfPath` | `string?` | Max 500 | Path to finalized PDF |
| `Notes` | `string?` | Max 1000 | Clinic notes |
| BaseEntity | — | Standard | — |

**Navigation Properties:**
- `Clinic` : `Clinic` (required) — Many-to-one
- `DocumentTemplate` : `DocumentTemplate` (required) — Many-to-one
- `Attachments` : `ICollection<ClinicDocumentAttachment>` — One-to-many (Cascade)

### `ClinicDocumentAttachment : BaseEntity`

| Property | Type | Constraints | Notes |
|---|---|---|---|
| `ClinicDocumentId` | `int` | FK → ClinicDocument, Cascade | Parent document |
| `FileName` | `string` | Required, max 255 | Original filename |
| `FilePath` | `string?` | Max 500 | Storage path |
| `FileType` | `string?` | Max 50 | Extension |
| `UploadedByUserId` | `string?` | FK → AspNetUsers, Restrict | Who uploaded |
| `UploadedAt` | `DateTime` | Default UtcNow | Timestamp |
| `Notes` | `string?` | Max 1000 | User notes |

**Navigation Properties:**
- `ClinicDocument` : `ClinicDocument` (required) — Many-to-one
- `UploadedByUser` : `AppUser?` — Many-to-one

---

## 7. SETTINGS ANALYSIS

### SystemSetting Keys for Document Templates

| Key | ViewModel Property | Type | Default (placeholder) | Effect |
|---|---|---|---|---|
| `DocumentTemplate.DefaultExpiryWarningDays` | `DocumentTemplate_DefaultExpiryWarningDays` | int? | 30 | Number of days before expiry to trigger warnings |
| `DocumentTemplate.AutoAssignToNewClinics` | `DocumentTemplate_AutoAssignToNewClinics` | bool? | (unchecked) | Whether new clinics automatically receive all templates |
| `DocumentTemplate.AllowedFileExtensions` | `DocumentTemplate_AllowedFileExtensions` | string | `.pdf,.docx,.xlsx` | Comma-separated list of allowed upload file types |
| `DocumentTemplate.MaxFileSizeMB` | `DocumentTemplate_MaxFileSizeMB` | int? | 20 | Max upload file size in MB |
| `DocumentTemplate.DefaultClinicTypeCategory` | `DocumentTemplate_DefaultClinicTypeCategory` | string | (empty) | Default category for clinic type selection |

**Important:** These settings are **stored but never read** by the current controller or service implementations. They are only **written** via the Settings UI. The actual upload validation in controllers is hardcoded:
- File type: hardcoded to `.docx` only (in Create and Edit views)
- File size: not validated in controller (relying on `FileUploadSettings.MaxFileSizeBytes` from `appsettings.json` at 20MB)

### appsettings.json Relevant Section
```json
"FileUploadSettings": {
  "MaxFileSizeBytes": 20971520,
  "AllowedExtensions": [".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".xslx", ".xls"],
  "BasePath": "wwwroot/uploads"
}
```
This config is **not consumed** by the Document Templates module directly. The module uses hardcoded paths.

### SettingCategory Enum
`DocumentTemplate` exists as a distinct category value in `SettingCategory` enum (alongside General, Mail, Branding, Notifications, Localization).

---

## 8. FILE STORAGE ANALYSIS

### Storage Locations
| File Type | Path | Hardcoded / Config |
|---|---|---|
| Template files (.docx) | `wwwroot/uploads/templates/{StandardCode}_{random}.docx` | Hardcoded in controller |
| Clinic evidence attachments | `wwwroot/uploads/document-evidence/{random}{ext}` | Hardcoded in controller |

### Upload Process (Template)
1. User selects `.docx` file in Create/Edit form
2. `IFormFile templateFile` arrives at controller action
3. File name generated: `{StandardCode}_{Path.GetRandomFileName()}.docx`
4. Directory created if not exists: `Directory.CreateDirectory("wwwroot/uploads/templates")`
5. File saved via `FileStream`
6. `TemplateFilePath` updated in DB via `UploadTemplateFileAsync`

### Upload Process (Evidence)
1. User selects file in Details view
2. File name: `Path.GetRandomFileName() + original extension`
3. Directory: `wwwroot/uploads/document-evidence/`
4. File saved, DB record created via `UploadEvidenceAsync`

### File Types Allowed
- **Template upload:** Only `.docx` (enforced by `accept=".docx"` in the view)
- **Evidence upload:** No client-side restriction; only `required` attribute

### Validation
- **Client-side:** `accept=".docx"` attribute on template file input
- **Server-side:** No file type validation in controller (only null/empty check)
- **Size:** No explicit size check in Document Templates controller
- **No virus/malware scanning**

### Download Process
1. `ClinicDocumentsController.Download` → `ClinicDocumentService.DownloadDocumentAsync`
2. Loads template file from `TemplateFilePath`
3. Opens with `WordprocessingDocument` (OpenXML)
4. Replaces placeholders (`{{ClinicName}}`, etc.) in body/headers/footers
5. Returns `FileContentResult` with MIME type `application/vnd.openxmlformats-officedocument.wordprocessingml.document`

### Delete Process
- File deletion only happens during **Edit** (when replacing with new file)
- Old file is deleted: `System.IO.File.Delete(oldFullPath)`
- **No file deletion on template Delete** (only soft-deletes the entity)
- Attachment deletion is soft-delete only (no file deletion from disk)

### Security Checks
- No file extension validation server-side
- No MIME type validation
- No file size validation in controller
- Path traversal not explicitly prevented (uses `Path.Combine` with hardcoded `wwwroot/uploads`)

---

## 9. CLINIC INTEGRATION ANALYSIS

### Template-to-Clinic Assignment Model
- Templates are **not directly assigned to clinics**
- `ClinicDocument` is the **junction entity** linking `DocumentTemplate` + `Clinic` with a status

### Assignment Flow (`AssignToAllClinics`)
1. SuperAdmin clicks "Assign to All Clinics" on a template
2. `DocumentTemplateService.AssignToAllClinicsAsync(templateId)`:
   - Loads template
   - Queries all active clinics (`c.IsActive == true`)
   - Queries existing `ClinicDocument` records for this template
   - Creates new `ClinicDocument` records for clinics not yet assigned
   - Each new record starts with `ClinicDocumentStatus.NeedsReview`

### Clinic Consumption Flow
1. ClinicAdmin logs in → goes to `ClinicDocuments/Index`
2. Sees all `ClinicDocument` records for their clinic
3. Can **download** the filled `.docx` (with clinic data merged)
4. Can **upload evidence** (attachments) to document
5. Can **update status**: NeedsReview → Complete / MissingAttachment
6. No creation or modification of template content — only evidence and status

### Creation Flow
```
SuperAdmin creates DocumentTemplate
         ↓
SuperAdmin clicks "Assign to All Clinics"
         ↓
ClinicDocument records created for all active clinics (NeedsReview)
         ↓
ClinicAdmin sees documents in their dashboard
```

### Synchronization Flow
- **No automated synchronization exists**
- When a template is updated (file changed), existing ClinicDocument assignments still point to the old template
- There is no "re-assign" or "sync" mechanism
- The unique index on `(ClinicId, DocumentTemplateId)` prevents duplicate assignments

### Update Flow
1. SuperAdmin edits template metadata or replaces file
2. Existing `ClinicDocument` records remain unchanged
3. `TemplateFilePath` is updated on the template entity
4. Next time ClinicAdmin downloads, they get the new file (because download reads `Template.TemplateFilePath` at runtime)

---

## 10. PERMISSION ANALYSIS

### Roles
| Role | System Name | Description |
|---|---|---|
| **SuperAdmin** | `SuperAdmin` | Full access, all 46 permissions |
| **ClinicAdmin** | `ClinicAdmin` | Clinic-level operations, 29 permissions |
| **ClinicViewer** | `ClinicViewer` | Read-only, 10 permissions |

### Permission System Architecture
- Custom claim-based `PermissionRequirement` + `PermissionAuthorizationHandler`
- Permissions stored as `Claim("Permission", "permission.name")` on IdentityRole
- Policy format: `"Permission.{permissionName}"` (e.g., `"Permission.system.configure"`)
- Three roles with hardcoded permission sets in `RolePermissionsSeeder`

### Document Template-Specific Permissions

| Permission String | Policy Name | Roles with Access |
|---|---|---|
| `system.configure` | `Permission.system.configure` | SuperAdmin only |
| `documents.manage` | `Permission.documents.manage` | ClinicAdmin, SuperAdmin |

### Capability Matrix

| Capability | SuperAdmin | ClinicAdmin | ClinicViewer |
|---|---|---|---|
| Create template | ✅ (`system.configure`) | ❌ | ❌ |
| Edit template | ✅ (`system.configure`) | ❌ | ❌ |
| Delete template | ✅ (`system.configure`) | ❌ | ❌ |
| View template list | ✅ (`system.configure`) | N/A (different view) | ❌ |
| View clinic documents | ✅ (`system.configure`) | ✅ (`documents.manage`) | ✅ (read clinic docs) |
| Download filled doc | ✅ | ✅ | ✅ |
| Upload evidence | ✅ | ✅ | ❌ |
| Delete attachment | ✅ | ✅ | ❌ |
| Change document status | ✅ | ✅ | ❌ |
| Assign to clinics | ✅ (`system.configure`) | ❌ | ❌ |
| Configure template settings | ✅ (`system.configure`) | ❌ | ❌ |

**Note:** There is no dedicated "document template" permission. The module shares `system.configure` with all other system settings and `documents.manage` with HR documents. The `Permission.documents.manage` permission is used for both `ClinicDocumentsController` and HR document management.

---

## 11. DEPENDENCY ANALYSIS

```
DocumentTemplatesController (SuperAdmin)
  ├── IDocumentTemplateService
  │     ├── IUnitOfWork
  │     │     ├── AppDbContext (EF Core)
  │     │     │     ├── DocumentTemplates DbSet
  │     │     │     ├── ClinicDocuments DbSet
  │     │     │     ├── Clinics DbSet
  │     │     │     └── AuditTrails DbSet
  │     │     └── GenericRepository<T>
  │     └── IMapper (AutoMapper)
  │         └── MappingProfile
  └── ITranslationService
        └── TranslationService
              ├── IHttpContextAccessor
              └── IWebHostEnvironment (Resources/*.json)

ClinicDocumentsController (ClinicAdmin)
  ├── IClinicDocumentService
  │     ├── IUnitOfWork
  │     │     └── (same as above + ClinicDocumentAttachments)
  │     └── IMapper
  └── ITranslationService

SettingsController → UpdateDocumentTemplate
  ├── ISettingsService
  │     ├── IUnitOfWork → SystemSettings DbSet
  │     ├── IEncryptionService
  │     ├── ICacheService (Redis)
  │     └── IMapper
  └── ITranslationService

ComplianceScoreService (indirect consumer)
  └── Reads ClinicDocument.DocumentStatus for scoring

AuditService (via controller audit log creation)
  ├── IUnitOfWork → AuditTrails DbSet
  └── Hangfire (background job for audit logging)

NotificationService (potential consumer)
  └── Could check ClinicDocument expiry
```

---

## 12. CURRENT LIMITATIONS

### Missing Functionality
- No template **versioning** — replacing the file overwrites; no history
- No **bulk operations** — can only assign one template at a time
- No **preview** of template before download
- No **expiry date calculation** or auto-expiry logic
- No **notification** triggers when documents are assigned or expiring (for Document Templates specifically)
- No **search** in the ClinicAdmin document details view
- No **sorting** options (only ordered by StandardCode)
- No **export** of template list or assignments
- No **bulk delete** or **bulk status change** for ClinicAdmin
- Assign modal says "Assign to All Clinics" but has no per-clinic selection
- No **activity log** specific to document templates (only generic AuditTrail)

### Technical Limitations
- **In-memory join**: `ClinicDocumentService.GetClinicDocumentsAsync` loads ALL clinic documents then joins with templates in a foreach loop (N+1 query pattern)
- **No pagination** in ClinicDocuments list (returns all records, client-side only)
- **No file validation** server-side (only HTML `accept` attribute on template upload)
- **No file size validation** in controller
- **No cleanup** of orphaned template files when template is deleted
- **No Path traversal protection** on file operations
- **Hardcoded paths** (`wwwroot/uploads/templates`, `.docx` extension)
- **Settings stored but never consumed** — `DefaultExpiryWarningDays`, `AllowedFileExtensions`, `MaxFileSizeMB` are written but never read
- **Placeholder replacement limited** — no way for clinics to add custom variables
- **Global DeleteBehavior.Restrict** override in `AppDbContext.OnModelCreating` overrides the Cascade on `ClinicDocumentAttachment`

### UX Limitations
- **No inline editing** — must go to separate Edit page
- **No drag-and-drop reordering** of templates
- **No confirmation** prompt on status change (missing for MarkComplete)
- **No batch operations** — every action is per-document
- **Standards cards show count** only when clinic type is selected in URL params
- **No toast notifications** in SuperAdmin views (uses TempData + page reload)
- **No "last download" timestamp** tracking

### Security Limitations
- **No server-side file type validation** — only client-side `accept` attribute
- **No file content validation** — any `.docx` (malicious or malformed) accepted
- **No authorization on `GetStandards`** endpoint (returns public data but no sensitive)
- **No rate limiting** on upload/download endpoints
- **Audit logs created manually** in controller action code (not via `AuditSaveChangesInterceptor`)
- **No input sanitization** on file names (uses `Path.GetRandomFileName()` which is safe)

### Scalability Limitations
- **No pagination** on ClinicDocuments Index (all records returned)
- **In-memory filtering** in `GetClinicDocumentsAsync` — loads all clinic docs then filters in C#
- **No EF Core includes** for eager loading — uses separate `GetByIdAsync` per template (N+1)
- **No caching** for template data
- **File storage on local disk** — not cloud-ready

---

## 13. SETTINGS MODULE RELATIONSHIP

### Shared Services
| Service | Used By Templates | Used By Settings |
|---|---|---|
| `ISettingsService` | ❌ (templates never read settings) | ✅ (read/write all keys) |
| `IUnitOfWork` | ✅ | ✅ |
| `IMapper` | ✅ | ✅ |
| `ICacheService` | ❌ | ✅ |
| `IEncryptionService` | ❌ | ✅ |
| `ITranslationService` | ✅ | ✅ |

### Shared Tables
| Table | Templates Module | Settings Module |
|---|---|---|
| `SystemSettings` | ❌ (never accessed) | ✅ (primary table) |
| `DocumentTemplates` | ✅ (primary table) | ❌ |
| `ClinicDocuments` | ✅ | ❌ |
| `ClinicDocumentAttachments` | ✅ | ❌ |
| `AuditTrails` | ✅ (writes only) | ❌ |
| `Clinics` | ✅ (reads for assignment) | ❌ |

### Shared Configuration
| Config Source | Templates Reads | Settings Reads |
|---|---|---|
| `appsettings.json` `FileUploadSettings` | ❌ (hardcoded) | ❌ |
| `SystemSetting` table | ❌ | ✅ (primary data source) |
| Translation JSON files | ✅ | ✅ |

### Runtime Dependencies
```
SettingsController.UpdateDocumentTemplate
  → calls ISettingsService.SetValueAsync("DocumentTemplate.*") → writes SystemSettings table
  → Settings written but templates never read them

DocumentTemplatesController
  → never calls ISettingsService
  → never reads SystemSettings
  → uses hardcoded values (.docx, wwwroot/uploads/templates, no size limit)

Conclusion: The connection is ONE-WAY. Settings writes config values that the template
module never consumes. There is no runtime coupling between the two modules.
```

---

## 14. SOURCE FILE INVENTORY

### Controllers
| Path | Lines |
|---|---|
| `src/AmbulatoryCarePortal.Presentation/Areas/SuperAdmin/Controllers/DocumentTemplatesController.cs` | 316 |
| `src/AmbulatoryCarePortal.Presentation/Areas/ClinicAdmin/Controllers/ClinicDocumentsController.cs` | 235 |
| `src/AmbulatoryCarePortal.Presentation/Areas/SuperAdmin/Controllers/SettingsController.cs` | 149 |

### Services
| Path | Lines |
|---|---|
| `src/AmbulatoryCarePortal.Application/Services/DocumentTemplateService.cs` | 152 |
| `src/AmbulatoryCarePortal.Application/Services/ClinicDocumentService.cs` | 262 |
| `src/AmbulatoryCarePortal.Application/Services/SettingsService.cs` | 152 |

### Interfaces
| Path | Lines |
|---|---|
| `src/AmbulatoryCarePortal.Application/Interfaces/IDocumentTemplateService.cs` | 17 |
| `src/AmbulatoryCarePortal.Application/Interfaces/IClinicDocumentService.cs` | 14 |
| `src/AmbulatoryCarePortal.Application/Interfaces/ISettingsService.cs` | 13 |
| `src/AmbulatoryCarePortal.Application/Interfaces/IUnitOfWork.cs` | (delegates to Infrastructure) |
| `src/AmbulatoryCarePortal.Application/Interfaces/Repositories/IGenericRepository.cs` | 26 |
| `src/AmbulatoryCarePortal.Application/Interfaces/ICacheService.cs` | 9 |
| `src/AmbulatoryCarePortal.Presentation/Helpers/ITranslationService.cs` | 8 |

### Entities
| Path | Lines |
|---|---|
| `src/AmbulatoryCarePortal.Domain/Entities/DocumentTemplate.cs` | 17 |
| `src/AmbulatoryCarePortal.Domain/Entities/ClinicDocument.cs` | 18 |
| `src/AmbulatoryCarePortal.Domain/Entities/ClinicDocumentAttachment.cs` | 16 |
| `src/AmbulatoryCarePortal.Domain/Entities/BaseEntity.cs` | 11 |
| `src/AmbulatoryCarePortal.Domain/Entities/Clinic.cs` | 31 |
| `src/AmbulatoryCarePortal.Domain/Entities/SystemSetting.cs` | 13 |
| `src/AmbulatoryCarePortal.Domain/Entities/AuditTrail.cs` | (referenced) |

### DTOs
| Path | Lines |
|---|---|
| `src/AmbulatoryCarePortal.Application/DTOs/Document/DocumentDtos.cs` | 81 |

**Classes:** `CreateDocumentTemplateDto`, `UpdateDocumentTemplateDto`, `DocumentTemplateDto`, `ClinicDocumentDto`, `ClinicDocumentDetailDto`, `ClinicDocumentAttachmentDto`

### ViewModels
| Path | Lines |
|---|---|
| `src/AmbulatoryCarePortal.Presentation/ViewModels/SettingsViewModels.cs` | 57 |

**Relevant class:** `DocumentTemplateSettingsViewModel`

### Views
| Path | Lines |
|---|---|
| `src/AmbulatoryCarePortal.Presentation/Areas/SuperAdmin/Views/DocumentTemplates/Index.cshtml` | 325 |
| `src/AmbulatoryCarePortal.Presentation/Areas/SuperAdmin/Views/DocumentTemplates/Create.cshtml` | 219 |
| `src/AmbulatoryCarePortal.Presentation/Areas/SuperAdmin/Views/DocumentTemplates/Edit.cshtml` | 230 |
| `src/AmbulatoryCarePortal.Presentation/Areas/ClinicAdmin/Views/ClinicDocuments/Index.cshtml` | 161 |
| `src/AmbulatoryCarePortal.Presentation/Areas/ClinicAdmin/Views/ClinicDocuments/Details.cshtml` | 281 |
| `src/AmbulatoryCarePortal.Presentation/Areas/SuperAdmin/Views/Settings/Index.cshtml` | 259 |
| `src/AmbulatoryCarePortal.Presentation/Areas/SuperAdmin/Views/Settings/_DocumentTemplateSettingsPartial.cshtml` | 49 |
| `src/AmbulatoryCarePortal.Presentation/Views/Shared/_DeleteConfirmation.cshtml` | 23 |
| `src/AmbulatoryCarePortal.Presentation/Views/Shared/_ValidationScriptsPartial.cshtml` | — |

### JavaScript
No dedicated JS files. All JavaScript is inline in `.cshtml` files:
- `Index.cshtml` — ~20 lines (modal event handlers)
- `Create.cshtml` — ~70 lines (AJAX standards loading, file upload preview)
- `Edit.cshtml` — ~60 lines (AJAX standards loading, file upload preview)
- `Settings/Index.cshtml` — ~160 lines (tab switching, form submission, toasts)

### CSS
| Path | Relevant Classes |
|---|---|
| `src/AmbulatoryCarePortal.Presentation/wwwroot/css/site.css` | `.template-hero`, `.template-hero-content`, `.template-hero-title`, `.template-hero-subtitle`, `.template-form-card`, `.form-section`, `.upload-area`, `.standard-card`, `.standard-card-selected`, `.table-toolbar`, `.table-container` |

### Constants
| Path | Lines |
|---|---|
| `src/AmbulatoryCarePortal.Application/Constants/ClinicTypeStandards.cs` | 12 |

### Validators
| Path | Lines |
|---|---|
| `src/AmbulatoryCarePortal.Application/Validators/CreateDocumentTemplateDtoValidator.cs` | 31 |

### EF Configurations
| Path | Lines |
|---|---|
| `src/AmbulatoryCarePortal.Infrastructure/Data/Configurations/DocumentTemplateConfiguration.cs` | 26 |
| `src/AmbulatoryCarePortal.Infrastructure/Data/Configurations/ClinicDocumentConfiguration.cs` | 34 |
| `src/AmbulatoryCarePortal.Infrastructure/Data/Configurations/ClinicDocumentAttachmentConfiguration.cs` | 28 |

### Infrastructure
| Path | Lines |
|---|---|
| `src/AmbulatoryCarePortal.Infrastructure/Data/AppDbContext.cs` | 70 |
| `src/AmbulatoryCarePortal.Infrastructure/Repositories/GenericRepository.cs` | 205 |
| `src/AmbulatoryCarePortal.Infrastructure/UnitOfWork/UnitOfWork.cs` | 38 |
| `src/AmbulatoryCarePortal.Infrastructure/Data/Seed/RolePermissionsSeeder.cs` | 231 |
| `src/AmbulatoryCarePortal.Infrastructure/Data/Seed/DbInitializer.cs` | 65 |

### DI Registration
| Path | Relevant Lines |
|---|---|
| `src/AmbulatoryCarePortal.Application/DependencyInjection/ApplicationServiceExtensions.cs` | Lines 42-43: `IDocumentTemplateService` and `IClinicDocumentService` |
| `src/AmbulatoryCarePortal.Presentation/DependencyInjection/PresentationServiceExtensions.cs` | Line 16: `ITranslationService` |

### Authorization
| Path | Lines |
|---|---|
| `src/AmbulatoryCarePortal.Presentation/Authorization/PermissionRequirement.cs` | 13 |
| `src/AmbulatoryCarePortal.Presentation/Authorization/PermissionAuthorizationHandler.cs` | 18 |
| `src/AmbulatoryCarePortal.Presentation/Authorization/PermissionPolicies.cs` | 115 |

### Enums
| Path | Lines |
|---|---|
| `src/AmbulatoryCarePortal.Domain/Enums/ClinicType.cs` | 7 |
| `src/AmbulatoryCarePortal.Domain/Enums/ClinicDocumentStatus.cs` | 10 |
| `src/AmbulatoryCarePortal.Domain/Enums/SettingCategory.cs` | 11 |
| `src/AmbulatoryCarePortal.Domain/Enums/SettingValueType.cs` | 11 |
| `src/AmbulatoryCarePortal.Domain/Enums/AuditActionType.cs` | 16 |
| `src/AmbulatoryCarePortal.Domain/Enums/DocumentStatus.cs` | (referenced by Policy) |

### Resources
| Path |
|---|
| `src/AmbulatoryCarePortal.Presentation/Resources/translations.en.json` (~81 document-template-related keys) |
| `src/AmbulatoryCarePortal.Presentation/Resources/translations.ar.json` |

### Helpers
| Path | Lines |
|---|---|
| `src/AmbulatoryCarePortal.Presentation/Helpers/TranslationService.cs` | — |
| `src/AmbulatoryCarePortal.Presentation/Helpers/StatusBadgeHelper.cs` | 30 |

### Common
| Path | Lines |
|---|---|
| `src/AmbulatoryCarePortal.Application/Common/PagedResult.cs` | — |

---

**End of Analysis.** This document reflects the codebase as-is, without suggestions or modifications.
