using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Presentation.ViewModels;
using AmbulatoryCarePortal.Infrastructure.Data.Seed;

namespace AmbulatoryCarePortal.Presentation.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class UserManagementController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserManagementController> _logger;

    public UserManagementController(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IUnitOfWork unitOfWork,
        ILogger<UserManagementController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// List all users with their roles
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
    {
        var users = await _userManager.Users
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
            DepartmentId = model.DepartmentId,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            // Assign role if selected
            if (!string.IsNullOrEmpty(model.SelectedRole))
            {
                await _userManager.AddToRoleAsync(user, model.SelectedRole);
            }

            _logger.LogInformation($"User {user.Email} created with role {model.SelectedRole}");
            TempData["SuccessMessage"] = "User created successfully!";

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
            DepartmentId = user.DepartmentId,
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
        user.DepartmentId = model.DepartmentId;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            // Update role
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (!string.IsNullOrEmpty(model.SelectedRole) && 
                !currentRoles.Contains(model.SelectedRole))
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, model.SelectedRole);
            }

            _logger.LogInformation($"User {user.Email} updated with role {model.SelectedRole}");
            TempData["SuccessMessage"] = "User updated successfully!";

            return RedirectToAction(nameof(Index));
        }

        model.AvailableRoles = await GetAvailableRolesAsync();
        model.AvailableClinics = await GetAvailableClinicsAsync();

        return View(model);
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

        // Prevent deleting the current admin
        if (user.Id == User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value)
        {
            TempData["ErrorMessage"] = "Cannot delete your own account!";
            return RedirectToAction(nameof(Index));
        }

        var result = await _userManager.DeleteAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation($"User {user.Email} deleted");
            TempData["SuccessMessage"] = "User deleted successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to delete user!";
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
            TempData["SuccessMessage"] = "Password reset successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to reset password!";
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

    /// <summary>
    /// Get available clinics for dropdown
    /// </summary>
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
