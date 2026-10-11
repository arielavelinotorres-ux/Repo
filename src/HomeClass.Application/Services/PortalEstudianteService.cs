using HomeClass.Application.Abstractions;
using HomeClass.Application.Dtos;
using HomeClass.Application.Services.Internal;
using HomeClass.Domain.Enums;
using HomeClass.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace HomeClass.Application.Services;

public interface IPortalEstudianteService
{
    Task<IReadOnlyList<CursoEstudianteDto>> ListarCursosAsync(string userId, CancellationToken ct = default);
    Task<CursoEstudianteDetalleDto?> ObtenerCursoAsync(string userId, int cursoId, CancellationToken ct = default);
}

/// <summary>Vista de solo lectura: un estudiante únicamente ve sus propios cursos, notas y asistencia.</summary>
public class PortalEstudianteService : IPortalEstudianteService
{
    private readonly IApplicationDbContext _db;
    public PortalEstudianteService(IApplicationDbContext db) => _db = db;

    private Task<int?> EstudianteIdAsync(string userId, CancellationToken ct) =>
        _db.Estudiantes.AsNoTracking().Where(e => e.UserId == userId).Select(e => (int?)e.Id).FirstOrDefaultAsync(ct);

    internal static CursoEstudianteDto ResumenDe(SnapshotRendimiento snap, CursoRow curso, int estudianteId)
    {
        var prom = snap.Promedio(estudianteId, curso.Id);
        var asis = snap.AsistenciaPct(estudianteId, curso.Id);
        return new CursoEstudianteDto(curso.Id, curso.Codigo, curso.Nombre, curso.Docente, prom,
            snap.PromedioCurso(curso.Id), asis, EvaluadorRiesgo.Evaluar(prom, asis),
            snap.NotasDe(estudianteId, curso.Id).Count(),
            snap.Evaluaciones.Count(e => e.CursoId == curso.Id && e.Fecha.Date <= DateTime.Today));
    }

    public async Task<IReadOnlyList<CursoEstudianteDto>> ListarCursosAsync(string userId, CancellationToken ct = default)
    {
        var id = await EstudianteIdAsync(userId, ct);
        if (id is null) return Array.Empty<CursoEstudianteDto>();

        var snap = await SnapshotRendimiento.CargarAsync(_db, estudianteId: id, ct: ct);
        return snap.Cursos.OrderBy(c => c.Codigo).Select(c => ResumenDe(snap, c, id.Value)).ToList();
    }

    public async Task<CursoEstudianteDetalleDto?> ObtenerCursoAsync(string userId, int cursoId, CancellationToken ct = default)
    {
        var id = await EstudianteIdAsync(userId, ct);
        if (id is null) return null;

        var snap = await SnapshotRendimiento.CargarAsync(_db, estudianteId: id, cursoId: cursoId, ct: ct);
        var curso = snap.Curso(cursoId);
        if (curso is null) return null; // no está matriculado en este curso

        var misNotas = snap.NotasDe(id.Value, cursoId).ToDictionary(n => n.EvaluacionId, n => n.Valor);
        var evaluaciones = snap.Evaluaciones.OrderBy(e => e.Fecha)
            .Select(e => new NotaEstudianteDto(e.Id, e.Titulo, e.Tipo, e.Ponderacion, e.Fecha,
                misNotas.TryGetValue(e.Id, out var n) ? n : null, snap.PromedioEvaluacion(e.Id)))
            .ToList();

        var asistencias = snap.AsistenciasDe(id.Value, cursoId)
            .OrderByDescending(a => a.Fecha)
            .Select(a => new AsistenciaRegistroDto(a.Fecha, a.Estado))
            .ToList();

        var resumen = ResumenDe(snap, curso, id.Value);
        return new CursoEstudianteDetalleDto(resumen, curso.Descripcion, evaluaciones, asistencias,
            EvaluadorRiesgo.Sugerir(resumen.MiPromedio, resumen.MiAsistencia));
    }
}
