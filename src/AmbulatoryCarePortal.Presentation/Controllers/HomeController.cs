using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmbulatoryCarePortal.Presentation.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            if (User.IsInRole("SuperAdmin"))
                return RedirectToAction("Index", "Dashboard", new { area = "SuperAdmin" });

            if (User.IsInRole("ClinicAdmin") || User.IsInRole("ClinicViewer"))
                return RedirectToAction("Index", "Dashboard", new { area = "ClinicAdmin" });
        }

        return RedirectToAction("Login", "Account");
    }

    [AllowAnonymous]
    public IActionResult Error()
    {
        return View();
    }
}
