using HomeClass.Application.Dtos;

namespace HomeClass.Web.Models;

/// <summary>Un dato de la franja de indicadores. Tono: bien, medio, mal o neutro.</summary>
public record StatVm(string Titulo, string Valor, string Icono, string Tono = "neutro", string? Detalle = null);

public record CursoCabeceraVm(int Id, string Codigo, string Nombre, string Tab);

public record PerfilPageVm(PerfilDto Perfil, PerfilForm Datos, CambiarPasswordForm Password);

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool MostrarRequestId => !string.IsNullOrEmpty(RequestId);
}
