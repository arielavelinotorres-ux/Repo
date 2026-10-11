using HomeClass.Application.Abstractions;
using HomeClass.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HomeClass.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Docente> Docentes => Set<Docente>();
    public DbSet<Estudiante> Estudiantes => Set<Estudiante>();
    public DbSet<Curso> Cursos => Set<Curso>();
    public DbSet<Inscripcion> Inscripciones => Set<Inscripcion>();
    public DbSet<Evaluacion> Evaluaciones => Set<Evaluacion>();
    public DbSet<Calificacion> Calificaciones => Set<Calificacion>();
    public DbSet<Asistencia> Asistencias => Set<Asistencia>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Tablas de Identity con nombres en español
        builder.Entity<ApplicationUser>(e =>
        {
            e.ToTable("Usuarios");
            e.Property(u => u.NombreCompleto).IsRequired().HasMaxLength(150);
        });
        builder.Entity<IdentityRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("UsuarioRoles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UsuarioClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UsuarioLogins");
        builder.Entity<IdentityUserToken<string>>().ToTable("UsuarioTokens");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RolClaims");

        // Una configuración por entidad (carpeta Configurations)
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
