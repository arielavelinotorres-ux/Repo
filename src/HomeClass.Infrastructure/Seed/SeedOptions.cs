namespace HomeClass.Infrastructure.Seed;

/// <summary>Valores iniciales. Se leen de la sección "Seed" de appsettings, nunca van fijos en el código.</summary>
public class SeedOptions
{
    public string AdminEmail { get; set; } = "admin@homeclass.edu";
    public string AdminNombre { get; set; } = "Administración HomeClass";
    public string AdminPassword { get; set; } = "Admin#2026";

    public string DocentePassword { get; set; } = "Docente#2026";
    public string EstudiantePassword { get; set; } = "Estudiante#2026";

    /// <summary>Crea docentes, estudiantes, cursos, notas y asistencias de ejemplo si la base está vacía.</summary>
    public bool CargarDatosDemo { get; set; } = true;
}
