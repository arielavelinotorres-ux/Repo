using HomeClass.Application.Services;
using HomeClass.Domain.Constants;
using HomeClass.Web.Controllers;
using HomeClass.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeClass.Web.Areas.Docente.Controllers;

[Area("Docente"), Authorize(Roles = Roles.Docente)]
public class RiesgoController : DocenteBaseController
{
    private readonly IRiesgoService _riesgo;

    public RiesgoController(IRiesgoService riesgo, IPerfilService perfil) : base(perfil) => _riesgo = riesgo;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var docenteId = await DocenteIdAsync();
        if (docenteId is null) return SinPerfil();
        return View(await _riesgo.ObtenerReporteAsync(docenteId, ct));
    }

    public async Task<IActionResult> Exportar(CancellationToken ct)
    {
        var docenteId = await DocenteIdAsync();
        if (docenteId is null) return SinPerfil();

        var reporte = await _riesgo.ObtenerReporteAsync(docenteId, ct);
        return File(Ui.CsvRiesgo(reporte), "text/csv", $"mis-alumnos-riesgo-{DateTime.Today:yyyyMMdd}.csv");
    }
}
