using HomeClass.Domain.Enums;

namespace HomeClass.Domain.Rules;

public record Sugerencia(string Categoria, string Titulo, string Descripcion, string Prioridad);

/// <summary>Detección temprana de riesgo académico y sugerencias de intervención.</summary>
public static class EvaluadorRiesgo
{
    public const decimal PromedioMinimo = 70m;
    public const decimal AsistenciaMinima = 75m;
    public const decimal PromedioAlerta = 80m;
    public const decimal AsistenciaAlerta = 85m;

    public static NivelRiesgo Evaluar(decimal? promedio, decimal? asistencia)
    {
        if (promedio is null && asistencia is null) return NivelRiesgo.SinDatos;

        if (promedio < PromedioMinimo || asistencia < AsistenciaMinima) return NivelRiesgo.Alto;
        if (promedio < PromedioAlerta || asistencia < AsistenciaAlerta) return NivelRiesgo.Moderado;
        return NivelRiesgo.Bajo;
    }

    public static IReadOnlyList<Sugerencia> Sugerir(decimal? promedio, decimal? asistencia)
    {
        var nivel = Evaluar(promedio, asistencia);
        var lista = new List<Sugerencia>();

        if (nivel == NivelRiesgo.SinDatos) return lista;

        if (promedio < PromedioMinimo)
        {
            lista.Add(new Sugerencia("Académica", "Tutoría de refuerzo",
                $"El promedio es {promedio:0.0}, por debajo del mínimo de {PromedioMinimo:0}. Agendar dos horas semanales de tutoría en los temas con menor nota.",
                "Alta"));
            lista.Add(new Sugerencia("Académica", "Estudio acompañado",
                "Asignar a un compañero con buen desempeño para repasar ejercicios y laboratorios.",
                "Media"));
        }

        if (asistencia < AsistenciaMinima)
        {
            lista.Add(new Sugerencia("Asistencia", "Conversar con coordinación",
                $"La asistencia es {asistencia:0.0}%, por debajo del {AsistenciaMinima:0}% requerido. Conocer las causas y acordar un compromiso por escrito.",
                "Alta"));
        }

        if (nivel == NivelRiesgo.Moderado)
        {
            lista.Add(new Sugerencia("Prevención", "Taller de hábitos de estudio",
                "Invitar al taller de organización del tiempo antes de que el rendimiento baje más.",
                "Media"));
        }

        if (nivel == NivelRiesgo.Bajo)
        {
            lista.Add(new Sugerencia("Reconocimiento", "Mantener el ritmo",
                "Buen desempeño y asistencia. Se puede proponer participación en proyectos avanzados o monitoría.",
                "Baja"));
        }

        return lista;
    }
}
