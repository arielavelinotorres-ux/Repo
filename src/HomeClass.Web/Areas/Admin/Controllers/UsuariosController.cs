using HomeClass.Application.Dtos;
using HomeClass.Application.Services;
using HomeClass.Domain.Constants;
using HomeClass.Web.Controllers;
using HomeClass.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeClass.Web.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Roles = Roles.Admin)]
public class UsuariosController : BaseController
{
    private readonly IUsuarioService _usuarios;
    public UsuariosController(IUsuarioService usuarios) => _usuarios = usuarios;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewBag.YoId = UserId;
        return View(await _usuarios.ListarAsync(ct: ct));
    }

    public IActionResult Crear(string? rol)
    {
        var form = new UsuarioForm { Rol = rol is not null && Roles.Todos.Contains(rol) ? rol : Roles.Estudiante, Semestre = 1 };
        return View("Formulario", form);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(UsuarioForm form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.Password))
            ModelState.AddModelError(nameof(form.Password), "La contraseña temporal es obligatoria.");
        if (!ModelState.IsValid) return View("Formulario", form);

        var res = await _usuarios.CrearAsync(new CrearUsuarioCommand(form.Rol, form.Email, form.NombreCompleto,
            form.Password!, form.Especialidad, form.Carnet, form.Carrera, form.Semestre, form.Telefono,
            form.ContactoEmergencia, form.FechaNacimiento), ct);

        if (!res.Exitoso)
        {
            foreach (var e in res.Errores) ModelState.AddModelError("", e);
            return View("Formulario", form);
        }

        Exito($"La cuenta de {form.NombreCompleto} se creó correctamente.");
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Editar(string id, CancellationToken ct)
    {
        var u = await _usuarios.ObtenerAsync(id, ct);
        if (u is null)
        {
            Fallo("Esa cuenta ya no existe.");
            return RedirectToAction(nameof(Index));
        }

        return View("Formulario", new UsuarioForm
        {
            Id = u.Id, Rol = u.Rol, Email = u.Email, NombreCompleto = u.NombreCompleto,
            Especialidad = u.Especialidad, Carnet = u.Carnet, Carrera = u.Carrera, Semestre = u.Semestre,
            Telefono = u.Telefono, ContactoEmergencia = u.ContactoEmergencia, FechaNacimiento = u.FechaNacimiento
        });
    }

    [HttpPost]
    public async Task<IActionResult> Editar(UsuarioForm form, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(form.Id)) return NotFound();
        if (!ModelState.IsValid) return View("Formulario", form);

        var res = await _usuarios.ActualizarAsync(new ActualizarUsuarioCommand(form.Id, form.Email, form.NombreCompleto,
            form.Especialidad, form.Carnet, form.Carrera, form.Semestre, form.Telefono, form.ContactoEmergencia,
            form.FechaNacimiento), ct);

        if (!res.Exitoso)
        {
            foreach (var e in res.Errores) ModelState.AddModelError("", e);
            return View("Formulario", form);
        }

        Exito($"Los datos de {form.NombreCompleto} se actualizaron.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstado(string id, CancellationToken ct)
    {
        var res = await _usuarios.CambiarEstadoAsync(id, UserId, ct);
        if (res.Exitoso) Exito(res.Valor ? "La cuenta quedó activa." : "La cuenta quedó desactivada.");
        else Fallo(res.PrimerError);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RestablecerPassword(string id, string nuevaPassword, CancellationToken ct)
    {
        var res = await _usuarios.RestablecerPasswordAsync(id, nuevaPassword ?? "", ct);
        if (res.Exitoso) Exito("La contraseña se restableció. Compártela con la persona de forma segura.");
        else Fallo(res.PrimerError);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Eliminar(string id, CancellationToken ct)
    {
        var res = await _usuarios.EliminarAsync(id, UserId, ct);
        if (res.Exitoso) Exito("La cuenta se eliminó.");
        else Fallo(res.PrimerError);
        return RedirectToAction(nameof(Index));
    }
}
