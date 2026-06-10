using System.Text.Json;

namespace AmbulatoryCarePortal.Presentation.Helpers;

public class TranslationService : ITranslationService
{
    private static readonly Dictionary<string, Dictionary<string, string>> _translations = new();
    private static readonly object _lock = new();
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _env;

    public TranslationService(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env)
    {
        _httpContextAccessor = httpContextAccessor;
        _env = env;
        LoadTranslations();
    }

    public string CurrentLanguage
    {
        get
        {
            if (_httpContextAccessor.HttpContext?.Request.Cookies.TryGetValue("lang", out var lang) == true && lang == "ar")
                return "ar";
            return "en";
        }
    }

    public string T(string key, params object[] args)
    {
        var lang = CurrentLanguage;
        if (_translations.TryGetValue(lang, out var langDict) && langDict.TryGetValue(key, out var value))
            return args.Length > 0 ? string.Format(value, args) : value;

        if (lang != "en" && _translations.TryGetValue("en", out var enDict) && enDict.TryGetValue(key, out var enValue))
            return args.Length > 0 ? string.Format(enValue, args) : enValue;

        return key;
    }

    private void LoadTranslations()
    {
        if (_translations.Count > 0) return;

        lock (_lock)
        {
            if (_translations.Count > 0) return;

            var translationsPath = Path.Combine(_env.ContentRootPath, "Resources");
            var files = new[] { ("en", "translations.en.json"), ("ar", "translations.ar.json") };

            foreach (var (lang, filename) in files)
            {
                var filePath = Path.Combine(translationsPath, filename);
                try
                {
                    if (File.Exists(filePath))
                    {
                        var json = File.ReadAllText(filePath);
                        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                        _translations[lang] = dict ?? new Dictionary<string, string>();
                    }
                    else
                    {
                        _translations[lang] = GetDefaultTranslations(lang);
                    }
                }
                catch
                {
                    _translations[lang] = GetDefaultTranslations(lang);
                }
            }
        }
    }

    private static Dictionary<string, string> GetDefaultTranslations(string lang)
    {
        return lang == "ar" ? GetDefaultArabic() : GetDefaultEnglish();
    }

    private static Dictionary<string, string> GetDefaultEnglish()
    {
        return new Dictionary<string, string>
        {
            ["App.Name"] = "CBAHI Ambulatory Care",
            ["App.Title"] = "CBAHI Ambulatory Care Portal",
            ["App.Tagline"] = "Enterprise compliance management platform for healthcare facilities. Streamline accreditation, monitor KPIs, and manage credentials.",
            ["App.Copyright"] = "© 2024 CBAHI. All rights reserved. Saudi Healthcare Compliance.",
            ["App.Version"] = "Version",

            ["Login.SecureLogin"] = "Secure Login",
            ["Login.WelcomeBack"] = "Welcome Back",
            ["Login.Subtitle"] = "Sign in to access the compliance portal",
            ["Login.Email"] = "Email Address",
            ["Login.EmailPlaceholder"] = "Enter your email",
            ["Login.Password"] = "Password",
            ["Login.PasswordPlaceholder"] = "Enter your password",
            ["Login.RememberMe"] = "Remember me",
            ["Login.ForgotPassword"] = "Forgot password?",
            ["Login.SignIn"] = "Sign In",
            ["Login.SigningIn"] = "Signing in...",
            ["Login.Footer"] = "© 2024 CBAHI Ambulatory Care Compliance Portal",

            ["Login.Feature.Policy"] = "Policy & Document Management",
            ["Login.Feature.KPI"] = "KPI Monitoring & Analytics",
            ["Login.Feature.Staff"] = "Staff Credentialing & Compliance",
            ["Login.Feature.Audit"] = "Automated Audit Readiness",

            ["ForgotPassword.Title"] = "Forgot Password",
            ["ForgotPassword.Badge"] = "Password Reset",
            ["ForgotPassword.Heading"] = "Reset Your Password",
            ["ForgotPassword.Subtitle"] = "Enter your registered email address and we'll send you a link to reset your password.",
            ["ForgotPassword.FormTitle"] = "Forgot Password",
            ["ForgotPassword.FormSubtitle"] = "Enter your email to receive a reset link",
            ["ForgotPassword.EmailPlaceholder"] = "Enter your registered email",
            ["ForgotPassword.SendLink"] = "Send Reset Link",
            ["ForgotPassword.Sending"] = "Sending...",
            ["ForgotPassword.BackToLogin"] = "Back to Login",

            ["ResetPassword.Title"] = "Reset Password",
            ["ResetPassword.Badge"] = "New Password",
            ["ResetPassword.Heading"] = "Create New Password",
            ["ResetPassword.Subtitle"] = "Choose a strong password that meets the security requirements for your account.",
            ["ResetPassword.FormTitle"] = "Reset Password",
            ["ResetPassword.FormSubtitle"] = "Enter your new password below",
            ["ResetPassword.NewPassword"] = "New Password",
            ["ResetPassword.NewPasswordPlaceholder"] = "Min. 8 characters",
            ["ResetPassword.ConfirmPassword"] = "Confirm Password",
            ["ResetPassword.ConfirmPasswordPlaceholder"] = "Confirm your password",
            ["ResetPassword.Reset"] = "Reset Password",
            ["ResetPassword.Resetting"] = "Resetting...",
            ["ResetPassword.BackToLogin"] = "Back to Login",

            ["ResetConfirmation.Title"] = "Password Reset",
            ["ResetConfirmation.Heading"] = "Password Reset Complete",
            ["ResetConfirmation.Subtitle"] = "Your password has been successfully updated. You can now sign in with your new credentials.",
            ["ResetConfirmation.Success"] = "Password Reset Successful",
            ["ResetConfirmation.Message"] = "Your password has been reset successfully. You can now login with your new password.",
            ["ResetConfirmation.SignIn"] = "Sign In",

            ["Nav.Dashboard"] = "Dashboard",
            ["Nav.Policies"] = "Policies",
            ["Nav.KPIMonitoring"] = "KPI Monitoring",
            ["Nav.Checklists"] = "Checklists",
            ["Nav.FormsLibrary"] = "Forms Library",
            ["Nav.HRCredentialing"] = "HR Credentialing",
            ["Nav.Notifications"] = "Notifications",
            ["Nav.Reports"] = "Reports",
            ["Nav.ManageClinics"] = "Manage Clinics",
            ["Nav.AuditLogs"] = "Audit Logs",
            ["Nav.MainMenu"] = "Main Menu",
            ["Nav.Staff"] = "Staff",
            ["Nav.ManageDocuments"] = "Documents Managment",
            ["Nav.Communication"] = "Communication",
            ["Nav.ManageUsers"] = "User Mangement",

            ["Nav.System"] = "System",

            ["User.MyProfile"] = "My Profile",
            ["User.Logout"] = "Logout",

            ["Common.Active"] = "Active",
            ["Common.Inactive"] = "Inactive",
            ["Common.Compliant"] = "Compliant",
            ["Common.NonCompliant"] = "Non-Compliant",
            ["Common.View"] = "View",
            ["Common.Edit"] = "Edit",
            ["Common.Delete"] = "Delete",
            ["Common.Create"] = "Create",
            ["Common.Cancel"] = "Cancel",
            ["Common.Save"] = "Save",
            ["Common.Search"] = "Search",
            ["Common.Reset"] = "Reset",
            ["Common.Loading"] = "Loading...",
            ["Common.NoData"] = "No data found.",
            ["Common.Actions"] = "Actions",
            ["Common.Status"] = "Status",
            ["Common.All"] = "All",
            ["Common.Showing"] = "Showing",
            ["Common.Of"] = "of",
            ["Common.Page"] = "Page",

            ["Notification.ViewAll"] = "View All Notifications",
            ["Notification.NoNotifications"] = "No notifications",

            ["Error.AccessDenied"] = "Access Denied",
            ["Error.AccessDeniedMessage"] = "You do not have permission to access this resource. Please contact your administrator if you believe this is an error.",
            ["Error.ReturnHome"] = "Return to Home",
            ["Error.GenericTitle"] = "Something went wrong",
        };
    }

    private static Dictionary<string, string> GetDefaultArabic()
    {
        return new Dictionary<string, string>
        {
            ["App.Name"] = "سباهي - رعاية العيادات الخارجية",
            ["App.Title"] = "بوابة سباهي للرعاية الخارجية",
            ["App.Tagline"] = "منصة إدارة الامتثال المؤسسي للمنشآت الصحية. تبسيط الاعتماد، ومراقبة مؤشرات الأداء، وإدارة المؤهلات.",
            ["App.Copyright"] = "© 2024 سباهي. جميع الحقوق محفوظة. الامتثال الصحي السعودي.",
            ["App.Version"] = "الإصدار",

            ["Login.SecureLogin"] = "تسجيل دخول آمن",
            ["Login.WelcomeBack"] = "مرحباً بعودتك",
            ["Login.Subtitle"] = "سجل الدخول للوصول إلى بوابة الامتثال",
            ["Login.Email"] = "البريد الإلكتروني",
            ["Login.EmailPlaceholder"] = "أدخل بريدك الإلكتروني",
            ["Login.Password"] = "كلمة المرور",
            ["Login.PasswordPlaceholder"] = "أدخل كلمة المرور",
            ["Login.RememberMe"] = "تذكرني",
            ["Login.ForgotPassword"] = "نسيت كلمة المرور؟",
            ["Login.SignIn"] = "تسجيل الدخول",
            ["Login.SigningIn"] = "جاري تسجيل الدخول...",
            ["Login.Footer"] = "© 2024 سباهي - بوابة الامتثال للرعاية الخارجية",

            ["Login.Feature.Policy"] = "إدارة السياسات والوثائق",
            ["Login.Feature.KPI"] = "مراقبة مؤشرات الأداء والتحليلات",
            ["Login.Feature.Staff"] = "اعتماد المؤهلات والامتثال",
            ["Login.Feature.Audit"] = "الجاهزية الآلية للتدقيق",

            ["ForgotPassword.Title"] = "نسيت كلمة المرور",
            ["ForgotPassword.Badge"] = "إعادة تعيين كلمة المرور",
            ["ForgotPassword.Heading"] = "إعادة تعيين كلمة المرور",
            ["ForgotPassword.Subtitle"] = "أدخل بريدك الإلكتروني المسجل وسنرسل لك رابطاً لإعادة تعيين كلمة المرور.",
            ["ForgotPassword.FormTitle"] = "نسيت كلمة المرور",
            ["ForgotPassword.FormSubtitle"] = "أدخل بريدك الإلكتروني لتلقي رابط إعادة التعيين",
            ["ForgotPassword.EmailPlaceholder"] = "أدخل بريدك الإلكتروني المسجل",
            ["ForgotPassword.SendLink"] = "إرسال رابط إعادة التعيين",
            ["ForgotPassword.Sending"] = "جاري الإرسال...",
            ["ForgotPassword.BackToLogin"] = "العودة إلى تسجيل الدخول",

            ["ResetPassword.Title"] = "إعادة تعيين كلمة المرور",
            ["ResetPassword.Badge"] = "كلمة مرور جديدة",
            ["ResetPassword.Heading"] = "إنشاء كلمة مرور جديدة",
            ["ResetPassword.Subtitle"] = "اختر كلمة مرور قوية تفي بمتطلبات الأمان لحسابك.",
            ["ResetPassword.FormTitle"] = "إعادة تعيين كلمة المرور",
            ["ResetPassword.FormSubtitle"] = "أدخل كلمة المرور الجديدة أدناه",
            ["ResetPassword.NewPassword"] = "كلمة المرور الجديدة",
            ["ResetPassword.NewPasswordPlaceholder"] = "8 أحرف على الأقل",
            ["ResetPassword.ConfirmPassword"] = "تأكيد كلمة المرور",
            ["ResetPassword.ConfirmPasswordPlaceholder"] = "تأكيد كلمة المرور",
            ["ResetPassword.Reset"] = "إعادة تعيين كلمة المرور",
            ["ResetPassword.Resetting"] = "جاري إعادة التعيين...",
            ["ResetPassword.BackToLogin"] = "العودة إلى تسجيل الدخول",

            ["ResetConfirmation.Title"] = "إعادة تعيين كلمة المرور",
            ["ResetConfirmation.Heading"] = "تم إعادة تعيين كلمة المرور",
            ["ResetConfirmation.Subtitle"] = "تم تحديث كلمة المرور بنجاح. يمكنك الآن تسجيل الدخول ببياناتك الجديدة.",
            ["ResetConfirmation.Success"] = "تم إعادة تعيين كلمة المرور بنجاح",
            ["ResetConfirmation.Message"] = "تم إعادة تعيين كلمة المرور بنجاح. يمكنك الآن تسجيل الدخول بكلمة المرور الجديدة.",
            ["ResetConfirmation.SignIn"] = "تسجيل الدخول",

            ["Nav.Dashboard"] = "لوحة القيادة",
            ["Nav.Policies"] = "السياسات",
            ["Nav.KPIMonitoring"] = "مراقبة مؤشرات الأداء",
            ["Nav.Checklists"] = "قوائم التحقق",
            ["Nav.FormsLibrary"] = "مكتبة النماذج",
            ["Nav.HRCredentialing"] = "اعتماد الموظفين",
            ["Nav.Notifications"] = "الإشعارات",
            ["Nav.Reports"] = "التقارير",
            ["Nav.ManageClinics"] = "إدارة العيادات",
            ["Nav.AuditLogs"] = "سجلات التدقيق",
            ["Nav.MainMenu"] = "القائمة الرئيسية",
            ["Nav.Staff"] = "الموظفين",
            ["Nav.ManageDocuments"] = "اداره الاوراق",
            ["Nav.ManageUsers"] = "إدارة المستخدمين",
            ["Nav.Communication"] = "التواصل",
            ["Nav.System"] = "النظام",

            ["User.MyProfile"] = "ملفي الشخصي",
            ["User.Logout"] = "تسجيل الخروج",

            ["Common.Active"] = "نشط",
            ["Common.Inactive"] = "غير نشط",
            ["Common.Compliant"] = "ممتثل",
            ["Common.NonCompliant"] = "غير ممتثل",
            ["Common.View"] = "عرض",
            ["Common.Edit"] = "تعديل",
            ["Common.Delete"] = "حذف",
            ["Common.Create"] = "إنشاء",
            ["Common.Cancel"] = "إلغاء",
            ["Common.Save"] = "حفظ",
            ["Common.Search"] = "بحث",
            ["Common.Reset"] = "إعادة تعيين",
            ["Common.Loading"] = "جاري التحميل...",
            ["Common.NoData"] = "لا توجد بيانات.",
            ["Common.Actions"] = "الإجراءات",
            ["Common.Status"] = "الحالة",
            ["Common.All"] = "الكل",
            ["Common.Showing"] = "عرض",
            ["Common.Of"] = "من",
            ["Common.Page"] = "صفحة",

            ["Notification.ViewAll"] = "عرض جميع الإشعارات",
            ["Notification.NoNotifications"] = "لا توجد إشعارات",

            ["Error.AccessDenied"] = "تم رفض الوصول",
            ["Error.AccessDeniedMessage"] = "ليس لديك صلاحية للوصول إلى هذا المورد. يرجى الاتصال بالمسؤول إذا كنت تعتقد أن هذا خطأ.",
            ["Error.ReturnHome"] = "العودة إلى الرئيسية",
            ["Error.GenericTitle"] = "حدث خطأ ما",
        };
    }
}
