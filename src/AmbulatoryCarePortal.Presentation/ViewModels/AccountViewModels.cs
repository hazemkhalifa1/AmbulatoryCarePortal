namespace AmbulatoryCarePortal.Presentation.ViewModels;

public class ForgotPasswordViewModel
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class ConfirmEmailViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}

public class ProfileViewModel
{
    public string? FullNameEn { get; set; }
    public string? FullNameAr { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ClinicName { get; set; }
    public string? Role { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool EmailConfirmed { get; set; }
}
