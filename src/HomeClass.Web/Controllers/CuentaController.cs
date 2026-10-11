using HomeClass.Application.Dtos;
using HomeClass.Application.Services;
using HomeClass.Domain.Entities;
using HomeClass.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HomeClass.Web.Controllers;

public class CuentaController : BaseController
{
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IPerfilService _perfil;

    public CuentaController(SignInManager<ApplicationUser> signIn, UserManager<ApplicationUser> users, IPerfilService perfil)
    {
        _signIn = signIn;
        _users = users;
        _perfil = perfil;
    }

    [AllowAnonymous]
    public IActionResult Ingresar(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new LoginForm { ReturnUrl = returnUrl });
    }

    [HttpPost, AllowAnonymous]
    public async Task<IActionResult> Ingresar(LoginForm form)
    {
        if (!ModelState.IsValid) return View(form);

        var user = await _users.FindByEmailAsync(form.Email.Trim());
        if (user is null)
        {
            ModelState.AddModelError("", "Correo o contraseña incorrectos.");
            return View(form);
        }

        var resultado = await _signIn.PasswordSignInAsync(user, form.Password, form.Recordarme, lockoutOnFailure: true);

        if (resultado.IsLockedOut)
        {
            ModelState.AddModelError("", "Demasiados intentos fallidos. Espera unos minutos e inténtalo de nuevo.");
            return View(form);
        }
        if (!resultado.Succeeded)
        {
            ModelState.AddModelError("", "Correo o contraseña incorrectos.");
            return View(form);
        }
        if (!user.Activo)
        {
            await _signIn.SignOutAsync();
            ModelState.AddModelError("", "Tu cuenta está desactivada. Comunícate con la administración.");
            return View(form);
        }

        user.UltimoAcceso = DateTime.UtcNow;
        await _users.UpdateAsync(user);

        if (!string.IsNullOrEmpty(form.ReturnUrl) && Url.IsLocalUrl(form.ReturnUrl)) return LocalRedirect(form.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Salir()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult AccesoDenegado() => View();

    // ---------- Perfil (los tres roles) ----------
    [Authorize]
    public async Task<IActionResult> Perfil()
    {
        var vm = await ConstruirPerfilAsync();
        return vm is null ? RedirectToAction("Index", "Home") : View(vm);
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Perfil(PerfilForm datos)
    {
        if (!ModelState.IsValid)
        {
            var vmInvalido = await ConstruirPerfilAsync(datos);
            return vmInvalido is null ? RedirectToAction("Index", "Home") : View(vmInvalido);
        }

        var resultado = await _perfil.ActualizarAsync(UserId,
            new ActualizarPerfilCommand(datos.NombreCompleto, datos.Especialidad, datos.Telefono, datos.ContactoEmergencia));

        if (!resultado.Exitoso)
        {
            foreach (var error in resultado.Errores) ModelState.AddModelError("", error);
            var vm = await ConstruirPerfilAsync(datos);
            return vm is null ? RedirectToAction("Index", "Home") : View(vm);
        }

        // Renueva la sesión para que el nombre nuevo aparezca en la barra superior.
        var user = await _users.GetUserAsync(User);
        if (user is not null) await _signIn.RefreshSignInAsync(user);

        Exito("Tus datos se guardaron.");
        return RedirectToAction(nameof(Perfil));
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordForm password)
    {
        var user = await _users.GetUserAsync(User);
        if (user is null) return RedirectToAction("Index", "Home");

        if (ModelState.IsValid)
        {
            var res = await _users.ChangePasswordAsync(user, password.Actual, password.Nueva);
            if (res.Succeeded)
            {
                await _signIn.RefreshSignInAsync(user);
                Exito("Tu contraseña se actualizó.");
                return RedirectToAction(nameof(Perfil));
            }
            foreach (var e in res.Errors) ModelState.AddModelError("", e.Description);
        }

        var vm = await ConstruirPerfilAsync(null, new CambiarPasswordForm());
        return vm is null ? RedirectToAction("Index", "Home") : View("Perfil", vm);
    }

    private async Task<PerfilPageVm?> ConstruirPerfilAsync(PerfilForm? datos = null, CambiarPasswordForm? password = null)
    {
        var perfil = await _perfil.ObtenerAsync(UserId);
        if (perfil is null) return null;

        datos ??= new PerfilForm
        {
            NombreCompleto = perfil.NombreCompleto,
            Especialidad = perfil.Especialidad,
            Telefono = perfil.Telefono,
            ContactoEmergencia = perfil.ContactoEmergencia
        };
        return new PerfilPageVm(perfil, datos, password ?? new CambiarPasswordForm());
    }
}
