namespace HomeClass.Domain.Entities;

/// <summary>Matrícula de un estudiante en un curso (tabla puente con datos propios).</summary>
public class Inscripcion
{
    public int CursoId { get; set; }
    public Curso Curso { get; set; } = null!;

    public int EstudianteId { get; set; }
    public Estudiante Estudiante { get; set; } = null!;

    public DateTime FechaInscripcion { get; set; } = DateTime.UtcNow;
}
