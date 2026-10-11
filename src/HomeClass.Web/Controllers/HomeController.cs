using System.Diagnostics;
using HomeClass.Domain.Constants;
using HomeClass.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace HomeClass.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole(Roles.Admin)) return RedirectToAction("Index", "Panel", new { area = "Admin" });
            if (User.IsInRole(Roles.Docente)) return RedirectToAction("Index", "Panel", new { area = "Docente" });
            if (User.IsInRole(Roles.Estudiante)) return RedirectToAction("Index", "Panel", new { area = "Estudiante" });
        }
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
