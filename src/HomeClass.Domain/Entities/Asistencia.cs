using HomeClass.Domain.Enums;

namespace HomeClass.Domain.Entities;

/// <summary>Registro de asistencia de un estudiante en una sesión (día) de un curso.</summary>
public class Asistencia
{
    public int Id { get; set; }

    public int CursoId { get; set; }
    public Curso Curso { get; set; } = null!;

    public int EstudianteId { get; set; }
    public Estudiante Estudiante { get; set; } = null!;

    public DateTime Fecha { get; set; } = DateTime.Today;
    public EstadoAsistencia Estado { get; set; } = EstadoAsistencia.Presente;
}
