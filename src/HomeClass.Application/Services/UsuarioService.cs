using HomeClass.Application.Abstractions;
using HomeClass.Application.Common;
using HomeClass.Application.Dtos;
using HomeClass.Domain.Constants;
using HomeClass.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HomeClass.Application.Services;

public interface IUsuarioService
{
    Task<IReadOnlyList<UsuarioItemDto>> ListarAsync(string? rol = null, string? buscar = null, CancellationToken ct = default);
    Task<UsuarioDetalleDto?> ObtenerAsync(string id, CancellationToken ct = default);
    Task<Resultado> CrearAsync(CrearUsuarioCommand cmd, CancellationToken ct = default);
    Task<Resultado> ActualizarAsync(ActualizarUsuarioCommand cmd, CancellationToken ct = default);
    Task<Resultado<bool>> CambiarEstadoAsync(string id, string adminActualId, CancellationToken ct = default);
    Task<Resultado> RestablecerPasswordAsync(string id, string nuevaPassword, CancellationToken ct = default);
    Task<Resultado> EliminarAsync(string id, string adminActualId, CancellationToken ct = default);
}

/// <summary>Administración de cuentas: cada alta crea la cuenta Identity y, según el rol, su ficha de Docente o Estudiante.</summary>
public class UsuarioService : IUsuarioService
{
    private readonly IApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public UsuarioService(IApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<UsuarioItemDto>> ListarAsync(string? rol = null, string? buscar = null, CancellationToken ct = default)
    {
        var usuarios = await _db.Users.AsNoTracking().OrderBy(u => u.NombreCompleto).ToListAsync(ct);

        var roles = await (from ur in _db.UserRoles.AsNoTracking()
                           join r in _db.Roles.AsNoTracking() on ur.RoleId equals r.Id
                           select new { ur.UserId, Rol = r.Name })
            .ToListAsync(ct);
        var rolPorUsuario = roles.GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => g.First().Rol ?? "");

        var docentes = await _db.Docentes.AsNoTracking()
            .Select(d => new { d.UserId, d.Especialidad, Cursos = d.Cursos.Count })
            .ToDictionaryAsync(d => d.UserId, ct);
        var estudiantes = await _db.Estudiantes.AsNoTracking()
            .Select(e => new { e.UserId, e.Carnet, e.Carrera })
            .ToDictionaryAsync(e => e.UserId, ct);

        var lista = usuarios.Select(u =>
        {
            var rolUsuario = rolPorUsuario.GetValueOrDefault(u.Id, "");
            string? detalle = null;
            if (docentes.TryGetValue(u.Id, out var d))
                detalle = string.IsNullOrWhiteSpace(d.Especialidad) ? $"{d.Cursos} curso(s)" : $"{d.Especialidad}, {d.Cursos} curso(s)";
            else if (estudiantes.TryGetValue(u.Id, out var e))
                detalle = $"{e.Carnet}, {e.Carrera}";

            return new UsuarioItemDto(u.Id, u.NombreCompleto, u.Email ?? "", rolUsuario, u.Activo,
                u.FechaCreacion, u.UltimoAcceso, detalle);
        });

        if (!string.IsNullOrWhiteSpace(rol)) lista = lista.Where(u => u.Rol == rol);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var t = buscar.Trim();
            lista = lista.Where(u => u.NombreCompleto.Contains(t, StringComparison.OrdinalIgnoreCase)
                                  || u.Email.Contains(t, StringComparison.OrdinalIgnoreCase));
        }
        return lista.ToList();
    }

    public async Task<UsuarioDetalleDto?> ObtenerAsync(string id, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return null;

        var rol = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "";
        var docente = await _db.Docentes.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == id, ct);
        var est = await _db.Estudiantes.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == id, ct);

        return new UsuarioDetalleDto(user.Id, rol, user.Email ?? "", user.NombreCompleto, user.Activo,
            docente?.Especialidad, est?.Carnet, est?.Carrera, est?.Semestre, est?.Telefono,
            est?.ContactoEmergencia, est?.FechaNacimiento);
    }

    public async Task<Resultado> CrearAsync(CrearUsuarioCommand cmd, CancellationToken ct = default)
    {
        if (!Roles.Todos.Contains(cmd.Rol)) return Resultado.Falla("El rol seleccionado no es válido.");

        var email = cmd.Email.Trim();
        if (await _userManager.FindByEmailAsync(email) is not null)
            return Resultado.Falla("Ya existe una cuenta con ese correo.");

        var carnet = cmd.Carnet?.Trim() ?? "";
        if (cmd.Rol == Roles.Estudiante)
        {
            if (string.IsNullOrWhiteSpace(carnet)) return Resultado.Falla("El carnet es obligatorio para estudiantes.");
            if (await _db.Estudiantes.AnyAsync(e => e.Carnet == carnet, ct))
                return Resultado.Falla("Ya existe un estudiante con ese carnet.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NombreCompleto = cmd.NombreCompleto.Trim(),
            Activo = true
        };

        var crear = await _userManager.CreateAsync(user, cmd.Password);
        if (!crear.Succeeded) return Resultado.Falla(crear.Errors.Select(e => e.Description));

        var asignarRol = await _userManager.AddToRoleAsync(user, cmd.Rol);
        if (!asignarRol.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return Resultado.Falla(asignarRol.Errors.Select(e => e.Description));
        }

        try
        {
            if (cmd.Rol == Roles.Docente)
            {
                _db.Docentes.Add(new Docente { UserId = user.Id, Especialidad = cmd.Especialidad?.Trim() ?? "" });
            }
            else if (cmd.Rol == Roles.Estudiante)
            {
                _db.Estudiantes.Add(new Estudiante
                {
                    UserId = user.Id,
                    Carnet = carnet,
                    Carrera = cmd.Carrera?.Trim() ?? "",
                    Semestre = cmd.Semestre ?? 1,
                    Telefono = cmd.Telefono?.Trim(),
                    ContactoEmergencia = cmd.ContactoEmergencia?.Trim(),
                    FechaNacimiento = cmd.FechaNacimiento
                });
            }
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            // Si la ficha falla, no dejamos una cuenta huérfana.
            await _userManager.DeleteAsync(user);
            throw;
        }

        return Resultado.Ok();
    }

    public async Task<Resultado> ActualizarAsync(ActualizarUsuarioCommand cmd, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(cmd.Id);
        if (user is null) return Resultado.Falla("La cuenta ya no existe.");

        var email = cmd.Email.Trim();
        user.NombreCompleto = cmd.NombreCompleto.Trim();
        user.Email = email;
        user.UserName = email;

        var actualizar = await _userManager.UpdateAsync(user);
        if (!actualizar.Succeeded) return Resultado.Falla(actualizar.Errors.Select(e => e.Description));

        var docente = await _db.Docentes.FirstOrDefaultAsync(d => d.UserId == cmd.Id, ct);
        if (docente is not null) docente.Especialidad = cmd.Especialidad?.Trim() ?? "";

        var est = await _db.Estudiantes.FirstOrDefaultAsync(e => e.UserId == cmd.Id, ct);
        if (est is not null)
        {
            var carnet = cmd.Carnet?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(carnet)) return Resultado.Falla("El carnet es obligatorio para estudiantes.");
            if (await _db.Estudiantes.AnyAsync(e => e.Carnet == carnet && e.Id != est.Id, ct))
                return Resultado.Falla("Ya existe otro estudiante con ese carnet.");

            est.Carnet = carnet;
            est.Carrera = cmd.Carrera?.Trim() ?? "";
            est.Semestre = cmd.Semestre ?? est.Semestre;
            est.Telefono = cmd.Telefono?.Trim();
            est.ContactoEmergencia = cmd.ContactoEmergencia?.Trim();
            est.FechaNacimiento = cmd.FechaNacimiento;
        }

        await _db.SaveChangesAsync(ct);
        return Resultado.Ok();
    }

    public async Task<Resultado<bool>> CambiarEstadoAsync(string id, string adminActualId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return Resultado<bool>.Falla("La cuenta ya no existe.");
        if (user.Id == adminActualId) return Resultado<bool>.Falla("No puedes desactivar tu propia cuenta.");

        user.Activo = !user.Activo;
        var res = await _userManager.UpdateAsync(user);
        if (!res.Succeeded) return Resultado<bool>.Falla(res.Errors.Select(e => e.Description));

        // Invalida las sesiones abiertas de esa persona para que el bloqueo sea inmediato.
        await _userManager.UpdateSecurityStampAsync(user);
        return Resultado<bool>.Ok(user.Activo);
    }

    public async Task<Resultado> RestablecerPasswordAsync(string id, string nuevaPassword, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return Resultado.Falla("La cuenta ya no existe.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var res = await _userManager.ResetPasswordAsync(user, token, nuevaPassword);
        return res.Succeeded ? Resultado.Ok() : Resultado.Falla(res.Errors.Select(e => e.Description));
    }

    public async Task<Resultado> EliminarAsync(string id, string adminActualId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return Resultado.Falla("La cuenta ya no existe.");
        if (user.Id == adminActualId) return Resultado.Falla("No puedes eliminar tu propia cuenta.");

        var docente = await _db.Docentes.Include(d => d.Cursos).FirstOrDefaultAsync(d => d.UserId == id, ct);
        if (docente is not null)
        {
            if (docente.Cursos.Count > 0)
                return Resultado.Falla($"{user.NombreCompleto} tiene {docente.Cursos.Count} curso(s) a su cargo. Reasígnalos o elimínalos antes de borrar la cuenta.");
            _db.Docentes.Remove(docente);
        }

        var est = await _db.Estudiantes.FirstOrDefaultAsync(e => e.UserId == id, ct);
        if (est is not null) _db.Estudiantes.Remove(est); // sus notas, matrículas y asistencias se eliminan en cascada

        await _db.SaveChangesAsync(ct);

        var res = await _userManager.DeleteAsync(user);
        return res.Succeeded ? Resultado.Ok() : Resultado.Falla(res.Errors.Select(e => e.Description));
    }
}
