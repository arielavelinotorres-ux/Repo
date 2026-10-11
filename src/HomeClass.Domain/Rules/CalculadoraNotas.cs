using HomeClass.Domain.Enums;

namespace HomeClass.Domain.Rules;

/// <summary>Reglas de cálculo académico. Funciones puras: fáciles de probar.</summary>
public static class CalculadoraNotas
{
    /// <summary>
    /// Promedio ponderado de las notas ya registradas: Σ(nota × peso) / Σ(peso).
    /// Devuelve null si todavía no hay ninguna nota.
    /// </summary>
    public static decimal? PromedioPonderado(IEnumerable<(decimal Nota, decimal Ponderacion)> notas)
    {
        decimal suma = 0, pesos = 0;
        foreach (var (nota, peso) in notas)
        {
            suma += nota * peso;
            pesos += peso;
        }
        return pesos <= 0 ? null : Math.Round(suma / pesos, 1);
    }

    /// <summary>Presente, tardanza y ausencia justificada cuentan como asistencia.</summary>
    public static bool CuentaComoAsistencia(EstadoAsistencia estado) => estado != EstadoAsistencia.Ausente;

    public static decimal? PorcentajeAsistencia(IEnumerable<EstadoAsistencia> estados)
    {
        int total = 0, asistidas = 0;
        foreach (var e in estados)
        {
            total++;
            if (CuentaComoAsistencia(e)) asistidas++;
        }
        return total == 0 ? null : Math.Round(asistidas * 100m / total, 1);
    }
}
