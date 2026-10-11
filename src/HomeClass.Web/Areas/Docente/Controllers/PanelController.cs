using HomeClass.Application.Services;
using HomeClass.Domain.Constants;
using HomeClass.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeClass.Web.Areas.Docente.Controllers;

[Area("Docente"), Authorize(Roles = Roles.Docente)]
public class PanelController : BaseController
{
    private readonly IDashboardService _dashboard;
    public PanelController(IDashboardService dashboard) => _dashboard = dashboard;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = await _dashboard.ObtenerDocenteAsync(UserId, ct);
        return vm is null ? SinPerfil() : View(vm);
    }
}
