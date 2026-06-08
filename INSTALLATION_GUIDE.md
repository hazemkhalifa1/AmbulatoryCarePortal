# Installation Guide - CBAHI Ambulatory Care Portal

## Prerequisites

Before you begin, ensure you have the following installed:

1. **.NET 8 SDK or later**
   - Download from: https://dotnet.microsoft.com/download/dotnet
   - Verify installation: `dotnet --version`

2. **SQL Server 2019 or later** (or SQL Server Express LocalDB)
   - Download from: https://www.microsoft.com/sql-server/sql-server-downloads
   - Or use LocalDB (included with Visual Studio)

3. **Visual Studio 2022** (recommended)
   - Community Edition is free
   - Or use Visual Studio Code with .NET extensions

4. **Git** (for cloning the repository)
   - Download from: https://git-scm.com/

## Step 1: Clone the Repository

```bash
git clone <repository-url>
cd AmbulatoryCarePortal
```

## Step 2: Configure Database Connection

### Option A: Using SQL Server LocalDB (Recommended for Development)

Edit `src/AmbulatoryCarePortal.Presentation/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AmbulatoryCarePortalDb_Dev;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

### Option B: Using SQL Server Express

Edit `src/AmbulatoryCarePortal.Presentation/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_MACHINE_NAME\\SQLEXPRESS;Database=AmbulatoryCarePortalDb;Trusted_Connection=True;MultipleActiveResultSets=true;Encrypt=False"
  }
}
```

Replace `YOUR_MACHINE_NAME` with your computer name.

### Option C: Using Remote SQL Server

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server-address;Database=AmbulatoryCarePortalDb;User Id=sa;Password=your-password;MultipleActiveResultSets=true;Encrypt=False;TrustServerCertificate=True"
  }
}
```

## Step 3: Restore NuGet Packages

Open PowerShell or Command Prompt in the project root directory:

```bash
dotnet restore
```

## Step 4: Apply Database Migrations

Navigate to the Presentation project directory:

```bash
cd src/AmbulatoryCarePortal.Presentation
```

Apply migrations to create the database:

```bash
dotnet ef database update --project ../AmbulatoryCarePortal.Infrastructure
```

This command will:
- Create the database
- Create all tables and relationships
- Seed initial data (roles, admin user, demo clinic)

## Step 5: Configure Email Settings (Optional)

For email notifications, edit `appsettings.json`:

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "SenderEmail": "noreply@cbahi-portal.com",
    "SenderName": "CBAHI Compliance Portal",
    "Username": "your-email@gmail.com",
    "Password": "your-gmail-app-password"
  }
}
```

### For Gmail:
1. Enable 2-Step Verification on your Google Account
2. Generate an App Password: https://myaccount.google.com/apppasswords
3. Use the generated 16-character password in the configuration

## Step 6: Run the Application

From the Presentation project directory:

```bash
dotnet run
```

Or from the root directory:

```bash
dotnet run --project src/AmbulatoryCarePortal.Presentation
```

The application will start on:
- HTTPS: https://localhost:5001
- HTTP: http://localhost:5000

## Step 7: Access the Application

1. Open your browser
2. Navigate to: https://localhost:5001
3. You will be redirected to the login page

### Default Credentials

- **Email**: admin@cbahi-portal.com
- **Password**: CbahiAdmin@2024

⚠️ **IMPORTANT**: Change this password immediately after first login!

## Step 8: Initial Configuration

After logging in as SuperAdmin:

1. **Create Your First Clinic**
   - Go to SuperAdmin → Manage Clinics
   - Click "Add Clinic"
   - Fill in clinic details
   - Select clinic type
   - Save

2. **Create Users**
   - Go to SuperAdmin → User Management
   - Click "Add User"
   - Assign roles (ClinicAdmin, DepartmentUser, etc.)
   - Save

3. **Configure Policies**
   - Go to ClinicAdmin → Policies
   - Create new policies for each department
   - Upload evidence documents

4. **Create KPIs**
   - Go to ClinicAdmin → KPI Management
   - Define KPIs for your clinic
   - Set target values and frequency

## Troubleshooting

### Issue: Database Connection Failed

**Solution**:
1. Verify SQL Server is running
2. Check connection string in appsettings.json
3. Ensure database user has proper permissions
4. Try connection with SQL Server Management Studio

### Issue: Migration Errors

**Solution**:
1. Delete the database manually
2. Delete the `Migrations` folder in Infrastructure project
3. Run migrations again:
   ```bash
   dotnet ef database update --project ../AmbulatoryCarePortal.Infrastructure
   ```

### Issue: Port Already in Use

**Solution**:
1. Change port in `Properties/launchSettings.json`
2. Or stop the process using the port:
   ```bash
   # Windows
   netstat -ano | findstr :5001
   taskkill /PID <PID> /F
   
   # Linux/Mac
   lsof -i :5001
   kill -9 <PID>
   ```

### Issue: Login Not Working

**Solution**:
1. Verify admin user was created (check database)
2. Clear browser cache and cookies
3. Try incognito/private browser window
4. Reset admin password using database query:
   ```sql
   UPDATE AspNetUsers 
   SET NormalizedUserName='ADMIN@CBAHI-PORTAL.COM', 
       NormalizedEmail='ADMIN@CBAHI-PORTAL.COM'
   WHERE Email='admin@cbahi-portal.com'
   ```

### Issue: CSS/JavaScript Not Loading

**Solution**:
1. Clear browser cache
2. Hard refresh (Ctrl+Shift+R or Cmd+Shift+R)
3. Check browser console for errors
4. Ensure `wwwroot` folder exists and has files

## Building for Production

### Release Build

```bash
dotnet build --configuration Release
```

### Publishing

```bash
dotnet publish --configuration Release --output ./publish
```

### Deployment

1. Copy contents of `publish` folder to server
2. Ensure .NET 8 Runtime is installed on server
3. Create application pool in IIS or configure Nginx/Apache
4. Configure connection string for production database
5. Set up HTTPS certificates
6. Configure firewall and network rules

## Development vs Production

### Development Configuration

- `appsettings.Development.json` - Overrides base settings
- Debug logging enabled
- LocalDB database
- Email notifications disabled (optional)

### Production Configuration

Create `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=prod-server;Database=AmbulatoryCarePortalDb;User Id=dbuser;Password=strongpassword;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```

## Maintaining the Application

### Regular Backups

```bash
# Backup database
sqlcmd -S (localdb)\mssqllocaldb -Q "BACKUP DATABASE AmbulatoryCarePortalDb TO DISK='C:\Backups\AmbulatoryCarePortal.bak'"
```

### Updating Dependencies

```bash
dotnet list package --outdated
dotnet package update
```

### Monitoring

- Check `logs/` folder for application logs
- Monitor database growth
- Review audit trails regularly
- Check for overdue compliance items

## Support

For issues not covered in this guide:
1. Check the README.md
2. Review application logs
3. Check SQL Server error logs
4. Contact development team

---

**Version**: 1.0.0
**Last Updated**: January 2024
