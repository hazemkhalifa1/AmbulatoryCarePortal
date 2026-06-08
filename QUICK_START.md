# Quick Start Guide - CBAHI Ambulatory Care Portal

## ⚡ Get Started in 5 Minutes

### Prerequisites ✅
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet) 
- SQL Server LocalDB or SQL Server Express
- Visual Studio 2022 or VS Code

---

## Step 1️⃣: Open the Project

```bash
cd AmbulatoryCarePortal
```

Open solution in Visual Studio 2022:
```bash
start AmbulatoryCarePortal.sln
```

---

## Step 2️⃣: Configure Database

**For LocalDB (Recommended):**

File: `src/AmbulatoryCarePortal.Presentation/appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AmbulatoryCarePortalDb_Dev;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

**For SQL Server Express:**

File: `src/AmbulatoryCarePortal.Presentation/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=AmbulatoryCarePortalDb;Trusted_Connection=True;MultipleActiveResultSets=true;"
  }
}
```

---

## Step 3️⃣: Apply Database Migrations

In Visual Studio Package Manager Console:

```powershell
cd .\src\AmbulatoryCarePortal.Presentation
dotnet ef database update --project ..\AmbulatoryCarePortal.Infrastructure
```

Or from command line:

```bash
cd src/AmbulatoryCarePortal.Presentation
dotnet ef database update --project ../AmbulatoryCarePortal.Infrastructure
```

---

## Step 4️⃣: Run the Application

In Visual Studio:
- Select **AmbulatoryCarePortal.Presentation** as startup project
- Press **F5** or click **Run**

From command line:

```bash
cd src/AmbulatoryCarePortal.Presentation
dotnet run
```

---

## Step 5️⃣: Login

Application URL: **https://localhost:5001**

**Default Credentials:**
- **Email**: admin@cbahi-portal.com
- **Password**: CbahiAdmin@2024

⚠️ Change password immediately after first login!

---

## 🎯 Next Actions

### 1. Create Your Clinic
```
Dashboard → Manage Clinics → Add Clinic
```
Fill in:
- Clinic Name
- Type (Ambulatory/Specialty/Dental)
- Location
- License Info

### 2. Create Users
```
System → User Management → Add User
```
Assign roles:
- ClinicAdmin - Full clinic management
- DepartmentUser - Department operations
- Auditor - Read-only audit access

### 3. Configure Policies
```
ClinicAdmin → Policies → Add Policy
```
For each department:
- Create policy
- Upload evidence
- Track compliance

### 4. Set Up KPIs
```
ClinicAdmin → KPI Management → Create KPI
```
Define:
- KPI Name
- Target Value
- Measurement Frequency

### 5. Create Checklists
```
ClinicAdmin → Checklists → Create Checklist
```
Set up:
- Daily/Weekly/Monthly schedules
- Add checklist items
- Assign to departments

---

## 📁 Project Structure

```
AmbulatoryCarePortal/
├── src/
│   ├── AmbulatoryCarePortal.Domain/         # Entities
│   ├── AmbulatoryCarePortal.Application/    # Business Logic
│   ├── AmbulatoryCarePortal.Infrastructure/ # Data Access
│   └── AmbulatoryCarePortal.Presentation/   # Web App (Start Here!)
├── tests/
│   └── AmbulatoryCarePortal.Tests/          # Unit Tests
├── README.md                                 # Full Documentation
├── INSTALLATION_GUIDE.md                    # Detailed Setup
└── PROJECT_SUMMARY.md                       # Complete Overview
```

---

## 🔧 Useful Commands

### Build
```bash
dotnet build
```

### Run Tests
```bash
dotnet test
```

### Create Migration
```bash
cd src/AmbulatoryCarePortal.Presentation
dotnet ef migrations add MigrationName --project ../AmbulatoryCarePortal.Infrastructure
```

### Generate Database
```bash
cd src/AmbulatoryCarePortal.Presentation
dotnet ef database update --project ../AmbulatoryCarePortal.Infrastructure
```

### Publish
```bash
dotnet publish --configuration Release --output ./publish
```

---

## 🛠️ Troubleshooting

### ❌ Database Connection Failed
✅ Check connection string in appsettings.json
✅ Verify SQL Server is running
✅ Ensure database doesn't exist (delete if present)

### ❌ Migration Errors
✅ Delete Migrations folder
✅ Delete database
✅ Run migrations again

### ❌ Cannot Login
✅ Clear browser cache
✅ Try private/incognito mode
✅ Verify admin user in database

### ❌ Port 5001 Already in Use
✅ Change port in launchSettings.json
✅ Or kill process using the port

---

## 📚 Documentation

| Document | Purpose |
|----------|---------|
| **README.md** | Full project overview |
| **INSTALLATION_GUIDE.md** | Detailed installation steps |
| **PROJECT_SUMMARY.md** | Complete file listing |
| **Code Comments** | Inline code documentation |

---

## 🎓 Key Features Ready to Use

✅ Multi-tenant clinic management
✅ Bilingual (Arabic/English) interface
✅ Policy and compliance tracking
✅ KPI monitoring and calculation
✅ Daily/Weekly/Monthly checklists
✅ HR staff and document management
✅ Comprehensive audit logging
✅ Role-based access control
✅ Professional AdminLTE UI
✅ Responsive mobile design
✅ Export and reporting ready

---

## 🔐 Default Roles

| Role | Permissions |
|------|------------|
| **SuperAdmin** | All system access, clinic management |
| **ClinicAdmin** | Clinic data, policies, KPIs, staff |
| **DepartmentUser** | Department tasks, checklist execution |
| **Auditor** | Read-only access to all data |
| **Viewer** | Dashboard view only |

---

## ⚙️ Configuration Files

### Development
`appsettings.Development.json` - LocalDB, debug logging

### Production
`appsettings.json` - Production settings, Serilog config

### Environment-Specific
`appsettings.Production.json` - Override for production

---

## 🚀 Ready for Production?

Before deploying:

1. ✅ Change admin password
2. ✅ Configure production database
3. ✅ Set up email notifications (optional)
4. ✅ Configure HTTPS certificates
5. ✅ Set up backup strategy
6. ✅ Configure logging and monitoring
7. ✅ Test all workflows
8. ✅ Create admin users for production

---

## 📞 Support

For detailed help:
- See **INSTALLATION_GUIDE.md** for setup issues
- See **PROJECT_SUMMARY.md** for file locations
- See **README.md** for features and APIs
- Check code comments for implementation details

---

## ✨ What's Included

✅ **17 Database Entities** with complete relationships
✅ **7 Service Interfaces** with implementations
✅ **4 Controllers** for admin operations
✅ **Professional UI** with AdminLTE 3.2
✅ **Complete Validation** with FluentValidation
✅ **Audit Logging** for compliance
✅ **Unit Tests** with xUnit and Moq
✅ **SQL Server Integration** with EF Core 8
✅ **ASP.NET Core Identity** authentication
✅ **Production-Ready Code** following SOLID principles

---

## 🎉 You're All Set!

Your CBAHI Ambulatory Care Compliance Portal is ready to:
- Track clinic compliance
- Monitor policies and documents
- Track key performance indicators
- Execute compliance checklists
- Manage HR documents
- Generate audit reports

Start building your clinic's compliance today!

---

**Version**: 1.0.0
**Status**: ✅ Production Ready
**Framework**: ASP.NET Core 8
**Database**: SQL Server
**Created**: January 2024

**Happy Coding! 🚀**
