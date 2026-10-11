using HomeClass.Application.Abstractions;
using HomeClass.Application.Common;
using HomeClass.Application.Dtos;
using HomeClass.Application.Services.Internal;
using HomeClass.Domain.Entities;
using HomeClass.Domain.Enums;
using HomeClass.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace HomeClass.Application.Services;

public interface ICursoService
{
    /// <param name="docenteId">Si se indica, solo devuelve los cursos de ese docente.</param>
    Task<IReadOnlyList<CursoResumenDto>> ListarAsync(int? docenteId = null, CancellationToken ct = default);
    Task<CursoDetalleDto?> ObtenerDetalleAsync(int cursoId, int? docenteId = null, CancellationToken ct = default);
    Task<CursoEdicionDto?> ObtenerParaEditarAsync(int cursoId, int? docenteId = null, CancellationToken ct = default);
    Task<Resultado<int>> GuardarAsync(GuardarCursoCommand cmd, int? docenteIdRestringido = null, CancellationToken ct = default);
    Task<Resultado> EliminarAsync(int cursoId, int? docenteId = null, CancellationToken ct = default);
    Task<MatriculaDto?> ObtenerMatriculaAsync(int cursoId, int? docenteId = null, CancellationToken ct = default);
    Task<Resultado> MatricularAsync(int cursoId, IReadOnlyCollection<int> estudianteIds, int? docenteId = null, CancellationToken ct = default);
    Task<IReadOnlyList<OpcionDto>> ListarDocentesAsync(CancellationToken ct = default);
}

public class CursoService : ICursoService
{
    private readonly IApplicationDbContext _db;
    public CursoService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<CursoResumenDto>> ListarAsync(int? docenteId = null, CancellationToken ct = default)
    {
        var snap = await SnapshotRendimiento.CargarAsync(_db, docenteId: docenteId, ct: ct);
        return snap.ResumenCursos();
    }

    public async Task<CursoDetalleDto?> ObtenerDetalleAsync(int cursoId, int? docenteId = null, CancellationToken ct = default)
    {
        var snap = await SnapshotRendimiento.CargarAsync(_db, docenteId: docenteId, cursoId: cursoId, ct: ct);
        var curso = snap.Curso(cursoId);
        if (curso is null) return null;

        var alumnos = snap.EstudiantesDe(cursoId).Select(e =>
        {
            var prom = snap.Promedio(e.Id, cursoId);
            var asis = snap.AsistenciaPct(e.Id, cursoId);
            return new AlumnoCursoDto(e.Id, e.Nombre, e.Carnet, prom, asis, EvaluadorRiesgo.Evaluar(prom, asis));
        }).ToList();

        var sesiones = snap.Asistencias.Select(a => a.Fecha.Date).Distinct().Count();

        return new CursoDetalleDto(curso.Id, curso.Codigo, curso.Nombre, curso.Descripcion, curso.Activo,
            curso.DocenteId, curso.Docente, snap.PromedioCurso(cursoId), snap.AsistenciaCurso(cursoId),
            snap.Evaluaciones.Count, sesiones, alumnos, DistribucionRiesgoDto.De(alumnos.Select(a => a.Nivel)));
    }

    public async Task<CursoEdicionDto?> ObtenerParaEditarAsync(int cursoId, int? docenteId = null, CancellationToken ct = default)
    {
        return await _db.Cursos.AsNoTracking()
            .Where(c => c.Id == cursoId && (docenteId == null || c.DocenteId == docenteId))
            .Select(c => new CursoEdicionDto(c.Id, c.Codigo, c.Nombre, c.Descripcion, c.DocenteId, c.Activo))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Resultado<int>> GuardarAsync(GuardarCursoCommand cmd, int? docenteIdRestringido = null, CancellationToken ct = default)
    {
        var codigo = cmd.Codigo.Trim().ToUpperInvariant();
        var nombre = cmd.Nombre.Trim();
        if (codigo.Length == 0 || nombre.Length == 0) return Resultado<int>.Falla("El código y el nombre son obligatorios.");

        // Un docente solo puede crear o editar cursos a su propio nombre.
        var docenteId = docenteIdRestringido ?? cmd.DocenteId;
        if (docenteId is null) return Resultado<int>.Falla("Selecciona el docente responsable del curso.");
        if (!await _db.Docentes.AnyAsync(d => d.Id == docenteId, ct)) return Resultado<int>.Falla("El docente seleccionado no existe.");

        if (await _db.Cursos.AnyAsync(c => c.Codigo == codigo && c.Id != (cmd.Id ?? 0), ct))
            return Resultado<int>.Falla($"Ya existe un curso con el código {codigo}.");

        Curso? curso;
        if (cmd.Id is int id)
        {
            curso = await _db.Cursos.FirstOrDefaultAsync(c => c.Id == id && (docenteIdRestringido == null || c.DocenteId == docenteIdRestringido), ct);
            if (curso is null) return Resultado<int>.Falla("El curso no existe o no tienes permiso para editarlo.");
        }
        else
        {
            curso = new Curso();
            _db.Cursos.Add(curso);
        }

        curso.Codigo = codigo;
        curso.Nombre = nombre;
        curso.Descripcion = string.IsNullOrWhiteSpace(cmd.Descripcion) ? null : cmd.Descripcion.Trim();
        curso.DocenteId = docenteId.Value;
        curso.Activo = cmd.Activo;

        await _db.SaveChangesAsync(ct);
        return Resultado<int>.Ok(curso.Id);
    }

    public async Task<Resultado> EliminarAsync(int cursoId, int? docenteId = null, CancellationToken ct = default)
    {
        var curso = await _db.Cursos.FirstOrDefaultAsync(c => c.Id == cursoId && (docenteId == null || c.DocenteId == docenteId), ct);
        if (curso is null) return Resultado.Falla("El curso no existe o no tienes permiso para eliminarlo.");

        _db.Cursos.Remove(curso); // matrículas, evaluaciones, notas y asistencias se eliminan en cascada
        await _db.SaveChangesAsync(ct);
        return Resultado.Ok();
    }

    public async Task<MatriculaDto?> ObtenerMatriculaAsync(int cursoId, int? docenteId = null, CancellationToken ct = default)
    {
        var curso = await _db.Cursos.AsNoTracking()
            .Where(c => c.Id == cursoId && (docenteId == null || c.DocenteId == docenteId))
            .Select(c => new { c.Id, c.Codigo, c.Nombre })
            .FirstOrDefaultAsync(ct);
        if (curso is null) return null;

        var inscritos = (await _db.Inscripciones.AsNoTracking()
                .Where(i => i.CursoId == cursoId).Select(i => i.EstudianteId).ToListAsync(ct))
            .ToHashSet();

        var estudiantes = await _db.Estudiantes.AsNoTracking()
            .OrderBy(e => e.Usuario.NombreCompleto)
            .Select(e => new { e.Id, e.Usuario.NombreCompleto, e.Carnet, e.Carrera })
            .ToListAsync(ct);

        return new MatriculaDto(curso.Id, curso.Codigo, curso.Nombre,
            estudiantes.Select(e => new OpcionMatriculaDto(e.Id, e.NombreCompleto, e.Carnet, e.Carrera, inscritos.Contains(e.Id))).ToList());
    }

    public async Task<Resultado> MatricularAsync(int cursoId, IReadOnlyCollection<int> estudianteIds, int? docenteId = null, CancellationToken ct = default)
    {
        var existe = await _db.Cursos.AnyAsync(c => c.Id == cursoId && (docenteId == null || c.DocenteId == docenteId), ct);
        if (!existe) return Resultado.Falla("El curso no existe o no tienes permiso para modificarlo.");

        var nuevos = estudianteIds.ToHashSet();
        var actuales = await _db.Inscripciones.Where(i => i.CursoId == cursoId).ToListAsync(ct);
        var idsActuales = actuales.Select(i => i.EstudianteId).ToHashSet();

        var quitar = actuales.Where(i => !nuevos.Contains(i.EstudianteId)).ToList();
        if (quitar.Count > 0)
        {
            var idsQuitar = quitar.Select(i => i.EstudianteId).ToList();

            // Al retirar a un estudiante también se limpian sus notas y asistencias de ESE curso,
            // para que los promedios nunca incluyan datos de alguien que ya no está matriculado.
            var notas = await _db.Calificaciones
                .Where(c => c.Evaluacion.CursoId == cursoId && idsQuitar.Contains(c.EstudianteId)).ToListAsync(ct);
            var asistencias = await _db.Asistencias
                .Where(a => a.CursoId == cursoId && idsQuitar.Contains(a.EstudianteId)).ToListAsync(ct);

            _db.Calificaciones.RemoveRange(notas);
            _db.Asistencias.RemoveRange(asistencias);
            _db.Inscripciones.RemoveRange(quitar);
        }

        var agregar = nuevos.Where(id => !idsActuales.Contains(id)).ToList();
        if (agregar.Count > 0)
        {
            var validos = await _db.Estudiantes.Where(e => agregar.Contains(e.Id)).Select(e => e.Id).ToListAsync(ct);
            _db.Inscripciones.AddRange(validos.Select(id => new Inscripcion { CursoId = cursoId, EstudianteId = id }));
        }

        await _db.SaveChangesAsync(ct);
        return Resultado.Ok();
    }

    public async Task<IReadOnlyList<OpcionDto>> ListarDocentesAsync(CancellationToken ct = default)
    {
        return await _db.Docentes.AsNoTracking()
            .OrderBy(d => d.Usuario.NombreCompleto)
            .Select(d => new OpcionDto(d.Id, d.Usuario.NombreCompleto))
            .ToListAsync(ct);
    }
}
