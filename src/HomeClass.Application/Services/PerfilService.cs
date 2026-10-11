using HomeClass.Application.Abstractions;
using HomeClass.Application.Common;
using HomeClass.Application.Dtos;
using HomeClass.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HomeClass.Application.Services;

public interface IPerfilService
{
    Task<int?> ObtenerDocenteIdAsync(string userId, CancellationToken ct = default);
    Task<int?> ObtenerEstudianteIdAsync(string userId, CancellationToken ct = default);
    Task<PerfilDto?> ObtenerAsync(string userId, CancellationToken ct = default);
    Task<Resultado> ActualizarAsync(string userId, ActualizarPerfilCommand cmd, CancellationToken ct = default);
}

public class PerfilService : IPerfilService
{
    private readonly IApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public PerfilService(IApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public Task<int?> ObtenerDocenteIdAsync(string userId, CancellationToken ct = default) =>
        _db.Docentes.AsNoTracking().Where(d => d.UserId == userId).Select(d => (int?)d.Id).FirstOrDefaultAsync(ct);

    public Task<int?> ObtenerEstudianteIdAsync(string userId, CancellationToken ct = default) =>
        _db.Estudiantes.AsNoTracking().Where(e => e.UserId == userId).Select(e => (int?)e.Id).FirstOrDefaultAsync(ct);

    public async Task<PerfilDto?> ObtenerAsync(string userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return null;

        var rol = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "";
        var docente = await _db.Docentes.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == userId, ct);
        var est = await _db.Estudiantes.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == userId, ct);

        return new PerfilDto(rol, user.NombreCompleto, user.Email ?? "", user.FechaCreacion,
            docente?.Especialidad, est?.Carnet, est?.Carrera, est?.Semestre, est?.Telefono,
            est?.ContactoEmergencia, est?.FechaNacimiento);
    }

    public async Task<Resultado> ActualizarAsync(string userId, ActualizarPerfilCommand cmd, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return Resultado.Falla("La cuenta ya no existe.");

        var nombre = cmd.NombreCompleto.Trim();
        if (nombre.Length == 0) return Resultado.Falla("El nombre es obligatorio.");
        user.NombreCompleto = nombre;

        var res = await _userManager.UpdateAsync(user);
        if (!res.Succeeded) return Resultado.Falla(res.Errors.Select(e => e.Description));

        // Cada rol solo puede cambiar sus propios datos de contacto; carnet, carrera y semestre los gestiona el administrador.
        var docente = await _db.Docentes.FirstOrDefaultAsync(d => d.UserId == userId, ct);
        if (docente is not null) docente.Especialidad = cmd.Especialidad?.Trim() ?? "";

        var est = await _db.Estudiantes.FirstOrDefaultAsync(e => e.UserId == userId, ct);
        if (est is not null)
        {
            est.Telefono = cmd.Telefono?.Trim();
            est.ContactoEmergencia = cmd.ContactoEmergencia?.Trim();
        }

        await _db.SaveChangesAsync(ct);
        return Resultado.Ok();
    }
}
