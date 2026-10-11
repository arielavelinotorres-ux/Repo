using HomeClass.Application.Abstractions;
using HomeClass.Application.Common;
using HomeClass.Application.Dtos;
using HomeClass.Application.Services.Internal;
using HomeClass.Domain.Entities;
using HomeClass.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HomeClass.Application.Services;

public interface IAsistenciaService
{
    Task<SesionAsistenciaDto?> ObtenerSesionAsync(int cursoId, DateTime fecha, int? docenteId = null, CancellationToken ct = default);
    Task<Resultado> GuardarSesionAsync(int cursoId, DateTime fecha, IReadOnlyDictionary<int, EstadoAsistencia> estados, int? docenteId = null, CancellationToken ct = default);
}

public class AsistenciaService : IAsistenciaService
{
    private readonly IApplicationDbContext _db;
    public AsistenciaService(IApplicationDbContext db) => _db = db;

    public async Task<SesionAsistenciaDto?> ObtenerSesionAsync(int cursoId, DateTime fecha, int? docenteId = null, CancellationToken ct = default)
    {
        var snap = await SnapshotRendimiento.CargarAsync(_db, docenteId: docenteId, cursoId: cursoId, ct: ct);
        var curso = snap.Curso(cursoId);
        if (curso is null) return null;

        var dia = fecha.Date;
        var delDia = snap.Asistencias.Where(a => a.Fecha.Date == dia).ToDictionary(a => a.EstudianteId, a => a.Estado);

        var alumnos = snap.EstudiantesDe(cursoId).Select(e => new AlumnoAsistenciaDto(
            e.Id, e.Nombre, e.Carnet,
            delDia.TryGetValue(e.Id, out var estado) ? estado : null,
            snap.AsistenciaPct(e.Id, cursoId))).ToList();

        var ultimas = snap.Asistencias.Select(a => a.Fecha.Date).Distinct().OrderByDescending(d => d).Take(8).ToList();

        return new SesionAsistenciaDto(curso.Id, curso.Codigo, curso.Nombre, dia, delDia.Count > 0, alumnos, ultimas);
    }

    public async Task<Resultado> GuardarSesionAsync(int cursoId, DateTime fecha, IReadOnlyDictionary<int, EstadoAsistencia> estados, int? docenteId = null, CancellationToken ct = default)
    {
        var dia = fecha.Date;
        if (dia > DateTime.Today) return Resultado.Falla("No se puede registrar asistencia de una fecha futura.");

        var cursoOk = await _db.Cursos.AnyAsync(c => c.Id == cursoId && (docenteId == null || c.DocenteId == docenteId), ct);
        if (!cursoOk) return Resultado.Falla("El curso no existe o no tienes permiso para modificarlo.");

        var matriculados = await _db.Inscripciones.Where(i => i.CursoId == cursoId).Select(i => i.EstudianteId).ToListAsync(ct);
        var existentes = await _db.Asistencias.Where(a => a.CursoId == cursoId && a.Fecha == dia).ToListAsync(ct);

        foreach (var estudianteId in matriculados)
        {
            if (!estados.TryGetValue(estudianteId, out var estado)) continue;

            var registro = existentes.FirstOrDefault(a => a.EstudianteId == estudianteId);
            if (registro is null)
                _db.Asistencias.Add(new Asistencia { CursoId = cursoId, EstudianteId = estudianteId, Fecha = dia, Estado = estado });
            else
                registro.Estado = estado;
        }

        await _db.SaveChangesAsync(ct);
        return Resultado.Ok();
    }
}
