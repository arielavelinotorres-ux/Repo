using System.Text;
using System.Text.Json;
using HomeClass.Application.Dtos;
using HomeClass.Domain.Enums;

namespace HomeClass.Web.Helpers;

/// <summary>Formato y clases CSS compartidos por las vistas.</summary>
public static class Ui
{
    public const string Rojo = "#d9483b";
    public const string Ambar = "#e3a008";
    public const string Verde = "#1f8a5b";
    public const string Azul = "#3b5bdb";
    public const string Gris = "#a3b1ab";

    public static string Nota(decimal? v) => v.HasValue ? v.Value.ToString("0.0") : "—";
    public static string Pct(decimal? v) => v.HasValue ? $"{v.Value:0.#}%" : "—";

    public static string ClaseNota(decimal? v) => v is null ? "sin" : v < 70 ? "mal" : v < 80 ? "medio" : "bien";
    public static string ClasePct(decimal? v) => v is null ? "sin" : v < 75 ? "mal" : v < 85 ? "medio" : "bien";

    public static string TonoColor(decimal? v) => v is null ? "var(--muted)" : v < 70 ? "var(--rojo)" : v < 80 ? "var(--ambar)" : "var(--verde)";

    public static string ClaseNivel(NivelRiesgo n) => n switch
    {
        NivelRiesgo.Alto => "nivel-alto",
        NivelRiesgo.Moderado => "nivel-moderado",
        NivelRiesgo.Bajo => "nivel-bajo",
        _ => "nivel-sin"
    };

    public static string ClaseAsistencia(EstadoAsistencia e) => e switch
    {
        EstadoAsistencia.Presente => "as-presente",
        EstadoAsistencia.Tardanza => "as-tardanza",
        EstadoAsistencia.Justificada => "as-justificada",
        _ => "as-ausente"
    };

    public static string Iniciales(string? nombre)
    {
        var partes = (nombre ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length switch
        {
            0 => "?",
            1 => partes[0][..1].ToUpperInvariant(),
            _ => (partes[0][..1] + partes[^1][..1]).ToUpperInvariant()
        };
    }

    public static string Fecha(DateTime f) => f.ToString("dd MMM yyyy");
    public static string FechaCorta(DateTime f) => f.ToString("dd MMM");
    public static string InputFecha(DateTime f) => f.ToString("yyyy-MM-dd");

    public static string Json(object datos) =>
        JsonSerializer.Serialize(datos, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

    /// <summary>Color de cada barra del histograma de promedios, de rojo (bajo) a verde (alto).</summary>
    public static readonly string[] ColoresRangos = [Rojo, "#e8703a", Ambar, "#7bb662", Verde];
    public static readonly string[] EtiquetasRangos = ["Menos de 60", "60 a 69", "70 a 79", "80 a 89", "90 a 100"];

    public static string[] ColoresRiesgo => [Rojo, Ambar, Verde, Gris];
    public static string[] EtiquetasRiesgo => ["Riesgo alto", "Riesgo moderado", "En buen camino", "Sin datos"];
    public static int[] ValoresRiesgo(DistribucionRiesgoDto d) => [d.Alto, d.Moderado, d.Bajo, d.SinDatos];

    /// <summary>CSV con BOM UTF-8 para que Excel muestre bien las tildes.</summary>
    public static byte[] CsvRiesgo(ReporteRiesgoDto reporte)
    {
        static string Esc(string? s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";

        var sb = new StringBuilder();
        sb.AppendLine("Estudiante,Carnet,Carrera,Cursos,Promedio,Asistencia %,Nivel,Primera sugerencia");
        foreach (var a in reporte.Alumnos)
        {
            sb.AppendLine(string.Join(",",
                Esc(a.Nombre), Esc(a.Carnet), Esc(a.Carrera), Esc(a.Cursos),
                a.Promedio?.ToString("0.0") ?? "", a.Asistencia?.ToString("0.#") ?? "",
                Esc(a.Nivel.Texto()), Esc(a.Sugerencias.FirstOrDefault()?.Titulo)));
        }
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }
}
