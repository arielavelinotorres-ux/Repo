using Microsoft.AspNetCore.Identity;

namespace HomeClass.Domain.Entities;

/// <summary>
/// Cuenta de acceso. Es la ÚNICA fuente del nombre y el correo de cada persona:
/// Docente y Estudiante solo guardan datos propios de su rol y apuntan aquí.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoAcceso { get; set; }
}
