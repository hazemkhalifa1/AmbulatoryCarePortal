# 🎯 PROFESSIONAL SETUP GUIDE - CBAHI PORTAL

**Complete Setup for Enterprise Features**

---

## 📋 TABLE OF CONTENTS

1. [Initial System Setup](#initial-system-setup)
2. [Role Assignment Guide](#role-assignment-guide)
3. [User Creation Workflow](#user-creation-workflow)
4. [Permission Configuration](#permission-configuration)
5. [Feature Activation](#feature-activation)
6. [Professional Configuration](#professional-configuration)
7. [Testing & Verification](#testing--verification)

---

## 🚀 INITIAL SYSTEM SETUP

### Step 1: Deploy Application

```bash
# Restore packages
dotnet restore

# Build solution
dotnet build

# Apply migrations
cd src/AmbulatoryCarePortal.Presentation
dotnet ef database update --project ../AmbulatoryCarePortal.Infrastructure

# Run application
dotnet run
```

### Step 2: System Initialization

The system automatically creates:
- ✅ 8 roles with permissions
- ✅ SuperAdmin user account
- ✅ Demo clinic with 11 departments
- ✅ Sample policies and KPIs
- ✅ Audit logging system
- ✅ Notification system

### Step 3: First Login

```
Email: admin@cbahi-portal.com
Password: CbahiAdmin@2024
```

⚠️ **IMPORTANT:** Change password immediately!

---

## 👥 ROLE ASSIGNMENT GUIDE

### Complete Role Hierarchy

```
SuperAdmin (System Owner)
├── ClinicAdmin (Clinic Manager)
│   ├── DepartmentHead (Department Manager)
│   │   ├── DepartmentUser (Staff Member)
│   │   ├── HRManager (HR Staff)
│   │   └── ComplianceOfficer (Compliance Lead)
└── Auditor (External Auditor)
└── Viewer (Stakeholder)
```

---

## 👤 USER CREATION WORKFLOW

### Creating Different User Types

#### **1. Creating a SuperAdmin User**

```
Path: SuperAdmin → User Management → Create User

Form Fields:
├── Email: admin2@cbahi-portal.com
├── Full Name: Second Admin
├── Phone: +966501234567
├── Clinic: (No clinic assignment needed)
├── Department: (No department needed)
├── Password: [Strong password]
├── Role: SuperAdmin
└── Status: Active

Permissions Granted:
- Full system access
- Manage all clinics
- Create/manage users
- Configure system
- View all reports
```

#### **2. Creating a ClinicAdmin User**

```
Path: SuperAdmin → User Management → Create User

Form Fields:
├── Email: admin@clinic1.com
├── Full Name: Clinic Manager - Clinic 1
├── Phone: +966501234568
├── Clinic: Clinic 1 [Required]
├── Department: (Optional)
├── Password: [Strong password]
├── Role: ClinicAdmin
└── Status: Active

Permissions Granted:
- Manage clinic operations
- Create policies
- Create KPIs
- Create checklists
- Manage clinic staff
- Generate clinic reports
- Manage clinic users
```

#### **3. Creating a DepartmentHead User**

```
Path: ClinicAdmin → User Management → Create User

Form Fields:
├── Email: head@lab.clinic1.com
├── Full Name: Laboratory Head
├── Phone: +966501234569
├── Clinic: Clinic 1 [Required]
├── Department: Laboratory (LD) [Required]
├── Password: [Strong password]
├── Role: DepartmentHead
└── Status: Active

Permissions Granted:
- Manage department policies
- Monitor department KPIs
- Create department checklists
- Supervise staff
- View department compliance
- Verify staff documents
- Generate department reports
```

#### **4. Creating a DepartmentUser (Staff)**

```
Path: DepartmentHead → User Management → Create User

Form Fields:
├── Email: staff@lab.clinic1.com
├── Full Name: Laboratory Technician 1
├── Phone: +966501234570
├── Clinic: Clinic 1 [Required]
├── Department: Laboratory (LD) [Required]
├── Password: [Strong password]
├── Role: DepartmentUser
└── Status: Active

Permissions Granted:
- Execute checklists
- Enter KPI data
- Upload documents
- View policies
- View personal tasks
```

#### **5. Creating a ComplianceOfficer User**

```
Path: SuperAdmin → User Management → Create User

Form Fields:
├── Email: compliance@cbahi.com
├── Full Name: Compliance Officer
├── Phone: +966501234571
├── Clinic: (Can be assigned to specific clinic or all)
├── Department: (Optional)
├── Password: [Strong password]
├── Role: ComplianceOfficer
└── Status: Active

Permissions Granted:
- Monitor compliance
- Approve policies and checklists
- Generate compliance reports
- Create alerts
- View audit trails
- Generate analytics
```

#### **6. Creating an HRManager User**

```
Path: ClinicAdmin → User Management → Create User

Form Fields:
├── Email: hr@clinic1.com
├── Full Name: HR Manager
├── Phone: +966501234572
├── Clinic: Clinic 1 [Required]
├── Department: Administrative Services (DA)
├── Password: [Strong password]
├── Role: HRManager
└── Status: Active

Permissions Granted:
- Manage staff records
- Verify documents
- Monitor expiry dates
- Manage certifications
- Generate HR reports
- View staff compliance
```

#### **7. Creating an Auditor User**

```
Path: SuperAdmin → User Management → Create User

Form Fields:
├── Email: auditor@external.com
├── Full Name: External Auditor
├── Phone: +966501234573
├── Clinic: (All clinics or specific)
├── Department: (None)
├── Password: [Strong password]
├── Role: Auditor
└── Status: Active

Permissions Granted:
- View audit logs (Read-Only)
- Generate audit reports
- Export audit data
- No modification permissions
```

#### **8. Creating a Viewer User**

```
Path: SuperAdmin → User Management → Create User

Form Fields:
├── Email: viewer@stakeholder.com
├── Full Name: Stakeholder Viewer
├── Phone: +966501234574
├── Clinic: Clinic 1
├── Department: (Optional)
├── Password: [Strong password]
├── Role: Viewer
└── Status: Active

Permissions Granted:
- View clinic dashboard
- View compliance metrics
- View policies (Read-Only)
- View KPI trends
- No modification permissions
```

---

## 🔐 PERMISSION CONFIGURATION

### Role-Based Access Control Setup

#### **SuperAdmin Configuration**

```yaml
Role: SuperAdmin
Clinic Access: All
Department Access: All
Features Enabled:
  - Clinic Management: Full
  - User Management: Full
  - Role Management: Full
  - System Settings: Full
  - Reporting: Full
  - Analytics: Full
  - Audit: Full
  - All Notifications: Yes
```

#### **ClinicAdmin Configuration**

```yaml
Role: ClinicAdmin
Clinic Access: Assigned Clinic Only
Department Access: All in Clinic
Features Enabled:
  - Clinic Management: Clinic Info Only
  - User Management: Clinic Users
  - Policy Management: Full
  - KPI Management: Full
  - Checklist Management: Full
  - HR Management: Full
  - Reporting: Clinic Reports
  - Audit: Clinic Logs
  - Notifications: Clinic Alerts
```

#### **DepartmentHead Configuration**

```yaml
Role: DepartmentHead
Clinic Access: Assigned Clinic
Department Access: Assigned Department Only
Features Enabled:
  - Department Management: Assigned Dept
  - Policy Management: Dept Policies
  - KPI Management: Dept KPIs
  - Checklist Management: Dept Checklists
  - HR Management: Dept Staff
  - Reporting: Dept Reports
  - Audit: Dept Logs
```

---

## 🎯 FEATURE ACTIVATION

### Activating Professional Features

#### **1. Enable Reporting**

```
Path: SuperAdmin → System Settings → Features → Reporting

Settings:
├── Enable PDF Export: ✅ Yes
├── Enable Excel Export: ✅ Yes
├── Enable CSV Export: ✅ Yes
├── Enable Scheduled Reports: ✅ Yes
├── Report Email Delivery: ✅ Yes
└── Save Reports History: ✅ Yes
```

#### **2. Enable Analytics**

```
Path: SuperAdmin → System Settings → Features → Analytics

Settings:
├── Enable Real-time Analytics: ✅ Yes
├── Enable Trend Analysis: ✅ Yes
├── Enable Predictive Analytics: ✅ Yes
├── Enable Custom Dashboards: ✅ Yes
├── Analytics Data Retention: 36 Months
└── Auto-calculate Insights: ✅ Yes
```

#### **3. Enable Notifications**

```
Path: SuperAdmin → System Settings → Features → Notifications

Settings:
├── Email Notifications: ✅ Yes
├── SMS Notifications: ✅ Yes (if configured)
├── In-App Notifications: ✅ Yes
├── Notification Scheduling: ✅ Yes
├── Bulk Notifications: ✅ Yes
└── Event-Based Alerts: ✅ Yes
```

#### **4. Enable Audit Logging**

```
Path: SuperAdmin → System Settings → Features → Audit

Settings:
├── Track User Actions: ✅ Yes
├── Track Data Changes: ✅ Yes
├── Track Login/Logout: ✅ Yes
├── Store IP Addresses: ✅ Yes
├── Audit Log Retention: 24 Months
└── Immutable Logs: ✅ Yes
```

---

## ⚙️ PROFESSIONAL CONFIGURATION

### Email & Notifications Setup

#### **Configure Email Service**

```
Path: SuperAdmin → System Settings → Email Configuration

Settings:
├── SMTP Server: smtp.gmail.com
├── SMTP Port: 587
├── Enable SSL: ✅ Yes
├── Username: noreply@cbahi-portal.com
├── Password: [App Password]
├── Sender Email: noreply@cbahi-portal.com
├── Sender Name: CBAHI Portal
└── Test Connection: ✅ Success
```

#### **Configure Notification Types**

```
Policy Approval Notifications:
├── To: Policy Owner, Department Head, Compliance Officer
├── Subject: Policy Approval Required
├── Template: [Custom Template]
└── Frequency: Immediate

Document Expiry Alerts:
├── Days Before: 30, 15, 7, 1
├── To: HR Manager, Staff Member
├── Subject: Document Expiring Soon
└── Include: Document Type, Expiry Date

Checklist Reminders:
├── Scheduled: Daily @ 8:00 AM
├── To: Assigned Users
├── Subject: Daily Checklist Due
└── Include: Checklist Details, Due Date
```

### Logo & Branding Setup

#### **Upload Company Logo**

```
Path: SuperAdmin → System Settings → Branding

Upload Logo:
├── Logo File: /images/cbahi-logo.png
├── Logo Size: 200x50 pixels
├── White Logo: /images/cbahi-logo-white.png
├── Favicon: /images/favicon.ico
└── Save Branding: ✅ Saved
```

#### **Configure Brand Colors**

```
Path: SuperAdmin → System Settings → Theme

Colors:
├── Primary Color: #667eea
├── Secondary Color: #764ba2
├── Success Color: #28a745
├── Warning Color: #ffc107
├── Danger Color: #dc3545
└── Apply Theme: ✅ Applied
```

---

## ✅ TESTING & VERIFICATION

### Testing Checklist

#### **1. Role Access Testing**

```
Test SuperAdmin:
□ Login as SuperAdmin
□ Access User Management
□ Access System Settings
□ View all reports
□ Access all clinics

Test ClinicAdmin:
□ Login as ClinicAdmin
□ Access clinic data only
□ Cannot access other clinics
□ Can create users in clinic
□ Can create policies/KPIs

Test DepartmentHead:
□ Login as DepartmentHead
□ See only assigned department
□ Can manage department policies
□ Can execute checklists
□ Can view staff

Test DepartmentUser:
□ Login as DepartmentUser
□ Cannot access admin functions
□ Can execute assigned checklists
□ Can enter KPI data
□ Can upload documents

Test Auditor:
□ Login as Auditor
□ Can view audit logs
□ Cannot modify data
□ Can generate reports
□ Cannot access admin settings
```

#### **2. Feature Testing**

```
Policy Management:
□ Create policy
□ Upload evidence
□ Submit for approval
□ Approve policy
□ Track version history

KPI System:
□ Create KPI
□ Define targets
□ Enter KPI data
□ View analytics
□ Generate reports

Checklists:
□ Create checklist template
□ Assign to department
□ Execute checklist
□ Submit with evidence
□ Approve execution

HR Module:
□ Add staff record
□ Upload documents
□ Set expiry date
□ Monitor expiry
□ Generate HR report
```

#### **3. Notification Testing**

```
Email Notifications:
□ Policy approval email sent
□ Document expiry alert sent
□ Checklist reminder email
□ System alert email
□ Report delivery email

In-App Notifications:
□ Notification appears in dashboard
□ Notification count updated
□ Mark as read option
□ Clear notification option
```

#### **4. Reporting Testing**

```
Generate Reports:
□ Compliance Report (PDF)
□ KPI Report (Excel)
□ Audit Report (CSV)
□ Checklist Report (PDF)
□ HR Report (Excel)

Report Features:
□ Date range filtering
□ Department selection
□ Include charts option
□ Include audit trail
□ Export successfully
```

---

## 🔍 VERIFICATION CHECKLIST

### System Readiness Check

```
✅ Database: All tables created
✅ Roles: 8 roles configured with permissions
✅ Admin User: SuperAdmin account active
✅ Demo Clinic: Sample clinic with departments
✅ Email: SMTP configured and tested
✅ Notifications: All notification types enabled
✅ Audit: Audit trail active and logging
✅ Reports: All report types configured
✅ Branding: Logo and colors applied
✅ Security: SSL/HTTPS configured
✅ Backups: Backup schedule configured
```

---

## 🎓 USER TRAINING GUIDE

### For SuperAdmin
- System administration
- User management
- Role assignment
- System settings
- Backup procedures

### For ClinicAdmin
- Clinic policy management
- KPI creation and monitoring
- Checklist creation
- Staff management
- Report generation
- User creation within clinic

### For DepartmentHead
- Department policy management
- KPI monitoring
- Checklist oversight
- Staff supervision
- Department reporting

### For DepartmentUser
- Checklist execution
- KPI data entry
- Document upload
- Daily operations

### For HR Manager
- Staff record management
- Document verification
- Expiry tracking
- HR reporting

### For Compliance Officer
- Compliance monitoring
- Policy approval
- Checklist approval
- Report generation

---

## 📞 SUPPORT & TROUBLESHOOTING

### Common Issues

**Issue:** Cannot send emails
```
Solution:
1. Check SMTP configuration in System Settings
2. Verify credentials
3. Enable "Less secure apps" (if Gmail)
4. Check firewall/network
5. Review email logs
```

**Issue:** Notifications not appearing
```
Solution:
1. Enable notifications in Feature Settings
2. Check user notification preferences
3. Verify email configuration
4. Check notification history logs
```

**Issue:** Users cannot access features
```
Solution:
1. Verify user role assignment
2. Check role permissions
3. Confirm clinic/department assignment
4. Review audit logs for errors
```

---

## ✨ SYSTEM IS FULLY CONFIGURED

Your CBAHI Portal is now ready for professional use with:

✅ 8 Professional Roles
✅ Complete Feature Set
✅ Professional Branding
✅ Email Notifications
✅ Advanced Reporting
✅ Analytics & Insights
✅ Complete Audit Trail
✅ Enterprise Security

**Start managing your clinic compliance today!**

