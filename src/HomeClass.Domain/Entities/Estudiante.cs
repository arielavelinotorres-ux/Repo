namespace HomeClass.Domain.Entities;

public class Estudiante
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser Usuario { get; set; } = null!;

    public string Carnet { get; set; } = string.Empty;
    public string Carrera { get; set; } = string.Empty;
    public int Semestre { get; set; } = 1;
    public string? Telefono { get; set; }
    public string? ContactoEmergencia { get; set; }
    public DateTime? FechaNacimiento { get; set; }

    public ICollection<Inscripcion> Inscripciones { get; set; } = new List<Inscripcion>();
    public ICollection<Calificacion> Calificaciones { get; set; } = new List<Calificacion>();
    public ICollection<Asistencia> Asistencias { get; set; } = new List<Asistencia>();
}
