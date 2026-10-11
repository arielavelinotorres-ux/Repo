namespace HomeClass.Domain.Entities;

/// <summary>Nota (0-100) de un estudiante en una evaluación.</summary>
public class Calificacion
{
    public int Id { get; set; }

    public int EvaluacionId { get; set; }
    public Evaluacion Evaluacion { get; set; } = null!;

    public int EstudianteId { get; set; }
    public Estudiante Estudiante { get; set; } = null!;

    public decimal Nota { get; set; }
}
