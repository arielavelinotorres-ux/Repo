using HomeClass.Application.Abstractions;
using HomeClass.Application.Dtos;
using HomeClass.Application.Services.Internal;

namespace HomeClass.Application.Services;

public interface IRiesgoService
{
    /// <param name="docenteId">Si se indica, el reporte solo considera los cursos de ese docente.</param>
    Task<ReporteRiesgoDto> ObtenerReporteAsync(int? docenteId = null, CancellationToken ct = default);
}

public class RiesgoService : IRiesgoService
{
    private readonly IApplicationDbContext _db;
    public RiesgoService(IApplicationDbContext db) => _db = db;

    public async Task<ReporteRiesgoDto> ObtenerReporteAsync(int? docenteId = null, CancellationToken ct = default)
    {
        var snap = await SnapshotRendimiento.CargarAsync(_db, docenteId: docenteId, ct: ct);
        var alumnos = snap.AlumnosRiesgo();
        return new ReporteRiesgoDto(alumnos, DistribucionRiesgoDto.De(alumnos.Select(a => a.Nivel)));
    }
}
