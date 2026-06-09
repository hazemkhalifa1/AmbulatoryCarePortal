using AmbulatoryCarePortal.Application.Interfaces;
using AmbulatoryCarePortal.Domain.Entities;
using AmbulatoryCarePortal.Infrastructure.Data;
using AmbulatoryCarePortal.Infrastructure.Data.Seed;
using AmbulatoryCarePortal.Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AmbulatoryCarePortal.Presentation.Areas.SuperAdmin.Controllers;

[Area("SuperAdmin")]
[Authorize(Roles = "SuperAdmin")]
public class UserManagementController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<UserManagementController> _logger;

    public UserManagementController(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IUnitOfWork unitOfWork,
        AppDbContext dbContext,
        ILogger<UserManagementController> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
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
            AvailableDepartments = await GetAvailableDepartmentsAsync(),
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

        var currentRoles = await _userManager.GetRolesAsync(user);
        IdentityResult result;

        if (!string.IsNullOrEmpty(model.SelectedRole) &&
            !currentRoles.Contains(model.SelectedRole))
        {
            // Batch all role changes + user property updates in a single SaveChangesAsync
            // to avoid the timeout caused by multiple individual saves per UserManager call.

            // Remove all current role assignments directly via DbContext
            var userRoles = await _dbContext.Set<IdentityUserRole<string>>()
                .Where(ur => ur.UserId == user.Id)
                .ToListAsync();

            _dbContext.Set<IdentityUserRole<string>>().RemoveRange(userRoles);

            // Add the new role assignment
            var role = await _roleManager.FindByNameAsync(model.SelectedRole);
            if (role != null)
            {
                _dbContext.Set<IdentityUserRole<string>>().Add(new IdentityUserRole<string>
                {
                    UserId = user.Id,
                    RoleId = role.Id
                });
            }

            // Update the user's concurrency stamp so Identity's change tracking is consistent
            user.ConcurrencyStamp = Guid.NewGuid().ToString();

            // Mark the user entity as modified
            _dbContext.Entry(user).State = EntityState.Modified;

            // Single save for all changes
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"User {user.Email} updated with role {model.SelectedRole}");
            TempData["SuccessMessage"] = "User updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        // No role change — just update user properties
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

        _logger.LogInformation($"User {user.Email} updated");
        TempData["SuccessMessage"] = "User updated successfully!";
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

    private async Task<List<DepartmentViewModel>> GetAvailableDepartmentsAsync()
    {
        var departments = await _unitOfWork.Repository<Department>().GetAllAsync();
        return departments.Select(d => new DepartmentViewModel
        {
            Id = d.Id,
            Name = d.NameEn,
            ClinicId = d.ClinicId
        }).ToList();
    }
}
