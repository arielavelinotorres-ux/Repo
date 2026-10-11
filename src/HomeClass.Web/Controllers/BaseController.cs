using System.Security.Claims;
using HomeClass.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HomeClass.Web.Controllers;

public abstract class BaseController : Controller
{
    protected string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    protected void Exito(string mensaje) => TempData["Exito"] = mensaje;
    protected void Fallo(string mensaje) => TempData["Error"] = mensaje;

    protected IActionResult SinPerfil() => View("~/Views/Shared/SinPerfil.cshtml");
}

/// <summary>Base para los controladores del área Docente: resuelve el docente de la sesión.</summary>
public abstract class DocenteBaseController : BaseController
{
    private readonly IPerfilService _perfil;
    protected DocenteBaseController(IPerfilService perfil) => _perfil = perfil;

    protected Task<int?> DocenteIdAsync() => _perfil.ObtenerDocenteIdAsync(UserId);
}
