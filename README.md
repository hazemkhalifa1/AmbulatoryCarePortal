# CBAHI Ambulatory Care Compliance Portal

A comprehensive ASP.NET Core 8 MVC application for managing healthcare clinic compliance according to Saudi Arabian CBAHI standards.

## Project Overview

The CBAHI Ambulatory Care Compliance Portal is a multi-tenant SaaS solution designed to help ambulatory care clinics, dental centers, and specialty clinics manage their compliance with CBAHI (Central Board for Accreditation of Healthcare Institutions) standards.

### Key Features

- **Multi-Tenant Architecture**: Support for multiple clinics
- **Policy Management**: Track and manage compliance policies and documents
- **KPI Monitoring**: Monitor key performance indicators
- **Checklist System**: Daily, weekly, and monthly compliance checklists
- **HR Management**: Track staff documents and certifications
- **Compliance Scoring**: Automatic calculation of compliance scores
- **Audit Logging**: Complete audit trail of all actions
- **Bilingual Support**: Arabic and English interface
- **RTL Support**: Full right-to-left layout support
- **Role-Based Access**: SuperAdmin, ClinicAdmin, DepartmentUser, Auditor, Viewer roles
- **Responsive Design**: AdminLTE 3.2 based UI
- **Document Management**: Upload and track evidence files

## Technology Stack

- **Framework**: ASP.NET Core 8
- **Database**: SQL Server
- **ORM**: Entity Framework Core 8
- **Authentication**: ASP.NET Core Identity
- **Mapping**: AutoMapper
- **Validation**: FluentValidation
- **Logging**: Serilog
- **Architecture**: Clean Architecture with N-Tier pattern
- **Frontend**: Bootstrap 5.3, AdminLTE 3.2

## Project Structure

```
AmbulatoryCarePortal/
├── src/
│   ├── AmbulatoryCarePortal.Domain/           # Entity definitions
│   │   ├── Entities/                          # Domain entities
│   │   ├── Enums/                             # Enumerations
│   │   └── ...
│   ├── AmbulatoryCarePortal.Application/      # Business logic
│   │   ├── DTOs/                              # Data transfer objects
│   │   ├── Interfaces/                        # Service contracts
│   │   ├── Services/                          # Service implementations
│   │   ├── Validators/                        # FluentValidation validators
│   │   ├── Mappings/                          # AutoMapper profiles
│   │   └── ...
│   ├── AmbulatoryCarePortal.Infrastructure/   # Data access & external services
│   │   ├── Data/                              # DbContext & Configurations
│   │   ├── Repositories/                      # Repository pattern
│   │   ├── UnitOfWork/                        # Unit of work pattern
│   │   └── ...
│   └── AmbulatoryCarePortal.Presentation/     # Web application
│       ├── Controllers/                       # MVC controllers
│       ├── Areas/                             # Admin areas
│       ├── Views/                             # Razor views
│       ├── wwwroot/                           # Static files
│       └── ...
└── tests/
    └── AmbulatoryCarePortal.Tests/            # Unit tests
```

## Prerequisites

- .NET 8 SDK or later
- SQL Server 2019 or later (or SQL Server Express LocalDB)
- Visual Studio 2022 or Visual Studio Code

## Installation & Setup

### 1. Clone the Repository

```bash
git clone <repository-url>
cd AmbulatoryCarePortal
```

### 2. Configure Database Connection

Edit `src/AmbulatoryCarePortal.Presentation/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=AmbulatoryCarePortalDb;Trusted_Connection=True;"
  }
}
```

### 3. Apply Database Migrations

```bash
cd src/AmbulatoryCarePortal.Presentation

# Create and apply migrations
dotnet ef database update --project ../AmbulatoryCarePortal.Infrastructure
```

### 4. Run the Application

```bash
dotnet run
```

The application will be available at `https://localhost:5001`

## Default Credentials

After initial setup, the following admin account is created:

- **Email**: admin@cbahi-portal.com
- **Password**: CbahiAdmin@2024

**Important**: Change this password on first login!

## Database Initialization

The application automatically initializes the database with:

- Required roles (SuperAdmin, ClinicAdmin, DepartmentUser, Auditor, Viewer)
- Admin user account
- Sample clinic with all CBAHI departments
- Department codes (LD, PC, LB, RD, DN, MM, MOI, IPC, FMS, DPU, DA)

## Entity Relationships

### Core Entities

1. **Clinic**: Main tenant entity
2. **Department**: Departments within a clinic
3. **AppUser**: System users with roles
4. **PolicyDocument**: Compliance policies by department
5. **EvidenceAttachment**: Files supporting policies
6. **KPI**: Key performance indicators
7. **KPIEntry**: Monthly KPI measurements
8. **ChecklistTemplate**: Checklist templates
9. **ChecklistRound**: Checklist execution instances
10. **HrStaff**: Employee records
11. **HrDocument**: Staff document tracking
12. **Notification**: System notifications
13. **AuditTrail**: Action logging

## Key Features

### 1. Clinic Management
- Register and manage multiple clinics
- Track clinic type (Ambulatory, Specialty, Dental, etc.)
- Monitor license expiry dates
- Calculate compliance scores

### 2. Policy Management
- Create and manage policies by department
- Track policy versions
- Upload evidence documents
- Monitor document status
- Track expiry dates

### 3. KPI Monitoring
- Define KPIs by clinic or department
- Track monthly measurements
- Calculate achievement percentages
- Generate KPI reports

### 4. Compliance Checklists
- Create daily, weekly, monthly checklists
- Execute checklists with evidence
- Track completion rates
- Generate checklist history

### 5. HR Management
- Maintain staff records
- Track employee documents (IDs, licenses, certifications)
- Monitor document expiry
- Set expiry reminders

### 6. Audit & Compliance
- Complete audit trail of all actions
- User activity tracking
- IP address logging
- Data change tracking

## API Endpoints

### Clinic Management
- `GET /api/clinics` - Get all clinics
- `GET /api/clinics/{id}` - Get clinic details
- `POST /api/clinics` - Create clinic
- `PUT /api/clinics/{id}` - Update clinic
- `DELETE /api/clinics/{id}` - Delete clinic

### Policy Documents
- `GET /api/policies?clinicId={id}` - Get clinic policies
- `GET /api/policies/{id}` - Get policy details
- `POST /api/policies` - Create policy
- `PUT /api/policies/{id}` - Update policy
- `DELETE /api/policies/{id}` - Delete policy

### KPIs
- `GET /api/kpis?clinicId={id}` - Get KPIs
- `POST /api/kpis` - Create KPI
- `POST /api/kpis/{id}/entry` - Add KPI entry

### Checklists
- `GET /api/checklists?clinicId={id}` - Get checklists
- `POST /api/checklists` - Create checklist
- `POST /api/checklists/{id}/execute` - Execute checklist

### HR
- `GET /api/staff?clinicId={id}` - Get clinic staff
- `POST /api/staff` - Add staff
- `POST /api/staff/{id}/documents` - Upload staff document

## Configuration

### Email Settings

Edit `appsettings.json` to configure email notifications:

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "SenderEmail": "noreply@cbahi-portal.com",
    "Username": "your-email@gmail.com",
    "Password": "your-app-password"
  }
}
```

### File Upload Settings

```json
{
  "FileUploadSettings": {
    "MaxFileSizeBytes": 20971520,
    "AllowedExtensions": [".pdf", ".doc", ".docx", ".jpg", ".jpeg", ".png", ".xlsx"],
    "BasePath": "wwwroot/uploads"
  }
}
```

## Roles & Permissions

### SuperAdmin
- Manage all clinics
- View system-wide compliance reports
- Manage users and roles
- View audit logs

### ClinicAdmin
- Manage clinic data
- Create and update policies
- Monitor KPIs
- Manage HR staff
- Create checklists
- View clinic reports

### DepartmentUser
- View department policies
- Execute checklists
- View HR staff
- Upload documents

### Auditor
- View-only access to all data
- Generate audit reports
- View compliance status

### Viewer
- Read-only access to dashboard
- View basic information

## Development

### Running Tests

```bash
cd tests/AmbulatoryCarePortal.Tests
dotnet test
```

### Creating New Migrations

```bash
# From Presentation project directory
dotnet ef migrations add MigrationName --project ../AmbulatoryCarePortal.Infrastructure
dotnet ef database update --project ../AmbulatoryCarePortal.Infrastructure
```

### Building for Production

```bash
dotnet publish -c Release -o ./publish
```

## Troubleshooting

### Database Connection Issues
- Verify SQL Server is running
- Check connection string in appsettings.json
- Ensure database doesn't already exist (or delete it first)

### Migration Errors
- Delete the Migrations folder and start fresh
- Ensure EF Core tools are updated: `dotnet tool update -g dotnet-ef`

### Login Issues
- Clear browser cookies and cache
- Run database initialization again
- Check if admin user was created

## Support

For issues and feature requests, please contact the development team.

## License

This project is proprietary software developed for CBAHI compliance management.

## Changelog

### Version 1.0.0
- Initial release
- Core functionality for clinic, policy, KPI, and HR management
- Bilingual interface support
- Role-based access control
- Comprehensive audit logging

---

**Last Updated**: January 2024
**Version**: 1.0.0
**Status**: Production Ready
