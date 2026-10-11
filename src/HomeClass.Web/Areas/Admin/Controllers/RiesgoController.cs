using HomeClass.Application.Services;
using HomeClass.Domain.Constants;
using HomeClass.Web.Controllers;
using HomeClass.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeClass.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = Roles.Admin)]
public class RiesgoController : BaseController
{
    private readonly IRiesgoService _riesgo;
    public RiesgoController(IRiesgoService riesgo) => _riesgo = riesgo;

    public async Task<IActionResult> Index(CancellationToken ct) => View(await _riesgo.ObtenerReporteAsync(null, ct));

    public async Task<IActionResult> Exportar(CancellationToken ct)
    {
        var reporte = await _riesgo.ObtenerReporteAsync(null, ct);
        return File(Ui.CsvRiesgo(reporte), "text/csv", $"riesgo-academico-{DateTime.Today:yyyyMMdd}.csv");
    }
}
