using HomeClass.Domain.Constants;
using HomeClass.Domain.Entities;
using HomeClass.Domain.Enums;
using HomeClass.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HomeClass.Infrastructure.Seed;

internal static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp, SeedOptions opt)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var rol in Roles.Todos)
        {
            if (!await roleManager.RoleExistsAsync(rol))
                await roleManager.CreateAsync(new IdentityRole(rol));
        }

        if (await userManager.FindByEmailAsync(opt.AdminEmail) is null)
            await CrearUsuarioAsync(userManager, opt.AdminEmail, opt.AdminNombre, opt.AdminPassword, Roles.Admin);

        if (opt.CargarDatosDemo && !await db.Docentes.AnyAsync() && !await db.Estudiantes.AnyAsync())
            await CargarDemoAsync(db, userManager, opt);
    }

    private static async Task<ApplicationUser> CrearUsuarioAsync(UserManager<ApplicationUser> um, string email, string nombre, string password, string rol)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            NombreCompleto = nombre,
            Activo = true
        };

        var res = await um.CreateAsync(user, password);
        if (!res.Succeeded)
            throw new InvalidOperationException($"No se pudo crear la cuenta {email}: {string.Join("; ", res.Errors.Select(e => e.Description))}");

        await um.AddToRoleAsync(user, rol);
        return user;
    }

    private record DocenteSeed(string Email, string Nombre, string Especialidad);
    private record CursoSeed(string Codigo, string Nombre, string Descripcion, int DocenteIndice);
    private record EstudianteSeed(string Email, string Nombre, string Carnet, int Semestre, int Rendimiento, double Asistencia, string Telefono, string Contacto, DateTime Nacimiento);
    private record EvaluacionSeed(string Titulo, TipoEvaluacion Tipo, decimal Peso, int DiasDesdeHoy);

    private static async Task CargarDemoAsync(AppDbContext db, UserManager<ApplicationUser> um, SeedOptions opt)
    {
        var docentesSeed = new[]
        {
            new DocenteSeed("docente@homeclass.edu", "Roberto Sánchez", "Redes y Telecomunicaciones"),
            new DocenteSeed("ana.lopez@homeclass.edu", "Ana López", "Inteligencia Artificial y Machine Learning"),
            new DocenteSeed("jorge.castillo@homeclass.edu", "Jorge Castillo", "Ingeniería de Software y DevOps"),
            new DocenteSeed("patricia.morales@homeclass.edu", "Patricia Morales", "Matemática Aplicada y Estadística")
        };

        var cursosSeed = new[]
        {
            new CursoSeed("RED-301", "Redes de Computadoras", "Modelo TCP/IP, direccionamiento y administración de infraestructura de red.", 0),
            new CursoSeed("SEG-330", "Seguridad Informática", "Fundamentos de ciberseguridad, criptografía y buenas prácticas.", 0),
            new CursoSeed("INT-410", "Inteligencia Artificial Aplicada", "Introducción a machine learning y modelos predictivos.", 1),
            new CursoSeed("DEV-320", "DevOps y Metodologías Ágiles", "Integración continua, entrega continua, Scrum y buenas prácticas.", 2),
            new CursoSeed("EST-210", "Estadística para Ingeniería", "Probabilidad, estadística descriptiva e inferencial aplicada.", 3)
        };

        var estudiantesSeed = new[]
        {
            new EstudianteSeed("estudiante@homeclass.edu", "Gabriel Bermúdez", "EST-2026-001", 3, 76, 0.86, "8611-2201", "Madre, Rosa Bermúdez (8611-2200)", new DateTime(2004, 2, 14)),
            new EstudianteSeed("fernanda.aguilar@homeclass.edu", "Fernanda Aguilar", "EST-2026-002", 3, 63, 0.62, "8611-2202", "Padre, Óscar Aguilar (8611-2203)", new DateTime(2003, 7, 22)),
            new EstudianteSeed("kevin.espinoza@homeclass.edu", "Kevin Espinoza", "EST-2026-003", 4, 93, 0.97, "8611-2204", "Madre, Silvia Espinoza (8611-2205)", new DateTime(2003, 11, 3)),
            new EstudianteSeed("valeria.torres@homeclass.edu", "Valeria Torres", "EST-2026-004", 2, 72, 0.80, "8611-2206", "Tía, Carla Torres (8611-2207)", new DateTime(2004, 5, 30)),
            new EstudianteSeed("diego.sequeira@homeclass.edu", "Diego Sequeira", "EST-2026-005", 3, 66, 0.78, "8611-2208", "Padre, Manuel Sequeira (8611-2209)", new DateTime(2003, 9, 17)),
            new EstudianteSeed("anabelen.reyes@homeclass.edu", "Ana Belén Reyes", "EST-2026-006", 4, 97, 0.99, "8611-2210", "Madre, Julia Reyes (8611-2211)", new DateTime(2003, 4, 9)),
            new EstudianteSeed("mario.ruiz@homeclass.edu", "Mario Alberto Ruiz", "EST-2026-007", 2, 58, 0.70, "8611-2212", "Tutor, Pedro Ruiz (8611-2213)", new DateTime(2004, 8, 25)),
            new EstudianteSeed("daniela.castro@homeclass.edu", "Daniela Castro", "EST-2026-008", 5, 85, 0.90, "8611-2214", "Madre, Marta Castro (8611-2215)", new DateTime(2002, 12, 1)),
            new EstudianteSeed("josue.vargas@homeclass.edu", "Josué Vargas", "EST-2026-009", 3, 74, 0.84, "8611-2216", "Padre, Ramón Vargas (8611-2217)", new DateTime(2003, 6, 19)),
            new EstudianteSeed("melissa.cruz@homeclass.edu", "Melissa Cruz", "EST-2026-010", 4, 90, 0.95, "8611-2218", "Madre, Ivania Cruz (8611-2219)", new DateTime(2003, 3, 5)),
            new EstudianteSeed("ricardo.duarte@homeclass.edu", "Ricardo Duarte", "EST-2026-011", 2, 64, 0.88, "8611-2220", "Tía, Norma Duarte (8611-2221)", new DateTime(2004, 1, 27)),
            new EstudianteSeed("andrea.salgado@homeclass.edu", "Andrea Salgado", "EST-2026-012", 5, 95, 0.96, "8611-2222", "Padre, Felipe Salgado (8611-2223)", new DateTime(2002, 10, 14)),
            new EstudianteSeed("bryan.cerda@homeclass.edu", "Bryan Cerda", "EST-2026-013", 3, 55, 0.58, "8611-2224", "Madre, Yolanda Cerda (8611-2225)", new DateTime(2003, 2, 8)),
            new EstudianteSeed("ligia.somarriba@homeclass.edu", "Ligia Somarriba", "EST-2026-014", 4, 82, 0.92, "8611-2226", "Padre, Ernesto Somarriba (8611-2227)", new DateTime(2003, 8, 12))
        };

        // Las evaluaciones suman 100%. La última queda en el futuro y sin notas: aparece como "próxima evaluación".
        var evaluacionesSeed = new[]
        {
            new EvaluacionSeed("Quiz 1", TipoEvaluacion.Quiz, 10, -52),
            new EvaluacionSeed("Tarea 1", TipoEvaluacion.Tarea, 15, -41),
            new EvaluacionSeed("Parcial 1", TipoEvaluacion.Examen, 25, -27),
            new EvaluacionSeed("Proyecto", TipoEvaluacion.Proyecto, 25, -9),
            new EvaluacionSeed("Examen final", TipoEvaluacion.Examen, 25, 16)
        };

        // ---- Docentes y cursos ----
        var docentes = new List<Docente>();
        foreach (var d in docentesSeed)
        {
            var user = await CrearUsuarioAsync(um, d.Email, d.Nombre, opt.DocentePassword, Roles.Docente);
            var docente = new Docente { UserId = user.Id, Especialidad = d.Especialidad };
            db.Docentes.Add(docente);
            docentes.Add(docente);
        }
        await db.SaveChangesAsync();

        var cursos = new List<Curso>();
        foreach (var c in cursosSeed)
        {
            var curso = new Curso { Codigo = c.Codigo, Nombre = c.Nombre, Descripcion = c.Descripcion, DocenteId = docentes[c.DocenteIndice].Id };
            foreach (var e in evaluacionesSeed)
            {
                curso.Evaluaciones.Add(new Evaluacion
                {
                    Titulo = e.Titulo,
                    Tipo = e.Tipo,
                    Ponderacion = e.Peso,
                    Fecha = DateTime.Today.AddDays(e.DiasDesdeHoy)
                });
            }
            db.Cursos.Add(curso);
            cursos.Add(curso);
        }
        await db.SaveChangesAsync();

        // ---- Estudiantes ----
        var estudiantes = new List<Estudiante>();
        foreach (var s in estudiantesSeed)
        {
            var user = await CrearUsuarioAsync(um, s.Email, s.Nombre, opt.EstudiantePassword, Roles.Estudiante);
            var estudiante = new Estudiante
            {
                UserId = user.Id,
                Carnet = s.Carnet,
                Carrera = "Ingeniería en Sistemas",
                Semestre = s.Semestre,
                Telefono = s.Telefono,
                ContactoEmergencia = s.Contacto,
                FechaNacimiento = s.Nacimiento
            };
            db.Estudiantes.Add(estudiante);
            estudiantes.Add(estudiante);
        }
        await db.SaveChangesAsync();

        // ---- Matrículas, notas y asistencias (semilla fija: siempre los mismos datos) ----
        var random = new Random(2026);

        for (int i = 0; i < estudiantes.Count; i++)
        {
            var est = estudiantes[i];
            var seed = estudiantesSeed[i];
            var indicesCursos = new[] { i % cursos.Count, (i + 1) % cursos.Count, (i + 3) % cursos.Count }.Distinct();

            foreach (var idx in indicesCursos)
            {
                var curso = cursos[idx];
                db.Inscripciones.Add(new Inscripcion { CursoId = curso.Id, EstudianteId = est.Id });

                // Notas: las evaluaciones ya pasadas se califican; la futura queda pendiente.
                foreach (var ev in curso.Evaluaciones.Where(e => e.Fecha.Date <= DateTime.Today))
                {
                    var nota = Math.Clamp(seed.Rendimiento + random.Next(-9, 10), 35, 100);
                    db.Calificaciones.Add(new Calificacion { EvaluacionId = ev.Id, EstudianteId = est.Id, Nota = nota });
                }

                // 12 sesiones de clase, una cada 4 días hacia atrás
                for (int sesion = 1; sesion <= 12; sesion++)
                {
                    EstadoAsistencia estado;
                    if (random.NextDouble() < seed.Asistencia)
                        estado = random.NextDouble() < 0.88 ? EstadoAsistencia.Presente : EstadoAsistencia.Tardanza;
                    else
                        estado = random.NextDouble() < 0.80 ? EstadoAsistencia.Ausente : EstadoAsistencia.Justificada;

                    db.Asistencias.Add(new Asistencia
                    {
                        CursoId = curso.Id,
                        EstudianteId = est.Id,
                        Fecha = DateTime.Today.AddDays(-4 * sesion),
                        Estado = estado
                    });
                }
            }
        }

        await db.SaveChangesAsync();
    }
}
