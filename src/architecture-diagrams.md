# CBAHI Ambulatory Care Portal — Architecture & Integration Documentation

---

## 1. Complete Entity Relationship Diagram (ERD)

### 1.1 Core Business Entities

```mermaid
erDiagram
    CLINIC ||--o{ DEPARTMENT : "has"
    CLINIC ||--o{ POLICY_DOCUMENT : "has"
    CLINIC ||--o{ KPI : "has"
    CLINIC ||--o{ CHECKLIST_TEMPLATE : "has"
    CLINIC ||--o{ HR_STAFF : "has"
    CLINIC ||--o{ FORM : "has"
    CLINIC ||--o{ NOTIFICATION : "receives"
    CLINIC ||--o{ AUDIT_TRAIL : "generates"
    CLINIC ||--o{ CLINIC_DOCUMENT : "owns"
    CLINIC ||--o{ APP_USER : "employs"
    CLINIC ||--o{ CHECKLIST_ROUND : "undergoes"

    DEPARTMENT ||--o{ POLICY_DOCUMENT : "categorized_by"
    DEPARTMENT ||--o{ KPI : "assigned_to"
    DEPARTMENT ||--o{ HR_STAFF : "belongs_to"
    DEPARTMENT ||--o{ CHECKLIST_TEMPLATE : "scoped_to"
    DEPARTMENT ||--o{ CHECKLIST_ROUND : "scoped_to"

    POLICY_DOCUMENT ||--o{ EVIDENCE_ATTACHMENT : "has"
    POLICY_DOCUMENT }o--|| DEPARTMENT : "belongs_to"

    KPI ||--o{ KPI_ENTRY : "tracks"

    CHECKLIST_TEMPLATE ||--o{ CHECKLIST_ITEM : "contains"
    CHECKLIST_TEMPLATE ||--o{ CHECKLIST_ROUND : "undergoes"

    CHECKLIST_ITEM ||--o{ CHECKLIST_ANSWER : "receives"

    CHECKLIST_ROUND ||--o{ CHECKLIST_ANSWER : "records"
    CHECKLIST_ROUND }o--|| CHECKLIST_TEMPLATE : "based_on"

    FORM ||--o{ FORM_VERSION : "has"

    HR_STAFF ||--o{ HR_DOCUMENT : "holds"

    DOCUMENT_TEMPLATE ||--o{ CLINIC_DOCUMENT : "instantiated_as"
    CLINIC_DOCUMENT ||--o{ CLINIC_DOCUMENT_ATTACHMENT : "has"

    APP_USER ||--o{ CHECKLIST_ROUND : "executes"
    APP_USER ||--o{ EVIDENCE_ATTACHMENT : "uploads"
    APP_USER ||--o{ HR_DOCUMENT : "uploads"
    APP_USER ||--o{ FORM_VERSION : "uploads"
    APP_USER ||--o{ CLINIC_DOCUMENT_ATTACHMENT : "uploads"
    APP_USER ||--o{ NOTIFICATION : "receives"
    APP_USER ||--o{ AUDIT_TRAIL : "performs"
    APP_USER ||--o{ POLICY_DOCUMENT : "reviews" : "approves"

    AUDIT_TRAIL }o--|| APP_USER : "performed_by"
    NOTIFICATION }o--|| APP_USER : "addressed_to"
```

### 1.2 System Entities (Non-Business)

```mermaid
erDiagram
    SYSTEM_SETTING {
        int Id PK
        string Key UK "Unique"
        string Value "Max 2000"
        string Category "SettingCategory enum"
        string ValueType "SettingValueType enum"
        string Description "Max 500"
        bool IsEncrypted
    }

    AUDIT_TRAIL {
        int Id PK
        int ClinicId FK
        string ActionType "AuditActionType enum"
        int TargetObjectId
        string TargetObjectType "Max 100"
        string Description "Max 1000"
        string OldValues "Max 2000"
        string NewValues "Max 2000"
        datetime ActionDate
        string UserId FK "nullable"
        string IpAddress "Max 50"
    }

    NOTIFICATION {
        int Id PK
        int ClinicId FK
        string Title "Max 255"
        string Message "Max 1000"
        string MessageAr "Max 1000"
        string NotificationType "NotificationType enum"
        int TargetObjectId
        string TargetObjectType "Max 100"
        bool IsRead
        datetime ReadAt "nullable"
        string UserId FK "nullable"
    }
```

### 1.3 BaseEntity Audit Columns (All Entities)

```mermaid
erDiagram
    BASE_ENTITY {
        int Id PK
        datetime CreatedAt
        datetime UpdatedAt "nullable"
        bool IsDeleted "soft delete"
        string CreatedBy "nullable, free text"
        string UpdatedBy "nullable, free text"
    }
```

---

## 2. Entity Dependencies

### 2.1 Dependency Hierarchy (Topological Order)

```
Level 0 (Roots):
  ├── SystemSetting          (no dependencies)
  ├── DocumentTemplate       (no dependencies)

Level 1 (Depend on Clinic):
  ├── Clinic                 (no FKs, all other entities reference it)
  ├── Department             → Clinic (Cascade)
  ├── ChecklistTemplate       → Clinic (Cascade), → Department (Restrict)
  ├── KPI                    → Clinic (Cascade), → Department (Restrict)
  ├── HrStaff                → Clinic (Cascade), → Department (Restrict)
  ├── Form                   → Clinic (Cascade)
  ├── AppUser                → Clinic (Restrict)

Level 2 (Depend on Level 1):
  ├── PolicyDocument         → Clinic (Cascade), → Department (Restrict)
  ├── ChecklistRound         → Clinic (Restrict), → Department (Restrict),
  │                            → ChecklistTemplate (Cascade), → AppUser×2 (Restrict)
  ├── ChecklistItem          → ChecklistTemplate (Cascade)
  ├── ClinicDocument         → Clinic (Restrict), → DocumentTemplate (Restrict)

Level 3 (Leaf Entities):
  ├── EvidenceAttachment     → PolicyDocument (Cascade), → AppUser (Restrict)
  ├── KPIEntry               → KPI (Cascade)
  ├── ChecklistAnswer        → ChecklistRound (Restrict), → ChecklistItem (Restrict),
  │                            → AppUser (Restrict)
  ├── FormVersion            → Form (Cascade), → AppUser (Restrict)
  ├── HrDocument             → HrStaff (Cascade), → AppUser (Restrict)
  ├── ClinicDocumentAttachment → ClinicDocument (Cascade), → AppUser (Restrict)

Shared Services (Cross-Cutting):
  ├── AuditTrail             → Clinic (Cascade), → AppUser (Restrict)
  ├── Notification           → Clinic (Cascade), → AppUser (Cascade)
```

### 2.2 Entity Dependency Graph (Delete Behavior Annotations)

```mermaid
flowchart TD
    %% Level 0
    SS[SystemSetting]

    %% Level 1
    C[Clinic]
    DT[DocumentTemplate]

    %% Level 2 - Direct Clinic children
    D[Department]
    DEPT_DELETE["Cascade"] --> D
    C -->|Cascade| D

    CT[ChecklistTemplate]
    C -->|Cascade| CT

    K[KPI]
    C -->|Cascade| K

    HS[HrStaff]
    C -->|Cascade| HS

    F[Form]
    C -->|Cascade| F

    AU[AppUser]
    C -->|Restrict| AU

    %% Level 2 - Other
    PD[PolicyDocument]
    C -->|Cascade| PD
    D -->|Restrict| PD

    CR[ChecklistRound]
    C -->|Restrict| CR
    D -->|Restrict| CR
    CT -->|Cascade| CR
    AU -->|Restrict| CR

    CI[ChecklistItem]
    CT -->|Cascade| CI

    CD[ClinicDocument]
    C -->|Restrict| CD
    DT -->|Restrict| CD

    %% Level 3 - Leaves
    EA[EvidenceAttachment]
    PD -->|Cascade| EA
    AU -->|Restrict| EA

    KE[KPIEntry]
    K -->|Cascade| KE

    CA[ChecklistAnswer]
    CR -->|Cascade| CA
    CI -->|Cascade| CA
    AU -->|Restrict| CA

    FV[FormVersion]
    F -->|Cascade| FV
    AU -->|Restrict| FV

    HD[HrDocument]
    HS -->|Cascade| HD
    AU -->|Restrict| HD

    CDA[ClinicDocumentAttachment]
    CD -->|Cascade| CDA
    AU -->|Restrict| CDA

    %% Cross-cutting
    AT[AuditTrail]
    C -->|Cascade| AT
    AU -->|Restrict| AT

    N[Notification]
    C -->|Cascade| N
    AU -->|Cascade| N

    %% Styling
    classDef root fill:#e1f5e1,stroke:#2e7d32
    classDef level1 fill:#e3f2fd,stroke:#1565c0
    classDef level2 fill:#fff3e0,stroke:#e65100
    classDef level3 fill:#fce4ec,stroke:#c62828
    classDef cross fill:#f3e5f5,stroke:#6a1b9a

    class SS,DT root
    class C,D,CT,K,HS,F,AU level1
    class PD,CR,CI,CD level2
    class EA,KE,CA,FV,HD,CDA level3
    class AT,N cross
```

---

## 3. Aggregate Boundaries

### 3.1 Aggregate Definitions (DDD)

```mermaid
flowchart LR
    subgraph "Aggregate: Clinic"
        direction TB
        C[Clinic Root]
        D[Department]
    end

    subgraph "Aggregate: Policy"
        direction TB
        PD[PolicyDocument Root]
        EA[EvidenceAttachment]
    end

    subgraph "Aggregate: KPI"
        direction TB
        K[KPI Root]
        KE[KPIEntry]
    end

    subgraph "Aggregate: Checklist"
        direction TB
        CT[ChecklistTemplate Root]
        CI[ChecklistItem]
        CR[ChecklistRound]
        CA[ChecklistAnswer]
    end

    subgraph "Aggregate: Form"
        direction TB
        F[Form Root]
        FV[FormVersion]
    end

    subgraph "Aggregate: HR"
        direction TB
        HS[HrStaff Root]
        HD[HrDocument]
    end

    subgraph "Aggregate: ClinicDocument"
        direction TB
        CD[ClinicDocument Root]
        CDA[ClinicDocumentAttachment]
    end

    subgraph "Shared: Identity"
        AU[AppUser Root]
    end

    subgraph "Shared: DocumentTemplate"
        DT[DocumentTemplate Root]
    end

    subgraph "Shared: Cross-Cutting"
        AT[AuditTrail]
        N[Notification]
        SS[SystemSetting]
    end

    %% Reference relationships between aggregates (by ID only)
    C -.->|FK: ClinicId| PD
    C -.->|FK: ClinicId| K
    C -.->|FK: ClinicId| CT
    C -.->|FK: ClinicId| HS
    C -.->|FK: ClinicId| F
    C -.->|FK: ClinicId| CD
    C -.->|FK: ClinicId| AT
    C -.->|FK: ClinicId| N
    C -.->|FK: ClinicId| AU
    D -.->|FK: DepartmentId| PD
    D -.->|FK: DepartmentId| K
    D -.->|FK: DepartmentId| CT
    D -.->|FK: DepartmentId| HS
    DT -.->|FK: DocumentTemplateId| CD
    AU -.->|FK: UploadedByUserId| EA
    AU -.->|FK: UploadedByUserId| CDA
    AU -.->|FK: UploadedByUserId| FV
    AU -.->|FK: UploadedByUserId| HD
    AU -.->|FK: ExecutedByUserId| CR
    AU -.->|FK: ApprovedByUserId| CR
    AU -.->|FK: OwnerId| CA
    AU -.->|FK: UserId| AT
    AU -.->|FK: UserId| N
```

### 3.2 Aggregate Design Rules

| Aggregate | Root Entity | Invariants | Repository | Notes |
|-----------|-------------|-----------|------------|-------|
| Clinic | Clinic | Name unique, LicenseNumber unique | GenericRepository<Clinic> | Departments managed inside aggregate |
| Policy | PolicyDocument | (ClinicId, StandardCode) unique, version sequence | GenericRepository<PolicyDocument> | EvidenceAttachment part of aggregate |
| KPI | KPI | (KPIId, PeriodYear, PeriodMonth) unique | GenericRepository<KPI> | KPIEntry part of aggregate |
| Checklist | ChecklistTemplate | Template scoped to Clinic+Department | GenericRepository<ChecklistTemplate> | Items, Rounds, Answers all inside |
| Form | Form | Version increments per Form | GenericRepository<Form> | VersionHistory managed inside |
| HR | HrStaff | Staff scoped to Clinic+Department | GenericRepository<HrStaff> | Documents managed inside |
| ClinicDocument | ClinicDocument | (ClinicId, DocumentTemplateId) unique | GenericRepository<ClinicDocument> | Attachments managed inside |
| DocumentTemplate | DocumentTemplate | StandardCode unique | GenericRepository<DocumentTemplate> | Standalone, no children |
| SystemSetting | SystemSetting | Key unique | GenericRepository<SystemSetting> | Standalone, no children |

---

## 4. Database Schema Documentation

### 4.1 Complete Table Inventory

| # | Table Name | Schema | Engine | Type | Est. Row Count | Audit Columns |
|---|------------|--------|--------|------|---------------|--------------|
| 1 | `AspNetUsers` | dbo | Identity | Identity | Medium | No (Identity internal) |
| 2 | `AspNetRoles` | dbo | Identity | Identity | Low | No |
| 3 | `AspNetUserRoles` | dbo | Identity | Identity | Medium | No |
| 4 | `AspNetRoleClaims` | dbo | Identity | Identity | Medium | No |
| 5 | `AspNetUserClaims` | dbo | Identity | Identity | Medium | No |
| 6 | `AspNetUserLogins` | dbo | Identity | Identity | Low | No |
| 7 | `AspNetUserTokens` | dbo | Identity | Identity | Low | No |
| 8 | `Clinics` | dbo | Business | Master | Low | Yes (BaseEntity) |
| 9 | `Departments` | dbo | Business | Master | Low | Yes |
| 10 | `PolicyDocuments` | dbo | Business | Transactional | Medium | Yes |
| 11 | `EvidenceAttachments` | dbo | Business | Transactional | Medium | Yes |
| 12 | `KPIs` | dbo | Business | Master | Low | Yes |
| 13 | `KPIEntries` | dbo | Business | Transactional | Medium-High | Yes |
| 14 | `ChecklistTemplates` | dbo | Business | Master | Low | Yes |
| 15 | `ChecklistItems` | dbo | Business | Master | Medium | Yes |
| 16 | `ChecklistRounds` | dbo | Business | Transactional | Medium | Yes |
| 17 | `ChecklistAnswers` | dbo | Business | Transactional | Medium-High | Yes |
| 18 | `Forms` | dbo | Business | Master | Low | Yes |
| 19 | `FormVersions` | dbo | Business | Transactional | Low-Medium | Yes |
| 20 | `HrStaffs` | dbo | Business | Master | Medium | Yes |
| 21 | `HrDocuments` | dbo | Business | Transactional | Medium-High | Yes |
| 22 | `Notifications` | dbo | Business | Transactional | High | Yes |
| 23 | `AuditTrails` | dbo | Business | Transactional | High | Yes |
| 24 | `DocumentTemplates` | dbo | Business | Master | Low | Yes |
| 25 | `ClinicDocuments` | dbo | Business | Transactional | Medium | Yes |
| 26 | `ClinicDocumentAttachments` | dbo | Business | Transactional | Medium | Yes |
| 27 | `SystemSettings` | dbo | Business | Master | Low | Yes |
| 28 | `__EFMigrationsHistory` | dbo | System | Metadata | Low | No |

### 4.2 Column Naming Convention

- **Primary Keys**: `Id` (int, auto-increment) — all entities
- **Foreign Keys**: `{ReferencedEntity}Id` (e.g., `ClinicId`, `DepartmentId`)
- **Audit Columns**: `CreatedAt`, `UpdatedAt`, `IsDeleted`, `CreatedBy`, `UpdatedBy`
- **Enum Storage**: All enums stored as strings (e.g., `Clinics.ClinicType = 'AMB'`)

### 4.3 Index Coverage

| Table | Index | Columns | Type | Unique | Filtered |
|-------|-------|---------|------|--------|----------|
| Clinics | PK_Clinics | Id | Clustered | Yes | No |
| Clinics | IX_Clinics_Name | Name | Non-clustered | Yes | No |
| Clinics | IX_Clinics_LicenseNumber | LicenseNumber | Non-clustered | Yes | No |
| Departments | PK_Departments | Id | Clustered | Yes | No |
| Departments | IX_Departments_ClinicId_Code | ClinicId, Code | Non-clustered | Yes | No |
| PolicyDocuments | PK_PolicyDocuments | Id | Clustered | Yes | No |
| PolicyDocuments | IX_PolicyDocuments_ClinicId_StandardCode | ClinicId, StandardCode | Non-clustered | Yes | No |
| KPIs | PK_KPIs | Id | Clustered | Yes | No |
| KPIEntries | PK_KPIEntries | Id | Clustered | Yes | No |
| KPIEntries | IX_KPIEntries_KPIId_PeriodYear_PeriodMonth | KPIId, PeriodYear, PeriodMonth | Non-clustered | Yes | No |
| DocumentTemplates | PK_DocumentTemplates | Id | Clustered | Yes | No |
| DocumentTemplates | IX_DocumentTemplates_StandardCode | StandardCode | Non-clustered | Yes | No |
| ClinicDocuments | PK_ClinicDocuments | Id | Clustered | Yes | No |
| ClinicDocuments | IX_ClinicDocuments_ClinicId_DocumentTemplateId | ClinicId, DocumentTemplateId | Non-clustered | Yes | No |
| AuditTrails | PK_AuditTrails | Id | Clustered | Yes | No |
| AuditTrails | IX_AuditTrails_ClinicId_ActionDate | ClinicId, ActionDate DESC | Non-clustered | No | No |
| SystemSettings | PK_SystemSettings | Id | Clustered | Yes | No |
| SystemSettings | IX_SystemSettings_Key | Key | Non-clustered | Yes | No |

### 4.4 Entity Column Specifications

```mermaid
flowchart LR
    subgraph "Clinics"
        C1[Id: int PK]
        C2[Name: nvarchar(255) NOT NULL]
        C3[NameAr: nvarchar(255)]
        C4[CityEn: nvarchar(100)]
        C5[CityAr: nvarchar(100)]
        C6[ClinicType: nvarchar(50)] 
        C7[LogoPath: nvarchar(500)]
        C8[LicenseNumber: nvarchar(100)]
        C9[LicenseExpiry: datetime2]
        C10[IsActive: bit]
        C11[ComplianceScore: decimal(5,2)]
    end

    subgraph "PolicyDocuments"
        P1[Id: int PK]
        P2[Title: nvarchar(255)]
        P3[TitleAr: nvarchar(255)]
        P4[StandardCode: nvarchar(50)]
        P5[DepartmentId: int FK]
        P6[ClinicId: int FK]
        P7[OfficialPdfPath: nvarchar(500)]
        P8[DocumentStatus: nvarchar(50)]
        P9[ExpiryDate: datetime2]
        P10[VersionNumber: int]
    end

    subgraph "ChecklistTemplates"
        T1[Id: int PK]
        T2[Name: nvarchar(255)]
        T3[NameAr: nvarchar(255)]
        T4[Description: nvarchar(max)]
        T5[ClinicId: int FK]
        T6[DepartmentId: int FK]
        T7[Frequency: nvarchar(50)]
        T8[IsActive: bit]
    end

    subgraph "HR_STAFF"
        H1[Id: int PK]
        H2[FullNameEn: nvarchar(255)]
        H3[FullNameAr: nvarchar(255)]
        H4[StaffType: nvarchar(50)]
        H5[ClinicId: int FK]
        H6[DepartmentId: int FK]
        H7[NationalId: nvarchar(100)]
        H8[Email: nvarchar(255)]
        H9[Phone: nvarchar(20)]
        H10[PositionTitle: nvarchar(max)]
        H11[JoinDate: datetime2]
        H12[IsActive: bit]
    end
```

---

## 5. Module Dependency Diagram

### 5.1 Module Dependencies by Layer

```mermaid
flowchart TB
    subgraph "Presentation Layer (AmbulatoryCarePortal.Presentation)"
        direction TB
        SA[SuperAdmin Module]
        CA[ClinicAdmin Module]
        AC[Account Module]
        SH[Shared UI]
        
        SA -->|uses| US[UserManagement]
        SA -->|uses| CM[ClinicManagement]
        SA -->|uses| DT[DocumentTemplatesMgmt]
        SA -->|uses| SYS[SystemSettings]
        
        CA -->|uses| PM[PolicyManagement]
        CA -->|uses| KPI_M[KPIManagement]
        CA -->|uses| CLM[ChecklistManagement]
        CA -->|uses| HRM[HRManagement]
        CA -->|uses| FM[FormsManagement]
        CA -->|uses| CDM[ClinicDocManagement]
        CA -->|uses| NTF[Notifications]
        CA -->|uses| RPT[Reporting]
        CA -->|uses| DSH[Dashboard]
    end

    subgraph "Application Layer"
        direction TB
        APP_SVC[Application Services Layer]
        
        APP_SVC -->|depends on| DOMAIN_INTERFACES[Domain Interfaces]
        APP_SVC -->|implements| APP_INTERFACES[Application Interfaces]
    end

    subgraph "Infrastructure Layer"
        direction TB
        INF_SVC[Infrastructure Services]
        REPO[GenericRepository + UnitOfWork]
        EF[EF Core DbContext]
        ENC[DataProtectionEncryption]
        EMAIL[SMTP Email Service]
        
        INF_SVC --> REPO
        INF_SVC --> ENC
        INF_SVC --> EMAIL
        REPO --> EF
    end

    subgraph "Data Layer"
        SQL[(SQL Server Database)]
        FS[(File System - wwwroot/uploads/)]
    end

    subgraph "Domain Layer"
        ENT[Entities]
        ENUM[Enums]
        BASE[BaseEntity]
    end

    %% Cross-layer dependencies
    SA --> APP_SVC
    CA --> APP_SVC
    AC --> APP_SVC
    
    APP_SVC -.-> ENT
    APP_SVC -.-> ENUM
    
    REPO -.->|IEntityTypeConfiguration| EF
    EF --> SQL
    
    EMAIL -->|SMTP| MailKit
    
    %% All upload paths
    PM --> FS
    CLM --> FS
    KPI_M --> FS
    HRM --> FS
    FM --> FS
    CDM --> FS
    SA --> FS

    classDef pres fill:#e3f2fd,stroke:#1565c0
    classDef app fill:#e8f5e9,stroke:#2e7d32
    classDef infra fill:#fff3e0,stroke:#e65100
    classDef data fill:#fce4ec,stroke:#c62828
    classDef domain fill:#f3e5f5,stroke:#6a1b9a
    
    class SA,CA,AC,SH,US,CM,DT,SYS,PM,KPI_M,CLM,HRM,FM,CDM,NTF,RPT,DSH pres
    class APP_SVC,APP_INTERFACES app
    class INF_SVC,REPO,EF,ENC,EMAIL infra
    class SQL,FS data
    class ENT,ENUM,BASE,DOMAIN_INTERFACES domain
```

### 5.2 Cross-Module Service Dependencies

```mermaid
flowchart LR
    %% Define modules
    SV[SettingsService]
    EM[EmailService]
    BJ[BackgroundJobService]
    NS[NotificationService]
    ANS[AdvancedNotificationService]
    AS[AuditService]
    CS[ClinicService]
    PS[PolicyDocumentService]
    KS[KPIService]
    CLS[ChecklistService]
    HS[HrService]
    FS[FormService]
    DS[DocumentTemplateService]
    CDS[ClinicDocumentService]
    RS[ReportingService]
    ANL[AnalyticsService]
    SRC[AdvancedSearchService]
    BOS[BulkOperationsService]
    DES[DataExportService]
    CCS[ComplianceCalendarService]

    %% Dependencies
    EM -->|reads SMTP settings| SV
    BJ -->|reads thresholds| SV
    BJ -->|sends alerts| EM
    BJ -->|creates notifications| NS
    ANS -->|creates notifications| NS
    CCS -->|reads compliance data| PS
    CCS -->|reads compliance data| KS
    CCS -->|reads compliance data| CLS
    CCS -->|reads compliance data| HS
    CCS -->|reads compliance data| CDS
    RS -->|reads all data| PS
    RS -->|reads all data| KS
    RS -->|reads all data| CLS
    RS -->|reads all data| HS
    RS -->|reads all data| AS
    RS -->|sends reports| EM
    ANL -->|reads data| PS
    ANL -->|reads data| KS
    ANL -->|reads data| CLS
    ANL -->|reads data| HS
    SRC -->|reads all data| PS
    SRC -->|reads all data| KS
    SRC -->|reads all data| CLS
    SRC -->|reads all data| HS
    SRC -->|reads all data| FS
    SRC -->|reads all data| CDS
    BOS -->|bulk operations| PS
    BOS -->|bulk operations| KS
    BOS -->|bulk operations| HS
    DES -->|exports data| PS
    DES -->|exports data| KS
    DES -->|exports data| HS
    DES -->|exports data| AS
    PS -->|audit| AS
    PS -->|notify| NS
    KS -->|audit| AS
    CS -->|audit| AS
    CLS -->|audit| AS
    HS -->|audit| AS
    FS -->|audit| AS
    CDS -->|audit| AS
    DS -->|audit| AS

    classDef settings fill:#f3e5f5
    classDef email fill:#e1f5fe
    classDef bg fill:#fff9c4
    classDef notify fill:#ffe0b2
    classDef audit fill:#d7ccc8
    classDef core fill:#c8e6c9
    classDef report fill:#b2dfdb
    classDef cross fill:#e1bee7

    class SV settings
    class EM email
    class BJ bg
    class NS,ANS notify
    class AS audit
    class PS,KS,CLS,HS,FS,DS,CDS,CS core
    class RS,ANL,CCS report
    class SRC,BOS,DES cross
```

---

## 6. Service Dependency Diagram

### 6.1 Full Service Injection Graph

```mermaid
flowchart TD
    %% Controllers
    SAC1[SuperAdmin: DashboardController]
    SAC2[SuperAdmin: UserManagementController]
    SAC3[SuperAdmin: DocumentTemplatesController]
    SAC4[SuperAdmin: SettingsController]

    CAC1[ClinicAdmin: DashboardController]
    CAC2[ClinicAdmin: PolicyManagementController]
    CAC3[ClinicAdmin: PolicyDocumentsController]
    CAC4[ClinicAdmin: KPIManagementController]
    CAC5[ClinicAdmin: ChecklistManagementController]
    CAC6[ClinicAdmin: HRManagementController]
    CAC7[ClinicAdmin: FormsController]
    CAC8[ClinicAdmin: ClinicDocumentsController]
    CAC9[ClinicAdmin: DepartmentManagementController]
    CAC10[ClinicAdmin: NotificationsController]
    CAC11[ClinicAdmin: ReportingController]

    RootC1[AccountController]
    RootC2[HomeController]

    %% Services
    CS[ClinicService]
    PDS[PolicyDocumentService]
    KS[KPIService]
    CHS[ChecklistService]
    HS[HrService]
    FS[FormService]
    DTS[DocumentTemplateService]
    CDS[ClinicDocumentService]
    DS[DepartmentService<br/>via ClinicService]
    NTS[NotificationService]
    AUS[AuditService]
    RS[ReportingService]
    ANS[AnalyticsService]
    EML[EmailService]
    SRS[AdvancedSearchService]
    BOS[BulkOperationsService]
    DES[DataExportService]
    SVS[SettingsService]
    CCS[ComplianceCalendarService]
    BJS[BackgroundJobService]

    %% Infrastructure Cross-Cutting
    UOW[IUnitOfWork]
    ENC[IEncryptionService]
    LOC[ITranslationService]
    CAF[ClinicAuthorizationFilter]

    %% Injections
    SAC1 --> CS
    SAC1 --> AUS
    SAC1 --> LOC
    
    SAC2 --> AUS
    SAC2 --> LOC
    
    SAC3 --> DTS
    SAC3 --> LOC
    
    SAC4 --> SVS
    SAC4 --> EML
    SAC4 --> LOC

    CAC1 --> CS
    CAC1 --> PDS
    CAC1 --> KS
    CAC1 --> CHS
    CAC1 --> HS
    CAC1 --> CDS
    CAC1 --> CCS
    CAC1 --> AUS
    CAC1 --> ANS
    CAC1 --> LOC

    CAC2 --> PDS
    CAC2 --> AUS
    CAC2 --> LOC

    CAC3 --> CDS
    CAC3 --> LOC

    CAC4 --> KS
    CAC4 --> AUS
    CAC4 --> LOC

    CAC5 --> CHS
    CAC5 --> AUS
    CAC5 --> LOC

    CAC6 --> HS
    CAC6 --> AUS
    CAC6 --> LOC

    CAC7 --> FS
    CAC7 --> LOC

    CAC8 --> CDS
    CAC8 --> LOC

    CAC9 --> CS
    CAC9 --> LOC

    CAC10 --> NTS
    CAC10 --> LOC

    CAC11 --> RS
    CAC11 --> ANS
    CAC11 --> AUS
    CAC11 --> DES
    CAC11 --> EML
    CAC11 --> LOC

    RootC1 --> LOC
    RootC2 --> LOC

    %% Service -> Service
    EML --> SVS
    BJS --> EML
    BJS --> SVS
    BJS --> NTS
    RS --> PDS
    RS --> KS
    RS --> CHS
    RS --> HS
    RS --> AUS
    RS --> EML
    ANS --> PDS
    ANS --> KS
    ANS --> CHS
    ANS --> HS
    CCS --> PDS
    CCS --> KS
    CCS --> CHS
    CCS --> HS
    CCS --> CDS
    SRS --> PDS
    SRS --> KS
    SRS --> CHS
    SRS --> HS
    SRS --> FS
    SRS --> CDS
    BOS --> PDS
    BOS --> KS
    BOS --> HS
    DES --> PDS
    DES --> KS
    DES --> HS
    DES --> AUS

    %% All services -> UOW
    CS --> UOW
    PDS --> UOW
    KS --> UOW
    CHS --> UOW
    HS --> UOW
    FS --> UOW
    DTS --> UOW
    CDS --> UOW
    NTS --> UOW
    AUS --> UOW
    SVS --> UOW
    SVS --> ENC

    classDef controller fill:#e3f2fd,stroke:#1565c0
    classDef service fill:#e8f5e9,stroke:#2e7d32
    classDef infra fill:#fff3e0,stroke:#e65100
    classDef cross fill:#f3e5f5,stroke:#6a1b9a
    
    class SAC1,SAC2,SAC3,SAC4,CAC1,CAC2,CAC3,CAC4,CAC5,CAC6,CAC7,CAC8,CAC9,CAC10,CAC11,RootC1,RootC2 controller
    class PDS,KS,CHS,HS,FS,DTS,CDS,CS,NTS,AUS,RS,ANS,EML,SRS,BOS,DES,SVS,CCS,BJS service
    class UOW,ENC infra
    class LOC,CAF cross
```

### 6.2 Service Lifecycle Diagram

```mermaid
flowchart LR
    subgraph "Request Scope (Scoped)"
        CTRL[Controller]
        SVC[Application Service]
        UOW[UnitOfWork]
        REPO[GenericRepository]
        DB[AppDbContext]
        ENC[EncryptionService]
        CTRL --> SVC --> UOW --> REPO --> DB
        SVC -.-> ENC
    end

    subgraph "Singleton Scope"
        HOST[NotificationBackgroundService]
        LOG[Serilog Logger]
        CFG[IConfiguration]
    end

    subgraph "Transient/Per-Call"
        SV[SmtpClient]
        EMAIL[EmailMessage]
        EXCEL[DataExport Bytes]
    end

    HOST -->|Creates Scope| SCOPED(Services resolved per iteration)
    SCOPED --> SVC
```

---

## 7. Request Flow Diagram

### 7.1 Standard HTTP Request Lifecycle

```mermaid
sequenceDiagram
    participant Browser
    participant IIS as Kestrel/IIS
    participant EMW as ExceptionMiddleware
    participant AMW as AuditMiddleware
    participant ROUTE as Routing
    participant AUTH as Authentication
    participant AUTHZ as Authorization
    participant CAMW as ClinicAccessMiddleware
    participant CTX as HttpContext
    participant CTRL as Controller
    participant SVC as Application Service
    participant UOW as UnitOfWork
    participant REPO as GenericRepository
    participant DB as AppDbContext
    participant SQL as SQL Server

    Browser->>IIS: HTTP Request
    IIS->>EMW: Pipeline Start
    EMW->>AMW: Pass Through
    AMW->>ROUTE: Pass Through
    
    ROUTE->>AUTH: Matched Route
    Note over AUTH: Cookie Auth<br/>Read Identity Cookie
    AUTH->>AUTHZ: User Principal
    
    AUTHZ->>CAMW: [Authorize] Check
    Note over AUTHZ: Role Check<br/>SuperAdmin/ClinicAdmin/ClinicViewer
    
    CAMW->>CTX: Pass Through
    Note over CAMW: ClinicId Claim<br/>vs Route Parameter
    
    alt GET Request
        CTX->>CTRL: HttpContext + Route Data
        CTRL->>CTRL: Model Binding (if any)
        CTRL->>SVC: Call Service Method
        SVC->>UOW: Get Repository
        UOW->>REPO: Repository<T>
        REPO->>DB: LINQ Query
        DB->>SQL: SQL Query
        SQL-->>DB: Result Set
        DB-->>REPO: Materialized Entities
        REPO-->>UOW: Entities
        UOW-->>SVC: Entities
        SVC->>SVC: AutoMapper .ProjectTo() / .Map()
        SVC-->>CTRL: DTOs / ViewModels
        CTRL-->>CTX: ViewResult / PartialView
        CTX-->>AMW: Response
        AMW-->>EMW: Response
        EMW-->>IIS: Response
        IIS-->>Browser: HTML/JSON
    else POST Request
        CTX->>CTRL: HttpContext + Form Data
        Note over CTX: CSRF Token Validation
        CTRL->>CTRL: Model Binding + Validation
        CTRL->>SVC: Call Service Method
        SVC->>UOW: Begin Transaction
        UOW->>REPO: Add/Update/Delete
        REPO->>DB: SaveChanges
        DB->>SQL: INSERT/UPDATE/DELETE
        SQL-->>DB: Success
        DB-->>REPO: Rows Affected
        REPO-->>UOW: Result
        UOW->>UOW: SaveChanges (Commit)
        UOW-->>SVC: Success
        SVC-->>CTRL: Result / DTO
        CTRL-->>CTX: Redirect / JSON
        CTX-->>AMW: Response
        Note over AMW: Logs to AuditTrail<br/>(Create/Update/Delete/Upload)
        AMW-->>EMW: Response
        EMW-->>IIS: Response
        IIS-->>Browser: Redirect / JSON
    end

    alt Exception Thrown
        EMW->>EMW: Catch Exception
        EMW->>EMW: Log Error
        EMW-->>IIS: Error Response (400/401/500)
        IIS-->>Browser: Error JSON
    end
```

### 7.2 Background Job Flow

```mermaid
sequenceDiagram
    participant HOST as NotificationBackgroundService
    participant SCOPE as DI Scope
    participant BJS as BackgroundJobService
    participant SVC as Various Services
    participant UOW as UnitOfWork
    participant SQL as SQL Server
    participant EMAIL as EmailService
    participant SMTP as SMTP (Gmail)

    Note over HOST: Startup: HostedService.StartAsync
    HOST->>HOST: ExecuteAsync(CancellationToken)
    
    loop Every N Minutes (Configurable: 60)
        HOST->>SCOPE: CreateScope()
        SCOPE->>BJS: Resolve IBackgroundJobService
        
        par Run Document Expiry Check
            BJS->>UOW: Get HrDocument Repository
            UOW->>SQL: SELECT HrDocs WHERE ExpiryDate <= WarningDays
            SQL-->>UOW: Expiring Documents
            UOW-->>BJS: Expiring HrDocuments
            BJS->>BJS: Group by Staff
            BJS->>EMAIL: SendExpiryReminderAsync()
            EMAIL->>SMTP: Send Email
            SMTP-->>EMAIL: OK
        and Run Compliance Alerts
            BJS->>UOW: Get Clinics with >5 Missing or >3 Expired
            UOW-->>BJS: Non-Compliant Clinics
            BJS->>SVC: Create Notifications
        and Run Checklist Reminders
            BJS->>UOW: Get Overdue ChecklistTemplates
            UOW-->>BJS: Overdue Templates
            BJS->>SVC: Create Notification per Template
        end
        
        SCOPE->>HOST: Dispose
        HOST->>HOST: Task.Delay(intervalMinutes)
    end
    
    Note over HOST: Shutdown: StopAsync
    HOST->>HOST: Cancel Token -> Exit Loop
```

### 7.3 Login Flow

```mermaid
sequenceDiagram
    participant Browser
    participant CTRL as AccountController
    participant Auth as ASP.NET Identity
    participant DB as AppDbContext
    participant SQL as SQL Server
    participant Audit as AuditService

    Browser->>CTRL: GET /Account/Login
    CTRL-->>Browser: Login.cshtml

    Browser->>CTRL: POST /Account/Login (email, password, rememberMe)
    Note over CTRL: ValidateAntiForgeryToken
    
    CTRL->>Auth: PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure:true)
    
    Auth->>DB: FindByEmailAsync
    DB-->>Auth: AppUser
    
    alt User Not Found
        Auth-->>CTRL: Failed
        CTRL-->>Browser: ModelState Error
    else Account Locked Out
        Auth-->>CTRL: LockedOut
        CTRL-->>Browser: Lockout View
    else 2FA Required (not configured)
        Auth-->>CTRL: TwoFactorRequired
        CTRL-->>Browser: (not implemented)
    else Success
        Auth->>Auth: SignInAsync (Create ClaimsPrincipal)
        Note over Auth: Custom ClinicClaimsPrincipalFactory<br/>injects ClinicId claim
        Auth->>DB: Update LastLoginAt
        Auth-->>CTRL: Success
        
        CTRL->>Audit: LogActionAsync (Login)
        Audit->>DB: INSERT AuditTrail
        DB-->>Audit: OK
        
        CTRL-->>Browser: Redirect to ReturnUrl / Dashboard
    end
```

---

## 8. Suggested CAPA Module Integration Points

### 8.1 CAPA (Corrective and Preventive Action) — Module Concept

CAPA is a quality management process where non-conformances trigger formal investigation, root cause analysis, corrective action planning, implementation, and effectiveness verification. This is a natural extension for a compliance platform.

### 8.2 Trigger Sources (Where CAPA Records Would Be Created)

```mermaid
flowchart TB
    subgraph "CAPA Trigger Sources"
        direction TB
        P1[Policy Expired / Missing]
        P2[KPI Below Threshold]
        P3[Checklist Non-Compliance]
        P4[HR Document Expired]
        P5[Audit Finding]
        P6[ClinicDocument Expired]
        P7[Manual CAPA Entry]
    end

    subgraph "CAPA Engine"
        CAPA[CAPA Record]
        CAPA --> RCA[Root Cause Analysis]
        RCA --> CAP[Corrective Action Plan]
        CAP --> IMP[Implementation]
        IMP --> VER[Effectiveness Verification]
        VER --> CLOSE[CAPA Closure]
    end

    subgraph "Existing Module Integration Points"
        PM[PolicyManagement: Approve/Reject]
        KM[KPIManagement: EnterData ViewAnalytics]
        CM[ChecklistManagement: ExecuteRound]
        HM[HRManagement: NonCompliantStaff]
        AM[Audit: SuperAdmin AuditLog]
        CLM[ClinicDocuments: UpdateStatus]
        REP[Reporting: ComplianceReport]
        DSH[Dashboard: ComplianceScore]
    end

    P1 -->|Auto-create CAPA| CAPA
    P2 -->|Threshold breach triggers| CAPA
    P3 -->|Failed items trigger| CAPA
    P4 -->|Expiry warning triggers| CAPA
    P5 -->|Audit findings trigger| CAPA
    P6 -->|Expiry triggers| CAPA
    P7 -->|Manual entry| CAPA

    CAPA -->|Integrates into| REP
    CAPA -->|Affects| DSH
    CAPA -->|Shown in audit| AM
    PM -.->|Review/Reject action| P1
    KM -.->|KPI entry below target| P2
    CM -.->|Checklist execution failures| P3
    HM -.->|Non-compliant staff| P4
    AM -.->|Audit trail entries| P5
    CLM -.->|Document status changes| P6

    classDef source fill:#fce4ec,stroke:#c62828
    classDef capa fill:#e8f5e9,stroke:#2e7d32
    classDef exist fill:#e3f2fd,stroke:#1565c0
    classDef report fill:#fff3e0,stroke:#e65100

    class P1,P2,P3,P4,P5,P6,P7 source
    class CAPA,RCA,CAP,IMP,VER,CLOSE capa
    class PM,KM,CM,HM,AM,CLM exist
    class REP,DSH report
```

### 8.3 Suggested CAPA Entity Model

```mermaid
erDiagram
    CAPA_RECORD {
        int Id PK
        int ClinicId FK
        string Title
        string Description
        string SourceType "PolicyExpiry|KPIBreach|ChecklistFail|HRExpiry|AuditFinding|Manual"
        int SourceObjectId "FK to originating record"
        string Severity "Critical|Major|Minor|Observation"
        string Status "Open|Investigation|ActionPlanned|Implementation|Verification|Closed"
        datetime IdentifiedAt
        string IdentifiedBy FK "UserId"
        datetime TargetClosureDate
        datetime ActualClosureDate
    }

    CAPA_ROOT_CAUSE {
        int Id PK
        int CapaRecordId FK
        string Category "Process|Training|System|Human|Equipment"
        string Description
        string AnalysisMethod "5Whys|Fishbone|FMEA"
    }

    CAPA_ACTION {
        int Id PK
        int CapaRecordId FK
        string ActionType "Corrective|Preventive"
        string Description
        string ResponsibleUserId FK
        datetime DueDate
        string Status "Planned|InProgress|Completed|Overdue"
        datetime CompletedAt
        string EvidenceFilePath
    }

    CAPA_VERIFICATION {
        int Id PK
        int CapaRecordId FK
        string Method "Audit|Review|Test|Inspection"
        string VerifierUserId FK
        datetime VerifiedAt
        string Result "Effective|NotEffective|PartiallyEffective"
        string Notes
    }

    CAPA_RECORD ||--o{ CAPA_ROOT_CAUSE : "has"
    CAPA_RECORD ||--o{ CAPA_ACTION : "contains"
    CAPA_RECORD ||--o{ CAPA_VERIFICATION : "undergoes"
    CAPA_RECORD }o--|| CLINIC : "belongs_to"
    CAPA_RECORD }o--|| APP_USER : "identified_by"
    CAPA_ACTION }o--|| APP_USER : "assigned_to"
```

### 8.4 CAPA Integration Points — Summary Table

| Trigger Source | Where CAPA Is Created | Existing Module | Integration Method |
|---------------|----------------------|-----------------|-------------------|
| Policy expired | PolicyManagement Approve/Reject action when status → Expired | PolicyDocumentService | Domain event: `PolicyDocumentExpired` → CAPA handler |
| KPI below threshold | KPI Service when `ActualValue < TargetValue * Threshold%` | KPIService | Background job: check KPI compliance → auto-create CAPA |
| Checklist failure | Checklist round completion with failing items | ChecklistService | Event: `ChecklistRoundCompleted` → create CAPA for failed items |
| HR document expired | HR document expiry background check | HrService | Background job: expiring docs → auto-create CAPA per staff |
| Audit finding | AuditMiddleware logging non-conformant actions | AuditService | Manual: AuditLog view → "Create CAPA" action button |
| ClinicDocument expired | Clinic document status change to Expired | ClinicDocumentService | Event: `ClinicDocumentExpired` → CAPA trigger |
| Manual entry | New CAPA button on Dashboard and Reporting views | DashboardController | Direct CRUD via CAPA controller |

---

## 9. Suggested Compliance Score Engine Integration Points

### 9.1 Compliance Score Architecture

```mermaid
flowchart TB
    subgraph "Data Sources"
        PD[PolicyDocuments]
        KPI[KPIEntries]
        CHK[ChecklistResults]
        HR[HrDocuments]
        CD[ClinicDocuments]
    end

    subgraph "Scoring Components"
        PC[Policy Compliance]
        KC[KPI Attainment]
        CC[Checklist Compliance]
        HC[HR Compliance]
        DC[Document Compliance]
    end

    subgraph "Weight Configuration"
        W1[Policy Weight: 25%]
        W2[KPI Weight: 20%]
        W3[Checklist Weight: 25%]
        W4[HR Weight: 15%]
        W5[Document Weight: 15%]
    end

    subgraph "Score Engine"
        RAW[Raw Scores 0-100]
        WGHT[Weighted Aggregation]
        OVER[Overall Score 0-100]
        CAPA_DEDUCT[CAPA Deductions]
        FINAL[Final Compliance Score]
    end

    subgraph "Storage & Display"
        CLINIC_TABLE[Clinics.ComplianceScore]
        DASHBOARD[ClinicAdmin Dashboard]
        SUPER_DASH[SuperAdmin Dashboard]
        CALENDAR[Compliance Calendar]
        REPORT[Compliance Reports]
        TREND[Trend Analytics]
    end

    PD --> PC
    KPI --> KC
    CHK --> CC
    HR --> HC
    CD --> DC

    PC --> RAW
    KC --> RAW
    CC --> RAW
    HC --> RAW
    DC --> RAW

    W1 --> WGHT
    W2 --> WGHT
    W3 --> WGHT
    W4 --> WGHT
    W5 --> WGHT

    RAW --> WGHT
    WGHT --> OVER
    OVER --> CAPA_DEDUCT
    CAPA_DEDUCT --> FINAL

    FINAL --> CLINIC_TABLE
    CLINIC_TABLE --> DASHBOARD
    CLINIC_TABLE --> SUPER_DASH
    CLINIC_TABLE --> CALENDAR
    CLINIC_TABLE --> REPORT
    CLINIC_TABLE --> TREND

    classDef source fill:#fce4ec,stroke:#c62828
    classDef score fill:#e8f5e9,stroke:#2e7d32
    classDef config fill:#fff3e0,stroke:#e65100
    classDef engine fill:#e3f2fd,stroke:#1565c0
    classDef output fill:#f3e5f5,stroke:#6a1b9a

    class PD,KPI,CHK,HR,CD source
    class PC,KC,CC,HC,DC score
    class W1,W2,W3,W4,W5 config
    class RAW,WGHT,OVER,CAPA_DEDUCT,FINAL engine
    class CLINIC_TABLE,DASHBOARD,SUPER_DASH,CALENDAR,REPORT,TREND output
```

### 9.2 Scoring Formulas

#### Policy Compliance Score
```
PC = (ApprovedPolicies + CompletePolicies) / TotalActivePolicies × 100

Where:
  - Approved = policyDocument.Status == "Approved" AND ExpiryDate > Today
  - Complete = policyDocument.Status == "Complete" AND ExpiryDate > Today
  - Exclude: Draft, NeedsReview, Expired
```

#### KPI Attainment Score
```
KC = AVG(per KPI (ActualValue / TargetValue) × 100)

Where:
  - Each KPI entry in last 12 months
  - Cap at 100% per KPI
  - Missing periods = 0% (not 100%)
```

#### Checklist Compliance Score
```
CC = (TotalPassingItemsAcrossAllRounds / TotalItemsAcrossAllRounds) × 100

Where:
  - "Passing" = ChecklistAnswer.AnswerValue == ChecklistAnswer.Yes
  - Only rounds in last 12 months
  - Rounds with status "Approved" only
```

#### HR Compliance Score
```
HC = (CompliantStaff / TotalActiveStaff) × 100

Where:
  - "Compliant" = all required document types exist AND are not expired
  - Required types vary by StaffType (e.g., Doctor requires License + CV + ID)
```

#### Document Compliance Score
```
DC = (CompleteClinicDocs + NeedsReviewClinicDocs) / TotalAssignedTemplates × 100

Where:
  - Each template assigned to clinic must have a clinic document
  - Status != Draft and Status != Expired and Status != Missing
```

### 9.3 Score Calculation Flow

```mermaid
sequenceDiagram
    participant SVC as ComplianceScoreService (NEW)
    participant UOW as UnitOfWork
    participant SQL as SQL Server
    participant CAPA as CapaService (NEW)
    participant SET as SettingsService
    participant CACHE as IDistributedCache (REDIS)

    Note over SVC: Triggered by: Dashboard load, Scheduled job, Manual recalculation
    
    SVC->>CACHE: Get cached score for ClinicId
    alt Cache Hit
        CACHE-->>SVC: Cached Score
    else Cache Miss
        SVC->>SET: Get weight configuration
        SET-->>SVC: Weights + Thresholds
        
        par Policy Score
            SVC->>UOW: Count active policies per status
            UOW->>SQL: SELECT Status, COUNT(*) GROUP BY Status
            SQL-->>UOW: Status counts
            UOW-->>SVC: Policy metrics
        and KPI Score
            SVC->>UOW: Get KPI attainment ratios
            UOW->>SQL: SELECT KPIId, AVG(ActualValue/TargetValue)
            SQL-->>UOW: Attainment data
            UOW-->>SVC: KPI metrics
        and Checklist Score
            SVC->>UOW: Get recent round pass/fail rates
            UOW->>SQL: SELECT AVG(CASE WHEN AnswerValue='Yes' THEN 1 ELSE 0 END)
            SQL-->>UOW: Pass rates
            UOW-->>SVC: Checklist metrics
        and HR Score
            SVC->>UOW: Get staff compliance counts
            UOW->>SQL: Staff with all valid docs vs total
            SQL-->>UOW: HR metrics
        and Document Score
            SVC->>UOW: Get clinic document statuses
            UOW->>SQL: ClinicDocument status distribution
            SQL-->>UOW: Document metrics
        end
        
        SVC->>SVC: Calculate weighted raw score
        SVC->>CAPA: Get active CAPA deductions for clinic
        CAPA-->>SVC: Deduction amount
        
        SVC->>SVC: Final = Raw - CAPA_Deduction
        SVC->>UOW: Update Clinics.ComplianceScore
        UOW->>SQL: UPDATE SET ComplianceScore = @Score WHERE Id = @ClinicId
        
        SVC->>CACHE: Set cached score (TTL: 15 minutes)
        
        alt Score Below Threshold
            SVC->>CAPA: Auto-generate CAPA record
            SVC->>NTF: Send compliance alert notification
        end
        
        SVC-->>CALLER: Final ComplianceScore
    end
```

### 9.4 Integration Points — Existing Code Changes

| Integration Point | File | Change |
|------------------|------|--------|
| Dashboard real-time score | `ClinicAdmin/DashboardController.cs` | Call `ComplianceScoreService` in `Index()`; display score card |
| SuperAdmin overview | `SuperAdmin/DashboardController.cs` | Aggregate all clinic scores; show min/max/avg |
| Policy action updates score | `PolicyManagementController.Approve` | Recalculate score after status change |
| KPI entry updates score | `KPIManagementController.EnterData` | Recalculate KPI component after entry |
| Checklist completion updates score | `ChecklistManagementController.Execute` | Recalculate checklist component after round |
| HR verify document updates score | `HRManagementController.VerifyDocument` | Recalculate HR component after verification |
| ClinicDocument status updates score | `ClinicDocumentsController.UpdateStatus` | Recalculate document component |
| Background scheduled recalculation | `BackgroundJobService.cs` | Add `ScheduleComplianceScoreRecalculationAsync()` |
| ComplianceCalendar severity | `ComplianceCalendarService.cs` | Use compliance scores to color-code calendar items |
| Analytics integration | `AnalyticsService.cs` | Add score trend to analytics |
| Reporting integration | `ReportingService.cs` | Add score to compliance reports |
| ComplianceScore column | `Clinics.ComplianceScore` | Already exists as `decimal(5,2)` — currently unused or stub |
| Settings for weights | `SystemSettings` | Add 5 system settings for weight configuration |
| Cache invalidation | `ComplianceScoreEngine.cs` | Invalidate cache on any data mutation that affects score |

### 9.5 Suggested CAPA Deduction Logic

```
Open_Investigation:  -2 points per CAPA
Open_ActionPlanned:  -5 points per CAPA
Open_Implementation: -3 points per CAPA
Past_Due:            -10 points per CAPA (any overdue CAPA of any status)
Critical_Severity:   -15 points per CAPA (stacked with status penalty)

Max Deduction: -30 points (cap to prevent negative scores)

Score Scale: 0-100
  90-100: Excellent (Green)
  75-89:  Good (Light Green)
  60-74:  Needs Improvement (Yellow)
  40-59:  Poor (Orange)
  <40:    Critical (Red)
```
