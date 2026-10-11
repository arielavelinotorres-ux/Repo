using HomeClass.Application.Dtos;
using HomeClass.Application.Services;
using HomeClass.Domain.Constants;
using HomeClass.Domain.Enums;
using HomeClass.Web.Controllers;
using HomeClass.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeClass.Web.Areas.Docente.Controllers;

/// <summary>
/// Todo lo que hace un docente sobre SUS cursos. Cada llamada a los servicios incluye su docenteId,
/// así que un docente nunca puede ver ni modificar cursos de otro.
/// </summary>
[Area("Docente"), Authorize(Roles = Roles.Docente)]
public class CursosController : DocenteBaseController
{
    private readonly ICursoService _cursos;
    private readonly ICalificacionService _calificaciones;
    private readonly IAsistenciaService _asistencia;

    public CursosController(ICursoService cursos, ICalificacionService calificaciones, IAsistenciaService asistencia, IPerfilService perfil)
        : base(perfil)
    {
        _cursos = cursos;
        _calificaciones = calificaciones;
        _asistencia = asistencia;
    }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var docenteId = await DocenteIdAsync();
        if (docenteId is null) return SinPerfil();
        return View(await _cursos.ListarAsync(docenteId, ct));
    }

    public async Task<IActionResult> Detalle(int id, CancellationToken ct)
    {
        var docenteId = await DocenteIdAsync();
        if (docenteId is null) return SinPerfil();

        var curso = await _cursos.ObtenerDetalleAsync(id, docenteId, ct);
        return curso is null ? NotFound() : View(curso);
    }

    // ---------- Crear / editar / eliminar ----------
    public IActionResult Crear() => View("Formulario", new CursoForm());

    [HttpPost]
    public async Task<IActionResult> Crear(CursoForm form, CancellationToken ct)
    {
        var docenteId = await DocenteIdAsync();
        if (docenteId is null) return SinPerfil();
        if (!ModelState.IsValid) return View("Formulario", form);

        var res = await _cursos.GuardarAsync(new GuardarCursoCommand(null, form.Codigo, form.Nombre, form.Descripcion, docenteId, form.Activo), docenteId, ct);
        if (!res.Exitoso)
        {
            foreach (var e in res.Errores) ModelState.AddModelError("", e);
            return View("Formulario", form);
        }

        Exito($"El curso {form.Nombre} se creó. Ahora puedes matricular estudiantes.");
        return RedirectToAction(nameof(Matricula), new { id = res.Valor });
    }

    public async Task<IActionResult> Editar(int id, CancellationToken ct)
    {
        var docenteId = await DocenteIdAsync();
        if (docenteId is null) return SinPerfil();

        var c = await _cursos.ObtenerParaEditarAsync(id, docenteId, ct);
        if (c is null) return NotFound();
        return View("Formulario", new CursoForm { Id = c.Id, Codigo = c.Codigo, Nombre = c.Nombre, Descripcion = c.Descripcion, DocenteId = c.DocenteId, Activo = c.Activo });
    }

    [HttpPost]
    public async Task<IActionResult> Editar(CursoForm form, CancellationToken ct)
    {
        var docenteId = await DocenteIdAsync();
        if (docenteId is null) return SinPerfil();
        if (!ModelState.IsValid) return View("Formulario", form);

        var res = await _cursos.GuardarAsync(new GuardarCursoCommand(form.Id, form.Codigo, form.Nombre, form.Descripcion, docenteId, form.Activo), docenteId, ct);
        if (!res.Exitoso)
        {
            foreach (var e in res.Errores) ModelState.AddModelError("", e);
            return View("Formulario", form);
        }

        Exito("El curso se actualizó.");
        return RedirectToAction(nameof(Detalle), new { id = form.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        var docenteId = await DocenteIdAsync();
        if (docenteId is null) return SinPerfil();

        var res = await _cursos.EliminarAsync(id, docenteId, ct);
        if (res.Exitoso) Exito("El curso se eliminó junto con sus notas y asistencias.");
        else Fallo(res.PrimerError);
        return RedirectToAction(nameof(Index));
    }

    // ---------- Matrícula ----------
    public async Task<IActionResult> Matricula(int id, CancellationToken ct)
    {
        var docenteId = await DocenteIdAsync();
        if (docenteId is null) return SinPerfil();

        var m = await _cursos.ObtenerMatriculaAsync(id, docenteId, ct);
        return m is null ? NotFound() : View(m);
    }

    [HttpPost]
    public async Task<IActionResult> Matricula(int id, int[] estudianteIds, CancellationToken ct)
    {
        var docenteId = await DocenteIdAsync();
        if (docenteId is null) return SinPerfil();

        var res = await _cursos.MatricularAsync(id, estudianteIds ?? [], docenteId, ct);
        if (res.Exitoso) Exito("La matrícula se guardó.");
        else Fallo(res.PrimerError);
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // ---------- Libro de calificaciones ----------
    public async Task<IActionResult> Calificaciones(int id, CancellationToken ct)
    {
        var docenteId = await DocenteIdAsync();
        if (docenteId is null) return SinPerfil();

        var libro = await _calificaciones.ObtenerLibroAsync(id, docenteId, ct);
        return libro is null ? NotFound() : View(libro);
    }

    [HttpPost]
    public async Task<IActionResult> GuardarEvaluacion(EvaluacionForm form, CancellationToken ct)
    {
        var docenteId = await DocenteIdAsync();
        if (docenteId is null) return SinPerfil();

        if (!ModelState.IsValid)
        {
            Fallo(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Revisa los datos de la evaluación.");
            return RedirectToAction(nameof(Calificaciones), new { id = form.CursoId });
        }

        var res = await _calificaciones.GuardarEvaluacionAsync(
            new GuardarEvaluacionCommand(form.Id, form.CursoId, form.Titulo, form.Tipo, form.Ponderacion, form.Fecha), docenteId, ct);

        if (res.Exitoso) Exito(form.Id is null ? "La evaluación se agregó al libro." : "La evaluación se actualizó.");
        else Fallo(res.PrimerError);
        return RedirectToAction(nameof(Calificaciones), new { id = form.CursoId });
    }

    [HttpPost]
    public async Task<IActionResult> EliminarEvaluacion(int id, int cursoId, CancellationToken ct)
    {
        var docenteId = await DocenteIdAsync();
        if (docenteId is null) return SinPerfil();

        var res = await _calificaciones.EliminarEvaluacionAsync(id, docenteId, ct);
        if (res.Exitoso) Exito("La evaluación y sus notas se eliminaron.");
        else Fallo(res.PrimerError);
        return RedirectToAction(nameof(Calificaciones), new { id = cursoId });
    }

    /// <summary>Guarda una sola nota (llamada desde el libro con fetch). Devuelve los promedios recalculados.</summary>
    [HttpPost]
    public async Task<IActionResult> GuardarNota([FromBody] NotaRequest req, CancellationToken ct)
    {
        var docenteId = await DocenteIdAsync();
        if (docenteId is null) return Forbid();

        var res = await _calificaciones.GuardarNotaAsync(req.EvaluacionId, req.EstudianteId, req.Nota, docenteId, ct);
        if (!res.Exitoso) return BadRequest(new { error = res.PrimerError });

        return Json(new
        {
            promedioEstudiante = res.Valor!.PromedioEstudiante,
            promedioGrupo = res.Valor.PromedioGrupoEvaluacion
        });
    }

    // ---------- Asistencia ----------
    public async Task<IActionResult> Asistencia(int id, DateTime? fecha, CancellationToken ct)
    {
        var docenteId = await DocenteIdAsync();
        if (docenteId is null) return SinPerfil();

        var dia = (fecha ?? DateTime.Today).Date;
        if (dia > DateTime.Today) dia = DateTime.Today;

        var sesion = await _asistencia.ObtenerSesionAsync(id, dia, docenteId, ct);
        return sesion is null ? NotFound() : View(sesion);
    }

    [HttpPost]
    public async Task<IActionResult> Asistencia(int id, DateTime fecha, Dictionary<int, int> estados, CancellationToken ct)
    {
        var docenteId = await DocenteIdAsync();
        if (docenteId is null) return SinPerfil();

        var mapa = (estados ?? new Dictionary<int, int>())
            .Where(kv => Enum.IsDefined(typeof(EstadoAsistencia), kv.Value))
            .ToDictionary(kv => kv.Key, kv => (EstadoAsistencia)kv.Value);

        var res = await _asistencia.GuardarSesionAsync(id, fecha, mapa, docenteId, ct);
        if (res.Exitoso) Exito($"Asistencia del {fecha:dd MMM yyyy} guardada.");
        else Fallo(res.PrimerError);
        return RedirectToAction(nameof(Asistencia), new { id, fecha = fecha.ToString("yyyy-MM-dd") });
    }
}
