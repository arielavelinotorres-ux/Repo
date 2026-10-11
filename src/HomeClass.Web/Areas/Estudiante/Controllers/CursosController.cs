using HomeClass.Application.Services;
using HomeClass.Domain.Constants;
using HomeClass.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeClass.Web.Areas.Estudiante.Controllers;

[Area("Estudiante"), Authorize(Roles = Roles.Estudiante)]
public class CursosController : BaseController
{
    private readonly IPortalEstudianteService _portal;
    public CursosController(IPortalEstudianteService portal) => _portal = portal;

    public async Task<IActionResult> Index(CancellationToken ct) => View(await _portal.ListarCursosAsync(UserId, ct));

    public async Task<IActionResult> Detalle(int id, CancellationToken ct)
    {
        var curso = await _portal.ObtenerCursoAsync(UserId, id, ct);
        return curso is null ? NotFound() : View(curso);
    }
}
