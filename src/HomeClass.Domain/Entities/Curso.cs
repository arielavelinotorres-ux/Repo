namespace HomeClass.Domain.Entities;

public class Curso
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;

    public int DocenteId { get; set; }
    public Docente Docente { get; set; } = null!;

    public ICollection<Inscripcion> Inscripciones { get; set; } = new List<Inscripcion>();
    public ICollection<Evaluacion> Evaluaciones { get; set; } = new List<Evaluacion>();
    public ICollection<Asistencia> Asistencias { get; set; } = new List<Asistencia>();
}
