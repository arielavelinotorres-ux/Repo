using HomeClass.Domain.Enums;
using HomeClass.Domain.Rules;

namespace HomeClass.Application.Dtos;

// ---------- Piezas comunes ----------
public record OpcionDto(int Id, string Texto);
public record SerieDto(IReadOnlyList<string> Etiquetas, IReadOnlyList<decimal?> Valores);
public record ComparativaDto(IReadOnlyList<string> Etiquetas, IReadOnlyList<decimal?> Mias, IReadOnlyList<decimal?> Grupo);

public record DistribucionRiesgoDto(int Alto, int Moderado, int Bajo, int SinDatos)
{
    public int Total => Alto + Moderado + Bajo + SinDatos;

    public static DistribucionRiesgoDto De(IEnumerable<NivelRiesgo> niveles)
    {
        var lista = niveles.ToList();
        return new DistribucionRiesgoDto(
            lista.Count(n => n == NivelRiesgo.Alto),
            lista.Count(n => n == NivelRiesgo.Moderado),
            lista.Count(n => n == NivelRiesgo.Bajo),
            lista.Count(n => n == NivelRiesgo.SinDatos));
    }
}

public record CursoResumenDto(int Id, string Codigo, string Nombre, string Docente, int Estudiantes,
    int Evaluaciones, decimal? Promedio, decimal? Asistencia, bool Activo);

public record AlumnoRiesgoDto(int EstudianteId, string Nombre, string Carnet, string Carrera, string Cursos,
    decimal? Promedio, decimal? Asistencia, NivelRiesgo Nivel, IReadOnlyList<Sugerencia> Sugerencias);

public record ReporteRiesgoDto(IReadOnlyList<AlumnoRiesgoDto> Alumnos, DistribucionRiesgoDto Distribucion);

public record EvaluacionProximaDto(int CursoId, string Curso, string Titulo, TipoEvaluacion Tipo,
    decimal Ponderacion, DateTime Fecha);

// ---------- Dashboards ----------
public record DocenteCargaDto(string Nombre, string Especialidad, int Cursos, int Estudiantes, decimal? Promedio);

public record AdminDashboardDto(
    int TotalUsuarios, int UsuariosActivos, int UsuariosInactivos,
    int TotalDocentes, int TotalEstudiantes, int TotalCursos, int TotalMatriculas,
    decimal? PromedioInstitucional, decimal? AsistenciaInstitucional,
    DistribucionRiesgoDto Riesgo, IReadOnlyList<int> DistribucionNotas,
    IReadOnlyList<CursoResumenDto> Cursos, IReadOnlyList<DocenteCargaDto> Docentes,
    IReadOnlyList<UsuarioItemDto> UltimosUsuarios, IReadOnlyList<AlumnoRiesgoDto> AlumnosCriticos);

public record DocenteDashboardDto(
    string Nombre, string Especialidad,
    int TotalCursos, int TotalEstudiantes, int TotalEvaluaciones,
    decimal? PromedioGeneral, decimal? AsistenciaGeneral,
    DistribucionRiesgoDto Riesgo, IReadOnlyList<int> DistribucionNotas,
    IReadOnlyList<CursoResumenDto> Cursos, IReadOnlyList<AlumnoRiesgoDto> AlumnosAtencion,
    IReadOnlyList<EvaluacionProximaDto> Proximas, SerieDto TendenciaEvaluaciones);

public record NotaRecienteDto(string Curso, string Titulo, DateTime Fecha, decimal Nota, decimal? PromedioGrupo);

public record EstudianteDashboardDto(
    string Nombre, string Carnet, string Carrera, int Semestre,
    decimal? Promedio, decimal? Asistencia, NivelRiesgo Nivel,
    int CursosActivos, int EvaluacionesCalificadas,
    IReadOnlyList<CursoEstudianteDto> Cursos, IReadOnlyList<EvaluacionProximaDto> Proximas,
    ComparativaDto Evolucion, IReadOnlyList<NotaRecienteDto> UltimasNotas,
    IReadOnlyList<Sugerencia> Sugerencias);

// ---------- Cursos (docente / admin) ----------
public record AlumnoCursoDto(int EstudianteId, string Nombre, string Carnet, decimal? Promedio,
    decimal? Asistencia, NivelRiesgo Nivel);

public record CursoDetalleDto(int Id, string Codigo, string Nombre, string? Descripcion, bool Activo,
    int DocenteId, string Docente, decimal? Promedio, decimal? Asistencia,
    int TotalEvaluaciones, int Sesiones, IReadOnlyList<AlumnoCursoDto> Alumnos, DistribucionRiesgoDto Riesgo);

public record CursoEdicionDto(int Id, string Codigo, string Nombre, string? Descripcion, int DocenteId, bool Activo);
public record GuardarCursoCommand(int? Id, string Codigo, string Nombre, string? Descripcion, int? DocenteId, bool Activo);

public record OpcionMatriculaDto(int EstudianteId, string Nombre, string Carnet, string Carrera, bool Inscrito);
public record MatriculaDto(int CursoId, string Codigo, string Nombre, IReadOnlyList<OpcionMatriculaDto> Estudiantes);

// ---------- Libro de calificaciones ----------
public record EvaluacionColumnaDto(int Id, string Titulo, TipoEvaluacion Tipo, decimal Ponderacion,
    DateTime Fecha, decimal? PromedioGrupo, int Calificadas);

public record FilaCalificacionDto(int EstudianteId, string Nombre, string Carnet,
    IReadOnlyDictionary<int, decimal> Notas, decimal? Promedio);

public record LibroCalificacionesDto(int CursoId, string Codigo, string Nombre,
    IReadOnlyList<EvaluacionColumnaDto> Evaluaciones, IReadOnlyList<FilaCalificacionDto> Filas,
    decimal SumaPonderacion);

public record GuardarEvaluacionCommand(int? Id, int CursoId, string Titulo, TipoEvaluacion Tipo,
    decimal Ponderacion, DateTime Fecha);

public record NotaGuardadaDto(decimal? PromedioEstudiante, decimal? PromedioGrupoEvaluacion);

// ---------- Asistencia ----------
public record AlumnoAsistenciaDto(int EstudianteId, string Nombre, string Carnet, EstadoAsistencia? Estado,
    decimal? PorcentajeCurso);

public record SesionAsistenciaDto(int CursoId, string Codigo, string Nombre, DateTime Fecha, bool YaRegistrada,
    IReadOnlyList<AlumnoAsistenciaDto> Alumnos, IReadOnlyList<DateTime> UltimasSesiones);

// ---------- Usuarios ----------
public record UsuarioItemDto(string Id, string NombreCompleto, string Email, string Rol, bool Activo,
    DateTime FechaCreacion, DateTime? UltimoAcceso, string? Detalle);

public record UsuarioDetalleDto(string Id, string Rol, string Email, string NombreCompleto, bool Activo,
    string? Especialidad, string? Carnet, string? Carrera, int? Semestre, string? Telefono,
    string? ContactoEmergencia, DateTime? FechaNacimiento);

public record CrearUsuarioCommand(string Rol, string Email, string NombreCompleto, string Password,
    string? Especialidad, string? Carnet, string? Carrera, int? Semestre, string? Telefono,
    string? ContactoEmergencia, DateTime? FechaNacimiento);

public record ActualizarUsuarioCommand(string Id, string Email, string NombreCompleto,
    string? Especialidad, string? Carnet, string? Carrera, int? Semestre, string? Telefono,
    string? ContactoEmergencia, DateTime? FechaNacimiento);

// ---------- Perfil ----------
public record PerfilDto(string Rol, string NombreCompleto, string Email, DateTime FechaCreacion,
    string? Especialidad, string? Carnet, string? Carrera, int? Semestre, string? Telefono,
    string? ContactoEmergencia, DateTime? FechaNacimiento);

public record ActualizarPerfilCommand(string NombreCompleto, string? Especialidad, string? Telefono,
    string? ContactoEmergencia);

// ---------- Portal del estudiante ----------
public record CursoEstudianteDto(int CursoId, string Codigo, string Nombre, string Docente,
    decimal? MiPromedio, decimal? PromedioGrupo, decimal? MiAsistencia, NivelRiesgo Nivel,
    int EvaluacionesCalificadas, int EvaluacionesTotales);

public record NotaEstudianteDto(int EvaluacionId, string Titulo, TipoEvaluacion Tipo, decimal Ponderacion,
    DateTime Fecha, decimal? MiNota, decimal? PromedioGrupo);

public record AsistenciaRegistroDto(DateTime Fecha, EstadoAsistencia Estado);

public record CursoEstudianteDetalleDto(CursoEstudianteDto Resumen, string? Descripcion,
    IReadOnlyList<NotaEstudianteDto> Evaluaciones, IReadOnlyList<AsistenciaRegistroDto> Asistencias,
    IReadOnlyList<Sugerencia> Sugerencias);
