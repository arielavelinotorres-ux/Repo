using HomeClass.Application.Dtos;
using HomeClass.Application.Services;
using HomeClass.Domain.Constants;
using HomeClass.Web.Controllers;
using HomeClass.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeClass.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = Roles.Admin)]
public class CursosController : BaseController
{
    private readonly ICursoService _cursos;
    public CursosController(ICursoService cursos) => _cursos = cursos;

    public async Task<IActionResult> Index(CancellationToken ct) => View(await _cursos.ListarAsync(ct: ct));

    public async Task<IActionResult> Crear(CancellationToken ct)
    {
        await CargarDocentesAsync(ct);
        return View("Formulario", new CursoForm());
    }

    [HttpPost]
    public async Task<IActionResult> Crear(CursoForm form, CancellationToken ct)
    {
        if (form.DocenteId is null) ModelState.AddModelError(nameof(form.DocenteId), "Selecciona el docente responsable.");
        if (!ModelState.IsValid) { await CargarDocentesAsync(ct); return View("Formulario", form); }

        var res = await _cursos.GuardarAsync(new GuardarCursoCommand(null, form.Codigo, form.Nombre, form.Descripcion, form.DocenteId, form.Activo), null, ct);
        if (!res.Exitoso)
        {
            foreach (var e in res.Errores) ModelState.AddModelError("", e);
            await CargarDocentesAsync(ct);
            return View("Formulario", form);
        }

        Exito($"El curso {form.Nombre} se creó. Ahora puedes matricular estudiantes.");
        return RedirectToAction(nameof(Matricula), new { id = res.Valor });
    }

    public async Task<IActionResult> Editar(int id, CancellationToken ct)
    {
        var c = await _cursos.ObtenerParaEditarAsync(id, null, ct);
        if (c is null) return NotFound();

        await CargarDocentesAsync(ct);
        return View("Formulario", new CursoForm { Id = c.Id, Codigo = c.Codigo, Nombre = c.Nombre, Descripcion = c.Descripcion, DocenteId = c.DocenteId, Activo = c.Activo });
    }

    [HttpPost]
    public async Task<IActionResult> Editar(CursoForm form, CancellationToken ct)
    {
        if (form.DocenteId is null) ModelState.AddModelError(nameof(form.DocenteId), "Selecciona el docente responsable.");
        if (!ModelState.IsValid) { await CargarDocentesAsync(ct); return View("Formulario", form); }

        var res = await _cursos.GuardarAsync(new GuardarCursoCommand(form.Id, form.Codigo, form.Nombre, form.Descripcion, form.DocenteId, form.Activo), null, ct);
        if (!res.Exitoso)
        {
            foreach (var e in res.Errores) ModelState.AddModelError("", e);
            await CargarDocentesAsync(ct);
            return View("Formulario", form);
        }

        Exito("El curso se actualizó.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Matricula(int id, CancellationToken ct)
    {
        var m = await _cursos.ObtenerMatriculaAsync(id, null, ct);
        return m is null ? NotFound() : View(m);
    }

    [HttpPost]
    public async Task<IActionResult> Matricula(int id, int[] estudianteIds, CancellationToken ct)
    {
        var res = await _cursos.MatricularAsync(id, estudianteIds ?? [], null, ct);
        if (res.Exitoso) Exito("La matrícula se guardó.");
        else Fallo(res.PrimerError);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        var res = await _cursos.EliminarAsync(id, null, ct);
        if (res.Exitoso) Exito("El curso se eliminó junto con sus notas y asistencias.");
        else Fallo(res.PrimerError);
        return RedirectToAction(nameof(Index));
    }

    private async Task CargarDocentesAsync(CancellationToken ct) => ViewBag.Docentes = await _cursos.ListarDocentesAsync(ct);
}
