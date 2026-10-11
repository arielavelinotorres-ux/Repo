using HomeClass.Application.Abstractions;
using HomeClass.Application.Dtos;
using HomeClass.Domain.Enums;
using HomeClass.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace HomeClass.Application.Services.Internal;

internal sealed record CursoRow(int Id, string Codigo, string Nombre, string? Descripcion, bool Activo, int DocenteId, string Docente);
internal sealed record EvalRow(int Id, int CursoId, string Titulo, TipoEvaluacion Tipo, decimal Ponderacion, DateTime Fecha);
internal sealed record InscRow(int CursoId, int EstudianteId);
internal sealed record NotaRow(int EvaluacionId, int CursoId, int EstudianteId, decimal Valor, decimal Ponderacion, DateTime Fecha);
internal sealed record AsistRow(int CursoId, int EstudianteId, DateTime Fecha, EstadoAsistencia Estado);
internal sealed record EstRow(int Id, string Nombre, string Carnet, string Carrera);

/// <summary>
/// Foto en memoria de cursos, evaluaciones, matrículas, notas y asistencias de un alcance
/// (todo el sistema, un docente, un estudiante o un curso). Todas las pantallas calculan
/// promedios y riesgo desde aquí, así las reglas viven en un único lugar.
/// Para el tamaño de un colegio o universidad pequeña es suficiente; si crece mucho,
/// estas agregaciones se pueden mover a consultas SQL sin cambiar a los consumidores.
/// </summary>
internal sealed class SnapshotRendimiento
{
    public IReadOnlyList<CursoRow> Cursos { get; }
    public IReadOnlyList<EvalRow> Evaluaciones { get; }
    public IReadOnlyList<InscRow> Inscripciones { get; }
    public IReadOnlyList<NotaRow> Notas { get; }
    public IReadOnlyList<AsistRow> Asistencias { get; }
    public IReadOnlyList<EstRow> Estudiantes { get; }

    private readonly ILookup<int, NotaRow> _notasPorEst;
    private readonly ILookup<int, AsistRow> _asistPorEst;
    private readonly ILookup<int, AsistRow> _asistPorCurso;
    private readonly ILookup<int, int> _cursosPorEst;
    private readonly ILookup<int, int> _estPorCurso;
    private readonly Dictionary<int, EstRow> _estPorId;
    private readonly Dictionary<int, CursoRow> _cursoPorId;

    private SnapshotRendimiento(List<CursoRow> cursos, List<EvalRow> evals, List<InscRow> insc,
        List<NotaRow> notas, List<AsistRow> asist, List<EstRow> estudiantes)
    {
        Cursos = cursos; Evaluaciones = evals; Inscripciones = insc;
        Notas = notas; Asistencias = asist; Estudiantes = estudiantes;

        _notasPorEst = notas.ToLookup(n => n.EstudianteId);
        _asistPorEst = asist.ToLookup(a => a.EstudianteId);
        _asistPorCurso = asist.ToLookup(a => a.CursoId);
        _cursosPorEst = insc.ToLookup(i => i.EstudianteId, i => i.CursoId);
        _estPorCurso = insc.ToLookup(i => i.CursoId, i => i.EstudianteId);
        _estPorId = estudiantes.ToDictionary(e => e.Id);
        _cursoPorId = cursos.ToDictionary(c => c.Id);
    }

    /// <summary>Carga el alcance pedido. Los filtros son opcionales y se combinan.</summary>
    public static async Task<SnapshotRendimiento> CargarAsync(IApplicationDbContext db,
        int? docenteId = null, int? estudianteId = null, int? cursoId = null, CancellationToken ct = default)
    {
        var consultaCursos = db.Cursos.AsNoTracking()
            .Where(c => (docenteId == null || c.DocenteId == docenteId)
                     && (cursoId == null || c.Id == cursoId)
                     && (estudianteId == null || c.Inscripciones.Any(i => i.EstudianteId == estudianteId)));

        var cursos = await consultaCursos
            .Select(c => new CursoRow(c.Id, c.Codigo, c.Nombre, c.Descripcion, c.Activo, c.DocenteId, c.Docente.Usuario.NombreCompleto))
            .ToListAsync(ct);
        var cursoIds = cursos.Select(c => c.Id).ToList();

        var evals = await db.Evaluaciones.AsNoTracking()
            .Where(e => cursoIds.Contains(e.CursoId))
            .Select(e => new EvalRow(e.Id, e.CursoId, e.Titulo, e.Tipo, e.Ponderacion, e.Fecha))
            .ToListAsync(ct);

        var insc = await db.Inscripciones.AsNoTracking()
            .Where(i => cursoIds.Contains(i.CursoId))
            .Select(i => new InscRow(i.CursoId, i.EstudianteId))
            .ToListAsync(ct);

        var notasCrudas = await db.Calificaciones.AsNoTracking()
            .Where(c => cursoIds.Contains(c.Evaluacion.CursoId))
            .Select(c => new { c.EvaluacionId, c.EstudianteId, c.Nota })
            .ToListAsync(ct);
        var evalPorId = evals.ToDictionary(e => e.Id);
        var notas = notasCrudas
            .Where(n => evalPorId.ContainsKey(n.EvaluacionId))
            .Select(n =>
            {
                var ev = evalPorId[n.EvaluacionId];
                return new NotaRow(n.EvaluacionId, ev.CursoId, n.EstudianteId, n.Nota, ev.Ponderacion, ev.Fecha);
            })
            .ToList();

        var asist = await db.Asistencias.AsNoTracking()
            .Where(a => cursoIds.Contains(a.CursoId))
            .Select(a => new AsistRow(a.CursoId, a.EstudianteId, a.Fecha, a.Estado))
            .ToListAsync(ct);

        var estIds = insc.Select(i => i.EstudianteId).Distinct().ToList();
        var estudiantes = await db.Estudiantes.AsNoTracking()
            .Where(e => estIds.Contains(e.Id))
            .Select(e => new EstRow(e.Id, e.Usuario.NombreCompleto, e.Carnet, e.Carrera))
            .ToListAsync(ct);

        return new SnapshotRendimiento(cursos, evals, insc, notas, asist, estudiantes);
    }

    // ---------- Cálculos por estudiante ----------
    public decimal? Promedio(int estudianteId, int? cursoId = null)
    {
        if (cursoId is int idCurso)
        {
            return CalculadoraNotas.PromedioPonderado(
                _notasPorEst[estudianteId].Where(n => n.CursoId == idCurso).Select(n => (n.Valor, n.Ponderacion)));
        }

        var porCurso = _cursosPorEst[estudianteId]
            .Select(cid => Promedio(estudianteId, cid))
            .Where(p => p.HasValue)
            .Select(p => p!.Value)
            .ToList();
        return porCurso.Count == 0 ? null : Math.Round(porCurso.Average(), 1);
    }

    public decimal? AsistenciaPct(int estudianteId, int? cursoId = null)
    {
        IEnumerable<AsistRow> filas = _asistPorEst[estudianteId];
        if (cursoId is int idCurso) filas = filas.Where(a => a.CursoId == idCurso);
        return CalculadoraNotas.PorcentajeAsistencia(filas.Select(a => a.Estado));
    }

    public IEnumerable<NotaRow> NotasDe(int estudianteId, int cursoId) =>
        _notasPorEst[estudianteId].Where(n => n.CursoId == cursoId);

    public IEnumerable<AsistRow> AsistenciasDe(int estudianteId, int cursoId) =>
        _asistPorEst[estudianteId].Where(a => a.CursoId == cursoId);

    // ---------- Cálculos por curso / globales ----------
    public decimal? PromedioCurso(int cursoId)
    {
        var ps = _estPorCurso[cursoId]
            .Select(e => Promedio(e, cursoId))
            .Where(p => p.HasValue)
            .Select(p => p!.Value)
            .ToList();
        return ps.Count == 0 ? null : Math.Round(ps.Average(), 1);
    }

    public decimal? AsistenciaCurso(int cursoId) =>
        CalculadoraNotas.PorcentajeAsistencia(_asistPorCurso[cursoId].Select(a => a.Estado));

    public decimal? PromedioGlobal()
    {
        var ps = _estPorId.Keys.Select(e => Promedio(e)).Where(p => p.HasValue).Select(p => p!.Value).ToList();
        return ps.Count == 0 ? null : Math.Round(ps.Average(), 1);
    }

    public decimal? AsistenciaGlobal() =>
        CalculadoraNotas.PorcentajeAsistencia(Asistencias.Select(a => a.Estado));

    public decimal? PromedioEvaluacion(int evaluacionId)
    {
        var valores = Notas.Where(n => n.EvaluacionId == evaluacionId).Select(n => n.Valor).ToList();
        return valores.Count == 0 ? null : Math.Round(valores.Average(), 1);
    }

    public IEnumerable<EstRow> EstudiantesDe(int cursoId) =>
        _estPorCurso[cursoId].Select(id => _estPorId[id]).OrderBy(e => e.Nombre);

    public CursoRow? Curso(int cursoId) => _cursoPorId.GetValueOrDefault(cursoId);

    // ---------- Piezas listas para las vistas ----------
    public List<CursoResumenDto> ResumenCursos() =>
        Cursos.OrderBy(c => c.Codigo)
            .Select(c => new CursoResumenDto(c.Id, c.Codigo, c.Nombre, c.Docente,
                _estPorCurso[c.Id].Count(), Evaluaciones.Count(e => e.CursoId == c.Id),
                PromedioCurso(c.Id), AsistenciaCurso(c.Id), c.Activo))
            .ToList();

    public List<AlumnoRiesgoDto> AlumnosRiesgo()
    {
        return Estudiantes.Select(e =>
            {
                var prom = Promedio(e.Id);
                var asis = AsistenciaPct(e.Id);
                var cursos = string.Join(", ", _cursosPorEst[e.Id].Select(id => _cursoPorId[id].Codigo).OrderBy(x => x));
                return new AlumnoRiesgoDto(e.Id, e.Nombre, e.Carnet, e.Carrera, cursos, prom, asis,
                    EvaluadorRiesgo.Evaluar(prom, asis), EvaluadorRiesgo.Sugerir(prom, asis));
            })
            .OrderBy(a => a.Nivel)
            .ThenBy(a => a.Promedio ?? 101m)
            .ThenBy(a => a.Nombre)
            .ToList();
    }

    /// <summary>Cuántos estudiantes caen en cada rango de promedio: &lt;60, 60-69, 70-79, 80-89, 90+.</summary>
    public int[] DistribucionNotas()
    {
        var r = new int[5];
        foreach (var id in _estPorId.Keys)
        {
            var p = Promedio(id);
            if (p is null) continue;
            r[p < 60 ? 0 : p < 70 ? 1 : p < 80 ? 2 : p < 90 ? 3 : 4]++;
        }
        return r;
    }

    public List<EvaluacionProximaDto> Proximas(int maximo)
    {
        var hoy = DateTime.Today;
        return Evaluaciones.Where(e => e.Fecha.Date >= hoy)
            .OrderBy(e => e.Fecha)
            .Take(maximo)
            .Select(e => new EvaluacionProximaDto(e.CursoId, _cursoPorId[e.CursoId].Codigo, e.Titulo, e.Tipo, e.Ponderacion, e.Fecha))
            .ToList();
    }

    /// <summary>Promedio de la clase en las últimas evaluaciones calificadas, de la más antigua a la más reciente.</summary>
    public SerieDto TendenciaEvaluaciones(int maximo)
    {
        var puntos = Evaluaciones
            .Select(e => new { Eval = e, Prom = PromedioEvaluacion(e.Id) })
            .Where(x => x.Prom.HasValue)
            .OrderByDescending(x => x.Eval.Fecha)
            .Take(maximo)
            .OrderBy(x => x.Eval.Fecha)
            .ToList();

        return new SerieDto(
            puntos.Select(x => $"{_cursoPorId[x.Eval.CursoId].Codigo} {x.Eval.Titulo}").ToList(),
            puntos.Select(x => x.Prom).ToList());
    }
}
