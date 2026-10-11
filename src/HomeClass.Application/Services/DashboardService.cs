using HomeClass.Application.Abstractions;
using HomeClass.Application.Dtos;
using HomeClass.Application.Services.Internal;
using HomeClass.Domain.Enums;
using HomeClass.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace HomeClass.Application.Services;

public interface IDashboardService
{
    Task<AdminDashboardDto> ObtenerAdminAsync(CancellationToken ct = default);
    Task<DocenteDashboardDto?> ObtenerDocenteAsync(string userId, CancellationToken ct = default);
    Task<EstudianteDashboardDto?> ObtenerEstudianteAsync(string userId, CancellationToken ct = default);
}

/// <summary>Arma el "balance académico" que cada rol ve al entrar: el mismo cálculo, con alcance distinto.</summary>
public class DashboardService : IDashboardService
{
    private readonly IApplicationDbContext _db;
    private readonly IUsuarioService _usuarios;

    public DashboardService(IApplicationDbContext db, IUsuarioService usuarios)
    {
        _db = db;
        _usuarios = usuarios;
    }

    public async Task<AdminDashboardDto> ObtenerAdminAsync(CancellationToken ct = default)
    {
        var snap = await SnapshotRendimiento.CargarAsync(_db, ct: ct);
        var alumnos = snap.AlumnosRiesgo();
        var usuarios = await _usuarios.ListarAsync(ct: ct);

        var docentes = await _db.Docentes.AsNoTracking()
            .Select(d => new { d.Id, d.Usuario.NombreCompleto, d.Especialidad })
            .ToListAsync(ct);

        var carga = docentes.Select(d =>
        {
            var cursosDocente = snap.Cursos.Where(c => c.DocenteId == d.Id).Select(c => c.Id).ToList();
            var estudiantes = snap.Inscripciones.Where(i => cursosDocente.Contains(i.CursoId)).Select(i => i.EstudianteId).Distinct().Count();
            var promedios = cursosDocente.Select(snap.PromedioCurso).Where(p => p.HasValue).Select(p => p!.Value).ToList();
            return new DocenteCargaDto(d.NombreCompleto, d.Especialidad, cursosDocente.Count, estudiantes,
                promedios.Count == 0 ? null : Math.Round(promedios.Average(), 1));
        }).OrderByDescending(d => d.Cursos).ThenBy(d => d.Nombre).ToList();

        return new AdminDashboardDto(
            TotalUsuarios: usuarios.Count,
            UsuariosActivos: usuarios.Count(u => u.Activo),
            UsuariosInactivos: usuarios.Count(u => !u.Activo),
            TotalDocentes: docentes.Count,
            TotalEstudiantes: await _db.Estudiantes.CountAsync(ct),
            TotalCursos: snap.Cursos.Count,
            TotalMatriculas: snap.Inscripciones.Count,
            PromedioInstitucional: snap.PromedioGlobal(),
            AsistenciaInstitucional: snap.AsistenciaGlobal(),
            Riesgo: DistribucionRiesgoDto.De(alumnos.Select(a => a.Nivel)),
            DistribucionNotas: snap.DistribucionNotas(),
            Cursos: snap.ResumenCursos(),
            Docentes: carga,
            UltimosUsuarios: usuarios.OrderByDescending(u => u.FechaCreacion).Take(5).ToList(),
            AlumnosCriticos: alumnos.Where(a => a.Nivel == NivelRiesgo.Alto).Take(5).ToList());
    }

    public async Task<DocenteDashboardDto?> ObtenerDocenteAsync(string userId, CancellationToken ct = default)
    {
        var docente = await _db.Docentes.AsNoTracking()
            .Where(d => d.UserId == userId)
            .Select(d => new { d.Id, d.Usuario.NombreCompleto, d.Especialidad })
            .FirstOrDefaultAsync(ct);
        if (docente is null) return null;

        var snap = await SnapshotRendimiento.CargarAsync(_db, docenteId: docente.Id, ct: ct);
        var alumnos = snap.AlumnosRiesgo();

        return new DocenteDashboardDto(
            Nombre: docente.NombreCompleto,
            Especialidad: docente.Especialidad,
            TotalCursos: snap.Cursos.Count,
            TotalEstudiantes: snap.Estudiantes.Count,
            TotalEvaluaciones: snap.Evaluaciones.Count,
            PromedioGeneral: snap.PromedioGlobal(),
            AsistenciaGeneral: snap.AsistenciaGlobal(),
            Riesgo: DistribucionRiesgoDto.De(alumnos.Select(a => a.Nivel)),
            DistribucionNotas: snap.DistribucionNotas(),
            Cursos: snap.ResumenCursos(),
            AlumnosAtencion: alumnos.Where(a => a.Nivel is NivelRiesgo.Alto or NivelRiesgo.Moderado).Take(6).ToList(),
            Proximas: snap.Proximas(5),
            TendenciaEvaluaciones: snap.TendenciaEvaluaciones(8));
    }

    public async Task<EstudianteDashboardDto?> ObtenerEstudianteAsync(string userId, CancellationToken ct = default)
    {
        var est = await _db.Estudiantes.AsNoTracking()
            .Where(e => e.UserId == userId)
            .Select(e => new { e.Id, e.Usuario.NombreCompleto, e.Carnet, e.Carrera, e.Semestre })
            .FirstOrDefaultAsync(ct);
        if (est is null) return null;

        var snap = await SnapshotRendimiento.CargarAsync(_db, estudianteId: est.Id, ct: ct);
        var promedio = snap.Promedio(est.Id);
        var asistencia = snap.AsistenciaPct(est.Id);

        var misNotas = snap.Notas.Where(n => n.EstudianteId == est.Id).OrderBy(n => n.Fecha).ToList();
        var evalPorId = snap.Evaluaciones.ToDictionary(e => e.Id);
        var codigoPorCurso = snap.Cursos.ToDictionary(c => c.Id, c => c.Codigo);

        var ultimasNotas = misNotas.OrderByDescending(n => n.Fecha).Take(6)
            .Select(n => new NotaRecienteDto(codigoPorCurso[n.CursoId], evalPorId[n.EvaluacionId].Titulo, n.Fecha,
                n.Valor, snap.PromedioEvaluacion(n.EvaluacionId)))
            .ToList();

        var recientes = misNotas.TakeLast(10).ToList();
        var evolucion = new ComparativaDto(
            recientes.Select(n => $"{codigoPorCurso[n.CursoId]} {evalPorId[n.EvaluacionId].Titulo}").ToList(),
            recientes.Select(n => (decimal?)n.Valor).ToList(),
            recientes.Select(n => snap.PromedioEvaluacion(n.EvaluacionId)).ToList());

        return new EstudianteDashboardDto(
            Nombre: est.NombreCompleto, Carnet: est.Carnet, Carrera: est.Carrera, Semestre: est.Semestre,
            Promedio: promedio, Asistencia: asistencia, Nivel: EvaluadorRiesgo.Evaluar(promedio, asistencia),
            CursosActivos: snap.Cursos.Count(c => c.Activo), EvaluacionesCalificadas: misNotas.Count,
            Cursos: snap.Cursos.OrderBy(c => c.Codigo).Select(c => PortalEstudianteService.ResumenDe(snap, c, est.Id)).ToList(),
            Proximas: snap.Proximas(5),
            Evolucion: evolucion,
            UltimasNotas: ultimasNotas,
            Sugerencias: EvaluadorRiesgo.Sugerir(promedio, asistencia));
    }
}
