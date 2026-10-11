using HomeClass.Application.Abstractions;
using HomeClass.Application.Common;
using HomeClass.Application.Dtos;
using HomeClass.Application.Services.Internal;
using HomeClass.Domain.Entities;
using HomeClass.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace HomeClass.Application.Services;

public interface ICalificacionService
{
    Task<LibroCalificacionesDto?> ObtenerLibroAsync(int cursoId, int? docenteId = null, CancellationToken ct = default);
    Task<Resultado<int>> GuardarEvaluacionAsync(GuardarEvaluacionCommand cmd, int? docenteId = null, CancellationToken ct = default);
    Task<Resultado> EliminarEvaluacionAsync(int evaluacionId, int? docenteId = null, CancellationToken ct = default);

    /// <param name="nota">null borra la nota existente.</param>
    Task<Resultado<NotaGuardadaDto>> GuardarNotaAsync(int evaluacionId, int estudianteId, decimal? nota, int? docenteId = null, CancellationToken ct = default);
}

public class CalificacionService : ICalificacionService
{
    private readonly IApplicationDbContext _db;
    public CalificacionService(IApplicationDbContext db) => _db = db;

    public async Task<LibroCalificacionesDto?> ObtenerLibroAsync(int cursoId, int? docenteId = null, CancellationToken ct = default)
    {
        var snap = await SnapshotRendimiento.CargarAsync(_db, docenteId: docenteId, cursoId: cursoId, ct: ct);
        var curso = snap.Curso(cursoId);
        if (curso is null) return null;

        var columnas = snap.Evaluaciones.OrderBy(e => e.Fecha).ThenBy(e => e.Id)
            .Select(e => new EvaluacionColumnaDto(e.Id, e.Titulo, e.Tipo, e.Ponderacion, e.Fecha,
                snap.PromedioEvaluacion(e.Id), snap.Notas.Count(n => n.EvaluacionId == e.Id)))
            .ToList();

        var filas = snap.EstudiantesDe(cursoId).Select(est => new FilaCalificacionDto(
            est.Id, est.Nombre, est.Carnet,
            snap.NotasDe(est.Id, cursoId).ToDictionary(n => n.EvaluacionId, n => n.Valor),
            snap.Promedio(est.Id, cursoId))).ToList();

        return new LibroCalificacionesDto(curso.Id, curso.Codigo, curso.Nombre, columnas, filas,
            columnas.Sum(c => c.Ponderacion));
    }

    public async Task<Resultado<int>> GuardarEvaluacionAsync(GuardarEvaluacionCommand cmd, int? docenteId = null, CancellationToken ct = default)
    {
        var titulo = cmd.Titulo.Trim();
        if (titulo.Length == 0) return Resultado<int>.Falla("El título es obligatorio.");
        if (cmd.Ponderacion <= 0 || cmd.Ponderacion > 100) return Resultado<int>.Falla("La ponderación debe estar entre 1 y 100.");

        var cursoOk = await _db.Cursos.AnyAsync(c => c.Id == cmd.CursoId && (docenteId == null || c.DocenteId == docenteId), ct);
        if (!cursoOk) return Resultado<int>.Falla("El curso no existe o no tienes permiso para modificarlo.");

        var otrosPesos = await _db.Evaluaciones
            .Where(e => e.CursoId == cmd.CursoId && e.Id != (cmd.Id ?? 0))
            .SumAsync(e => (decimal?)e.Ponderacion, ct) ?? 0m;
        if (otrosPesos + cmd.Ponderacion > 100m)
            return Resultado<int>.Falla($"Las ponderaciones del curso sumarían {otrosPesos + cmd.Ponderacion:0.#}%. El máximo es 100% (ya hay {otrosPesos:0.#}% asignado).");

        Evaluacion? evaluacion;
        if (cmd.Id is int id)
        {
            evaluacion = await _db.Evaluaciones.FirstOrDefaultAsync(e => e.Id == id && e.CursoId == cmd.CursoId, ct);
            if (evaluacion is null) return Resultado<int>.Falla("La evaluación ya no existe.");
        }
        else
        {
            evaluacion = new Evaluacion { CursoId = cmd.CursoId };
            _db.Evaluaciones.Add(evaluacion);
        }

        evaluacion.Titulo = titulo;
        evaluacion.Tipo = cmd.Tipo;
        evaluacion.Ponderacion = cmd.Ponderacion;
        evaluacion.Fecha = cmd.Fecha.Date;

        await _db.SaveChangesAsync(ct);
        return Resultado<int>.Ok(evaluacion.Id);
    }

    public async Task<Resultado> EliminarEvaluacionAsync(int evaluacionId, int? docenteId = null, CancellationToken ct = default)
    {
        var evaluacion = await _db.Evaluaciones
            .FirstOrDefaultAsync(e => e.Id == evaluacionId && (docenteId == null || e.Curso.DocenteId == docenteId), ct);
        if (evaluacion is null) return Resultado.Falla("La evaluación no existe o no tienes permiso para eliminarla.");

        _db.Evaluaciones.Remove(evaluacion); // sus notas se eliminan en cascada
        await _db.SaveChangesAsync(ct);
        return Resultado.Ok();
    }

    public async Task<Resultado<NotaGuardadaDto>> GuardarNotaAsync(int evaluacionId, int estudianteId, decimal? nota, int? docenteId = null, CancellationToken ct = default)
    {
        if (nota is < 0 or > 100) return Resultado<NotaGuardadaDto>.Falla("La nota debe estar entre 0 y 100.");

        var evaluacion = await _db.Evaluaciones.AsNoTracking()
            .Where(e => e.Id == evaluacionId && (docenteId == null || e.Curso.DocenteId == docenteId))
            .Select(e => new { e.Id, e.CursoId })
            .FirstOrDefaultAsync(ct);
        if (evaluacion is null) return Resultado<NotaGuardadaDto>.Falla("La evaluación no existe o no tienes permiso para calificarla.");

        var matriculado = await _db.Inscripciones.AnyAsync(i => i.CursoId == evaluacion.CursoId && i.EstudianteId == estudianteId, ct);
        if (!matriculado) return Resultado<NotaGuardadaDto>.Falla("El estudiante no está matriculado en este curso.");

        var existente = await _db.Calificaciones.FirstOrDefaultAsync(c => c.EvaluacionId == evaluacionId && c.EstudianteId == estudianteId, ct);
        if (nota is null)
        {
            if (existente is not null) _db.Calificaciones.Remove(existente);
        }
        else if (existente is null)
        {
            _db.Calificaciones.Add(new Calificacion { EvaluacionId = evaluacionId, EstudianteId = estudianteId, Nota = nota.Value });
        }
        else
        {
            existente.Nota = nota.Value;
        }
        await _db.SaveChangesAsync(ct);

        var items = await _db.Calificaciones.AsNoTracking()
            .Where(c => c.EstudianteId == estudianteId && c.Evaluacion.CursoId == evaluacion.CursoId)
            .Select(c => new { c.Nota, c.Evaluacion.Ponderacion })
            .ToListAsync(ct);
        var promedio = CalculadoraNotas.PromedioPonderado(items.Select(i => (i.Nota, i.Ponderacion)));

        var grupo = await _db.Calificaciones.AsNoTracking()
            .Where(c => c.EvaluacionId == evaluacionId)
            .Select(c => (decimal?)c.Nota)
            .AverageAsync(ct);

        return Resultado<NotaGuardadaDto>.Ok(new NotaGuardadaDto(promedio, grupo.HasValue ? Math.Round(grupo.Value, 1) : null));
    }
}
