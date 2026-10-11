using HomeClass.Application.Services;
using HomeClass.Domain.Constants;
using HomeClass.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeClass.Web.Areas.Estudiante.Controllers;

[Area("Estudiante"), Authorize(Roles = Roles.Estudiante)]
public class PanelController : BaseController
{
    private readonly IDashboardService _dashboard;
    public PanelController(IDashboardService dashboard) => _dashboard = dashboard;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = await _dashboard.ObtenerEstudianteAsync(UserId, ct);
        return vm is null ? SinPerfil() : View(vm);
    }
}
