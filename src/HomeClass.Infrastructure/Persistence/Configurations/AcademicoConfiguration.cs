using HomeClass.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeClass.Infrastructure.Persistence.Configurations;

public class CursoConfiguration : IEntityTypeConfiguration<Curso>
{
    public void Configure(EntityTypeBuilder<Curso> b)
    {
        b.ToTable("Cursos");
        b.Property(c => c.Codigo).IsRequired().HasMaxLength(20);
        b.Property(c => c.Nombre).IsRequired().HasMaxLength(150);
        b.Property(c => c.Descripcion).HasMaxLength(500);
        b.HasIndex(c => c.Codigo).IsUnique();

        b.HasOne(c => c.Docente).WithMany(d => d.Cursos).HasForeignKey(c => c.DocenteId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class InscripcionConfiguration : IEntityTypeConfiguration<Inscripcion>
{
    public void Configure(EntityTypeBuilder<Inscripcion> b)
    {
        b.ToTable("Inscripciones");
        b.HasKey(i => new { i.CursoId, i.EstudianteId });
        b.HasIndex(i => i.EstudianteId);

        b.HasOne(i => i.Curso).WithMany(c => c.Inscripciones).HasForeignKey(i => i.CursoId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(i => i.Estudiante).WithMany(e => e.Inscripciones).HasForeignKey(i => i.EstudianteId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class EvaluacionConfiguration : IEntityTypeConfiguration<Evaluacion>
{
    public void Configure(EntityTypeBuilder<Evaluacion> b)
    {
        b.ToTable("Evaluaciones");
        b.Property(e => e.Titulo).IsRequired().HasMaxLength(120);
        b.Property(e => e.Ponderacion).HasColumnType("decimal(5,2)");
        b.Property(e => e.Fecha).HasColumnType("date");
        b.HasIndex(e => e.CursoId);

        b.HasOne(e => e.Curso).WithMany(c => c.Evaluaciones).HasForeignKey(e => e.CursoId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CalificacionConfiguration : IEntityTypeConfiguration<Calificacion>
{
    public void Configure(EntityTypeBuilder<Calificacion> b)
    {
        b.ToTable("Calificaciones");
        b.Property(c => c.Nota).HasColumnType("decimal(5,2)");

        // Una sola nota por estudiante y evaluación
        b.HasIndex(c => new { c.EvaluacionId, c.EstudianteId }).IsUnique();
        b.HasIndex(c => c.EstudianteId);

        b.HasOne(c => c.Evaluacion).WithMany(e => e.Calificaciones).HasForeignKey(c => c.EvaluacionId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(c => c.Estudiante).WithMany(e => e.Calificaciones).HasForeignKey(c => c.EstudianteId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class AsistenciaConfiguration : IEntityTypeConfiguration<Asistencia>
{
    public void Configure(EntityTypeBuilder<Asistencia> b)
    {
        b.ToTable("Asistencias");
        b.Property(a => a.Fecha).HasColumnType("date");

        // Un solo registro por estudiante, curso y día
        b.HasIndex(a => new { a.CursoId, a.EstudianteId, a.Fecha }).IsUnique();
        b.HasIndex(a => a.EstudianteId);

        b.HasOne(a => a.Curso).WithMany(c => c.Asistencias).HasForeignKey(a => a.CursoId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(a => a.Estudiante).WithMany(e => e.Asistencias).HasForeignKey(a => a.EstudianteId).OnDelete(DeleteBehavior.Cascade);
    }
}
