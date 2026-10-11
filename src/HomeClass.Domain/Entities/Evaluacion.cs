using HomeClass.Domain.Enums;

namespace HomeClass.Domain.Entities;

/// <summary>
/// Una actividad evaluable de un curso (un parcial, una tarea...).
/// Las notas de cada alumno viven en <see cref="Calificacion"/>.
/// </summary>
public class Evaluacion
{
    public int Id { get; set; }
    public int CursoId { get; set; }
    public Curso Curso { get; set; } = null!;

    public string Titulo { get; set; } = string.Empty;
    public TipoEvaluacion Tipo { get; set; } = TipoEvaluacion.Tarea;

    /// <summary>Peso dentro del curso, en porcentaje (0-100].</summary>
    public decimal Ponderacion { get; set; } = 10m;

    public DateTime Fecha { get; set; } = DateTime.Today;

    public ICollection<Calificacion> Calificaciones { get; set; } = new List<Calificacion>();
}
