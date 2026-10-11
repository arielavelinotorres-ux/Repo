using HomeClass.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HomeClass.Application.Abstractions;

/// <summary>
/// Puerto de acceso a datos. La capa Application depende de esta interfaz,
/// no de la implementación concreta (SQL Server) que vive en Infrastructure.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<IdentityRole> Roles { get; }
    DbSet<IdentityUserRole<string>> UserRoles { get; }

    DbSet<Docente> Docentes { get; }
    DbSet<Estudiante> Estudiantes { get; }
    DbSet<Curso> Cursos { get; }
    DbSet<Inscripcion> Inscripciones { get; }
    DbSet<Evaluacion> Evaluaciones { get; }
    DbSet<Calificacion> Calificaciones { get; }
    DbSet<Asistencia> Asistencias { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
