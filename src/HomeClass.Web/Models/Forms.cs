using System.ComponentModel.DataAnnotations;
using HomeClass.Domain.Enums;

namespace HomeClass.Web.Models;

public class LoginForm
{
    [Required(ErrorMessage = "Escribe tu correo.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [Display(Name = "Correo")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Escribe tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = "";

    public bool Recordarme { get; set; }
    public string? ReturnUrl { get; set; }
}

public class UsuarioForm
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Selecciona un rol.")]
    public string Rol { get; set; } = "";

    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(150)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = "";

    [Required(ErrorMessage = "El correo es obligatorio.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [Display(Name = "Correo")]
    public string Email { get; set; } = "";

    [DataType(DataType.Password)]
    [Display(Name = "Contraseña temporal")]
    public string? Password { get; set; }

    [StringLength(150)]
    public string? Especialidad { get; set; }

    [StringLength(30)]
    public string? Carnet { get; set; }

    [StringLength(120)]
    public string? Carrera { get; set; }

    [Range(1, 12, ErrorMessage = "El semestre debe estar entre 1 y 12.")]
    public int? Semestre { get; set; }

    [StringLength(30)]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [StringLength(200)]
    [Display(Name = "Contacto de emergencia")]
    public string? ContactoEmergencia { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de nacimiento")]
    public DateTime? FechaNacimiento { get; set; }
}

public class CursoForm
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(20)]
    [Display(Name = "Código")]
    public string Codigo { get; set; } = "";

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150)]
    public string Nombre { get; set; } = "";

    [StringLength(500)]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Display(Name = "Docente responsable")]
    public int? DocenteId { get; set; }

    public bool Activo { get; set; } = true;
}

public class EvaluacionForm
{
    public int? Id { get; set; }
    public int CursoId { get; set; }

    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(120)]
    public string Titulo { get; set; } = "";

    public TipoEvaluacion Tipo { get; set; } = TipoEvaluacion.Tarea;

    [Range(1, 100, ErrorMessage = "La ponderación debe estar entre 1 y 100.")]
    public decimal Ponderacion { get; set; } = 10;

    public DateTime Fecha { get; set; } = DateTime.Today;
}

public class PerfilForm
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(150)]
    [Display(Name = "Nombre completo")]
    public string NombreCompleto { get; set; } = "";

    [StringLength(150)]
    public string? Especialidad { get; set; }

    [StringLength(30)]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [StringLength(200)]
    [Display(Name = "Contacto de emergencia")]
    public string? ContactoEmergencia { get; set; }
}

public class CambiarPasswordForm
{
    [Required(ErrorMessage = "Escribe tu contraseña actual.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña actual")]
    public string Actual { get; set; } = "";

    [Required(ErrorMessage = "Escribe la nueva contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    public string Nueva { get; set; } = "";

    [Required(ErrorMessage = "Repite la nueva contraseña.")]
    [Compare(nameof(Nueva), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirmar contraseña")]
    public string Confirmar { get; set; } = "";
}

/// <summary>Cuerpo JSON que envía el libro de calificaciones al guardar una nota.</summary>
public record NotaRequest(int EvaluacionId, int EstudianteId, decimal? Nota);
