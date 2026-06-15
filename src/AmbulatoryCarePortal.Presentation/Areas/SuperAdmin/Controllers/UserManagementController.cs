using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Domain.Enums;
using AmbulatoryCarePortal.Infrastructure.Data;
using AmbulatoryCarePortal.Infrastructure.Data.Seed;
using AmbulatoryCarePortal.Presentation.ViewModels;
using AmbulatoryCarePortal.Presentation.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmbulatoryCarePortal.Presentation.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Policy = "Permission.users.manage")]
public class UserManagementController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<UserManagementController> _logger;
    private readonly ITranslationService _localizer;
    private readonly IEmailService _emailService;

    public UserManagementController(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IUnitOfWork unitOfWork,
        AppDbContext dbContext,
        ILogger<UserManagementController> logger,
        ITranslationService localizer,
        IEmailService emailService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _logger = logger;
        _localizer = localizer;
        _emailService = emailService;
    }

    /// <summary>
    /// List all users with their roles
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        var users = await _userManager.Users
            .OrderBy(u => u.UserName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var userViewModels = new List<UserRoleManagementViewModel>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userViewModels.Add(new UserRoleManagementViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullNameEn,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                SelectedRole = roles.FirstOrDefault() ?? "None",
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            });
        }

        ViewBag.TotalCount = await _userManager.Users.CountAsync();
        ViewBag.CurrentPage = page;

        return View(userViewModels);
    }

    /// <summary>
    /// Create new user with role selection
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new UserRoleManagementViewModel
        {
            AvailableRoles = await GetAvailableRolesAsync(),
            AvailableClinics = await GetAvailableClinicsAsync(),
            IsActive = true
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserRoleManagementViewModel model, string password)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await GetAvailableRolesAsync();
            model.AvailableClinics = await GetAvailableClinicsAsync();
            return View(model);
        }

        var user = new AppUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullNameEn = model.FullName,
            PhoneNumber = model.PhoneNumber,
            IsActive = model.IsActive,
            ClinicId = model.ClinicId,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            if (!string.IsNullOrEmpty(model.SelectedRole))
            {
                await _userManager.AddToRoleAsync(user, model.SelectedRole);
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Action(
                "ConfirmEmail",
                "Account",
                new { userId = user.Id, code },
                Request.Scheme
            );

            await _emailService.SendWelcomeEmailAsync(user.Email, model.FullName, password);
            await _emailService.SendEmailAsync(user.Email, "Confirm your email",
                $"Welcome to CBAHI Portal. Please confirm your email by clicking <a href='{callbackUrl}'>here</a>.");

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var auditLog = new AuditTrail
            {
                ClinicId = user.ClinicId ?? 0,
                ActionType = AuditActionType.Create,
                TargetObjectId = 0,
                TargetObjectType = nameof(AppUser),
                Description = $"Created user: {user.Email}",
                CreatedBy = userId,
                ActionDate = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"User {user.Email} created with role {model.SelectedRole}");
            TempData["SuccessMessage"] = _localizer.T("Alert.Success.UserCreated");

            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        model.AvailableRoles = await GetAvailableRolesAsync();
        model.AvailableClinics = await GetAvailableClinicsAsync();

        return View(model);
    }

    /// <summary>
    /// Edit user and change role
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        var clinic = await _unitOfWork.Repository<Clinic>().GetByIdAsync(user.ClinicId ?? 0);

        var model = new UserRoleManagementViewModel
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullNameEn,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            ClinicId = user.ClinicId,
            SelectedRole = roles.FirstOrDefault() ?? "None",
            AvailableRoles = await GetAvailableRolesAsync(),
            AvailableClinics = await GetAvailableClinicsAsync(),
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, UserRoleManagementViewModel model)
    {
        if (id != model.UserId)
            return BadRequest();

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            model.AvailableRoles = await GetAvailableRolesAsync();
            model.AvailableClinics = await GetAvailableClinicsAsync();
            return View(model);
        }

        user.Email = model.Email;
        user.UserName = model.Email;
        user.FullNameEn = model.FullName;
        user.PhoneNumber = model.PhoneNumber;
        user.IsActive = model.IsActive;
        user.ClinicId = model.ClinicId;

        var currentRoles = await _userManager.GetRolesAsync(user);
        IdentityResult result;

        if (!string.IsNullOrEmpty(model.SelectedRole) &&
            !currentRoles.Contains(model.SelectedRole))
        {
            var userRoles = await _dbContext.Set<IdentityUserRole<string>>()
                .Where(ur => ur.UserId == user.Id)
                .ToListAsync();

            _dbContext.Set<IdentityUserRole<string>>().RemoveRange(userRoles);

            var role = await _roleManager.FindByNameAsync(model.SelectedRole);
            if (role != null)
            {
                _dbContext.Set<IdentityUserRole<string>>().Add(new IdentityUserRole<string>
                {
                    UserId = user.Id,
                    RoleId = role.Id
                });
            }

            user.ConcurrencyStamp = Guid.NewGuid().ToString();
            _dbContext.Entry(user).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var auditLog = new AuditTrail
            {
                ClinicId = user.ClinicId ?? 0,
                ActionType = AuditActionType.Update,
                TargetObjectId = 0,
                TargetObjectType = nameof(AppUser),
                Description = $"Updated user: {user.Email}",
                CreatedBy = userId,
                ActionDate = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"User {user.Email} updated with role {model.SelectedRole}");
            TempData["SuccessMessage"] = _localizer.T("Alert.Success.UserUpdated");
            return RedirectToAction(nameof(Index));
        }

        result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            model.AvailableRoles = await GetAvailableRolesAsync();
            model.AvailableClinics = await GetAvailableClinicsAsync();
            return View(model);
        }

        var editUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var editAuditLog = new AuditTrail
        {
            ClinicId = user.ClinicId ?? 0,
            ActionType = AuditActionType.Update,
            TargetObjectId = 0,
            TargetObjectType = nameof(AppUser),
            Description = $"Updated user: {user.Email}",
            CreatedBy = editUserId,
            ActionDate = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        await _unitOfWork.Repository<AuditTrail>().AddAsync(editAuditLog);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation($"User {user.Email} updated");
        TempData["SuccessMessage"] = _localizer.T("Alert.Success.UserUpdated");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Delete user
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        if (user.Id == User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value)
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.UserDeleteOwn");
            return RedirectToAction(nameof(Index));
        }

        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Any())
        {
            await _userManager.RemoveFromRolesAsync(user, roles);
        }

        var result = await _userManager.DeleteAsync(user);

        if (result.Succeeded)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var auditLog = new AuditTrail
            {
                ClinicId = user.ClinicId ?? 0,
                ActionType = AuditActionType.Delete,
                TargetObjectId = 0,
                TargetObjectType = nameof(AppUser),
                Description = $"Deleted user: {user.Email}",
                CreatedBy = userId,
                ActionDate = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            await _unitOfWork.Repository<AuditTrail>().AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"User {user.Email} deleted");
            TempData["SuccessMessage"] = _localizer.T("Alert.Success.UserDeleted");
        }
        else
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.UserDeleteFailed");
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Reset user password
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ResetPassword(string id, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (result.Succeeded)
        {
            _logger.LogInformation($"Password reset for user {user.Email}");
            TempData["SuccessMessage"] = _localizer.T("Alert.Success.UserPasswordReset");
        }
        else
        {
            TempData["ErrorMessage"] = _localizer.T("Alert.Error.UserPasswordResetFailed");
        }

        return RedirectToAction(nameof(Edit), new { id });
    }

    /// <summary>
    /// View user activity and audit trail
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ActivityLog(string userId)
    {
        var auditLogs = await _unitOfWork.Repository<AuditTrail>().FindAsync(
            a => a.CreatedBy == userId
        );

        var logs = auditLogs.Select(l => new AuditLogViewModel
        {
            Id = l.Id,
            Action = l.Description,
            User = l.CreatedBy,
            ActionDate = l.ActionDate,
            IpAddress = l.IpAddress,
            OldValues = l.OldValues,
            NewValues = l.NewValues
        }).ToList();

        return View(logs);
    }

    /// <summary>
    /// Get available roles for dropdown
    /// </summary>
    private async Task<List<RoleOptionViewModel>> GetAvailableRolesAsync()
    {
        var roles = await _roleManager.Roles.ToListAsync();

        return roles.Select(role => new RoleOptionViewModel
        {
            RoleName = role.Name,
            Description = RoleDescriptions.Descriptions.ContainsKey(role.Name)
                ? RoleDescriptions.Descriptions[role.Name]
                : "No description available",
            Permissions = RoleDescriptions.ResponsibilitiesByRole.ContainsKey(role.Name)
                ? RoleDescriptions.ResponsibilitiesByRole[role.Name]
                : Array.Empty<string>()
        }).ToList();
    }

    private async Task<List<ClinicViewModel>> GetAvailableClinicsAsync()
    {
        var clinics = await _unitOfWork.Repository<Clinic>().GetAllAsync(false);

        return clinics.Select(c => new ClinicViewModel
        {
            Id = c.Id,
            Name = c.Name,
            NameAr = c.NameAr,
            CityEn = c.CityEn
        }).ToList();
    }

}
