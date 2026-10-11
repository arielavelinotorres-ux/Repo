using HomeClass.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeClass.Infrastructure.Persistence.Configurations;

public class DocenteConfiguration : IEntityTypeConfiguration<Docente>
{
    public void Configure(EntityTypeBuilder<Docente> b)
    {
        b.ToTable("Docentes");
        b.Property(d => d.Especialidad).HasMaxLength(150);
        b.HasIndex(d => d.UserId).IsUnique();

        // Si existen cursos a su cargo no se puede borrar al docente: el servicio lo valida con un mensaje claro.
        b.HasOne(d => d.Usuario).WithOne().HasForeignKey<Docente>(d => d.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EstudianteConfiguration : IEntityTypeConfiguration<Estudiante>
{
    public void Configure(EntityTypeBuilder<Estudiante> b)
    {
        b.ToTable("Estudiantes");
        b.Property(e => e.Carnet).IsRequired().HasMaxLength(30);
        b.Property(e => e.Carrera).HasMaxLength(120);
        b.Property(e => e.Telefono).HasMaxLength(30);
        b.Property(e => e.ContactoEmergencia).HasMaxLength(200);
        b.Property(e => e.FechaNacimiento).HasColumnType("date");

        b.HasIndex(e => e.Carnet).IsUnique();
        b.HasIndex(e => e.UserId).IsUnique();

        b.HasOne(e => e.Usuario).WithOne().HasForeignKey<Estudiante>(e => e.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
