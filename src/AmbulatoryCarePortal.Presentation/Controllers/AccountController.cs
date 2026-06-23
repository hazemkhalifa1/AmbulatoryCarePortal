using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Presentation.ViewModels;
using AmbulatoryCarePortal.Presentation.Helpers;
using AmbulatoryCarePortal.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AmbulatoryCarePortal.Presentation.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<AccountController> _logger;
    private readonly ITranslationService _localizer;
    private readonly IEmailService _emailService;

    public AccountController(
        SignInManager<AppUser> signInManager,
        UserManager<AppUser> userManager,
        ILogger<AccountController> logger,
        ITranslationService localizer,
        IEmailService emailService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
        _localizer = localizer;
        _emailService = emailService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true
            );

            if (result.Succeeded)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("User {Email} logged in successfully.", model.Email);
                return LocalRedirect(returnUrl ?? "/");
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {Email} account locked.", model.Email);
                ModelState.AddModelError(string.Empty, _localizer.T("Alert.Error.LoginLocked"));
            }
            else if (result.RequiresTwoFactor)
            {
                return RedirectToPage("/Account/LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
            }
            else
            {
                ModelState.AddModelError(string.Empty, _localizer.T("Alert.Error.LoginFailed"));
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");
        return RedirectToAction("Login");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("Login")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);

        if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
        {
            TempData["SuccessMessage"] = _localizer.T("Alert.Info.PasswordResetSent");
            return RedirectToAction(nameof(Login));
        }

        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        var callbackUrl = Url.Action(
            nameof(ResetPassword),
            "Account",
            new { email = user.Email, code },
            Request.Scheme
        );

        var sent = await _emailService.SendPasswordResetEmailAsync(user.Email, callbackUrl);

        if (sent)
        {
            _logger.LogInformation("Password reset email sent to {Email}", user.Email);
            TempData["SuccessMessage"] = _localizer.T("Alert.Success.PasswordResetEmail");
        }
        else
        {
            _logger.LogError("Failed to send password reset email to {Email}", user.Email);
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.PasswordResetSendFailed");
        }

        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email = null, string? code = null)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(code))
            return BadRequest("Invalid password reset link.");

        var model = new ResetPasswordViewModel
        {
            Email = email,
            Code = code
        };

        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
            return RedirectToAction(nameof(ResetPasswordConfirmation));

        var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
        if (result.Succeeded)
        {
            _logger.LogInformation("Password reset for user {Email}", model.Email);
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ConfirmEmail(string? userId = null, string? code = null)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(code))
            return BadRequest("Invalid email confirmation link.");

        var model = new ConfirmEmailViewModel { UserId = userId, Code = code };
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null)
            return NotFound();

        var result = await _userManager.ConfirmEmailAsync(user, model.Code);
        if (result.Succeeded)
        {
            _logger.LogInformation("Email confirmed for user {Email}", user.Email);
            TempData["SuccessMessage"] = _localizer.T("Alert.Success.EmailConfirmed");
            return RedirectToAction(nameof(Login));
        }

        TempData["ErrorMessage"] = _localizer.T("Alert.Error.EmailConfirmationFailed");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendConfirmationEmail()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound();

        if (user.EmailConfirmed)
        {
            TempData["InfoMessage"] = _localizer.T("Alert.Info.EmailAlreadyConfirmed");
            return RedirectToAction("Profile");
        }

        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var callbackUrl = Url.Action(
            nameof(ConfirmEmail),
            "Account",
            new { userId = user.Id, code },
            Request.Scheme
        );

        await _emailService.SendEmailAsync(user.Email, "Confirm your email",
            $"Please confirm your email by clicking <a href='{callbackUrl}'>here</a>.");

        _logger.LogInformation("Email confirmation resent to {Email}", user.Email);
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.ConfirmationEmailSent");

        return RedirectToAction("Profile");
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var model = new ProfileViewModel
        {
            FullNameEn = user.FullNameEn,
            FullNameAr = user.FullNameAr,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = roles.FirstOrDefault(),
            LastLoginAt = user.LastLoginAt,
            EmailConfirmed = user.EmailConfirmed
        };

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
