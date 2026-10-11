# Entorno Académico HomeClass

Rediseño completo de EduBridge. Plataforma académica con tres roles (Administración, Docente y Estudiante),
cada uno con su propio panel y su propio balance (dashboard).

- ASP.NET Core MVC (.NET 9), EF Core 9, SQL Server, ASP.NET Identity
- Bootstrap 5, Bootstrap Icons y Chart.js incluidos **localmente** (no depende de CDNs; solo las tipografías vienen de Google Fonts y tienen alternativa del sistema)

## 1. Cómo ejecutarlo

Requisitos: SDK de .NET 9 y SQL Server LocalDB (viene con Visual Studio) o cualquier SQL Server.

1. Abre `HomeClass.slnx` en Visual Studio 2022 (17.13 o superior), o entra a la carpeta en una terminal.
2. Si no usas LocalDB, cambia `ConnectionStrings:DefaultConnection` en `src/HomeClass.Web/appsettings.json`.
3. Ejecuta:
   ```
   dotnet run --project src/HomeClass.Web
   ```
4. Abre la URL que muestre la consola (por defecto https://localhost:7120).

Al primer arranque se crea la base `HomeClassDb`, los roles, el administrador y los datos de demostración.

### Cuentas de demostración

| Rol | Correo | Contraseña |
|---|---|---|
| Administración | admin@homeclass.edu | Admin#2026 |
| Docente | docente@homeclass.edu | Docente#2026 |
| Estudiante | estudiante@homeclass.edu | Estudiante#2026 |

Hay además 3 docentes más y 13 estudiantes más (`nombre.apellido@homeclass.edu`, misma contraseña de su rol).
La pantalla de ingreso muestra botones para rellenar estas cuentas **solo en Desarrollo**.
Cambia las contraseñas en la sección `Seed` de `appsettings.json` antes de publicar y pon `CargarDatosDemo` en `false`.

## 2. Migraciones (recomendado)

Sin migraciones, la base se crea directamente desde el modelo (`EnsureCreated`) para que puedas arrancar de inmediato.
Para trabajar con migraciones de EF Core:

1. Elimina la base de datos creada (en el Explorador de objetos de SQL Server, o `dotnet ef database drop`).
2. Instala la herramienta una vez: `dotnet tool install --global dotnet-ef`
3. Desde la carpeta de la solución:
   ```
   dotnet ef migrations add Inicial --project src/HomeClass.Infrastructure --startup-project src/HomeClass.Web
   ```
4. Vuelve a ejecutar la app: detecta la migración y la aplica sola.

## 3. Qué ve cada rol

**Administración**: balance institucional (cuentas, docentes, estudiantes, cursos, matrículas, promedio y asistencia general),
gráficos de riesgo, de estudiantes por rango de promedio y de promedio por curso, estudiantes críticos, carga docente.
Gestiona usuarios (crear, editar, activar o desactivar, restablecer contraseña, eliminar), cursos y matrículas, y exporta el reporte de riesgo a CSV.

**Docente**: balance de SUS cursos (promedio, asistencia, riesgo, evolución de la clase, próximas evaluaciones, quién necesita atención).
Por curso: lista de estudiantes, libro de calificaciones (cada nota se guarda sola y recalcula el promedio en vivo),
asistencia por sesión y matrícula. Un docente nunca puede ver ni tocar cursos de otro.

**Estudiante**: su boletín (anillo con promedio general, asistencia, nivel), sus notas comparadas con el promedio del grupo,
próximas evaluaciones, últimas calificaciones y recomendaciones. Es de solo lectura y solo ve sus propios cursos.

## 4. Qué cambió respecto a EduBridge

### Base de datos
- **Eliminado**: `Productos` y `Categorias` (restos de plantilla), `CatalogoEvaluaciones` (reemplazado por el tipo de evaluación), y las columnas duplicadas que obligaban a sincronizar datos a mano.
- **Una sola fuente de verdad**: nombre y correo viven solo en la cuenta (`Usuarios`). `Docentes` y `Estudiantes` guardan solo datos propios de su rol y apuntan a la cuenta (antes la cuenta apuntaba a ambos con dos columnas nulas).
- **Evaluaciones bien modeladas**: antes cada fila mezclaba la actividad y la nota de un alumno. Ahora `Evaluaciones` es la actividad (con tipo, ponderación y fecha) y `Calificaciones` es la nota de cada estudiante.
- **Asistencia real**: antes era un solo porcentaje guardado en el estudiante. Ahora hay una tabla `Asistencias` por curso, estudiante y fecha, y el porcentaje se calcula.
- **Integridad**: índices únicos (carnet, código de curso, una nota por evaluación y estudiante, una asistencia por día), tipos `decimal(5,2)` y `date`, y borrado en cascada controlado.
- Tablas en español sin prefijos (`Usuarios`, `Cursos`, `Calificaciones`...).

### Arquitectura y buenas prácticas
- 4 proyectos: `Domain` (entidades y reglas de negocio puras), `Application` (servicios y DTOs), `Infrastructure` (EF Core y datos iniciales) y `Web` (MVC).
- Controladores delgados: la lógica está en servicios; los promedios y el riesgo se calculan en un solo lugar para que los tres dashboards sean coherentes.
- Resultados explícitos (`Resultado`) en vez de excepciones para errores de negocio.
- Autorización por rol y por área, y comprobación de propiedad en cada operación del docente.
- Protección anti-falsificación (CSRF) en todos los POST, incluidas las llamadas `fetch`.
- Sin registro público: las cuentas las crea la administración. Bloqueo tras 5 intentos fallidos. Desactivar una cuenta cierra su sesión en menos de un minuto.
- Mensajes de Identity en español, cultura fija es-NI, contraseñas de datos iniciales fuera del código.
- Sin jQuery: JavaScript propio y ligero.

### Diseño e interacciones
- Concepto visual "pizarra", acento distinto por rol, tema claro y oscuro, adaptable a móvil.
- Búsqueda y filtros en vivo, tablas ordenables, confirmaciones en modal, avisos (toasts), contadores animados.
- Libro de calificaciones editable con navegación por teclado, asistencia con "marcar todos" y contadores, matrícula con búsqueda.

## 5. Reglas de negocio

- Promedio del curso = Σ(nota × ponderación) / Σ(ponderación) de las evaluaciones ya calificadas. Las ponderaciones de un curso no pueden pasar de 100%.
- Presente, tardanza y ausencia justificada cuentan como asistencia.
- Riesgo **alto**: promedio menor a 70 o asistencia menor a 75%. **Moderado**: promedio menor a 80 o asistencia menor a 85%. Los umbrales están en `Domain/Rules/EvaluadorRiesgo.cs`.
- Al retirar a un estudiante de un curso se borran sus notas y asistencias de ese curso.

## 6. Estructura

```
src/
  HomeClass.Domain/          entidades, enums, reglas (CalculadoraNotas, EvaluadorRiesgo)
  HomeClass.Application/     servicios, DTOs, interfaz IApplicationDbContext
  HomeClass.Infrastructure/  AppDbContext, configuraciones de EF, seeder
  HomeClass.Web/
    Areas/Admin, Areas/Docente, Areas/Estudiante   (controladores y vistas por rol)
    Controllers, Views/Shared, Identity, Helpers, wwwroot
```
