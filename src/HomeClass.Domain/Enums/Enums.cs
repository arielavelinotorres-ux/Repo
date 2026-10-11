namespace HomeClass.Domain.Enums;

public enum TipoEvaluacion
{
    Examen = 1,
    Quiz = 2,
    Tarea = 3,
    Proyecto = 4,
    Laboratorio = 5,
    Participacion = 6
}

public enum EstadoAsistencia
{
    Presente = 1,
    Tardanza = 2,
    Justificada = 3,
    Ausente = 4
}

/// <summary>El orden numérico importa: 0 es lo más urgente y se muestra primero.</summary>
public enum NivelRiesgo
{
    Alto = 0,
    Moderado = 1,
    Bajo = 2,
    SinDatos = 3
}

public static class EnumTextos
{
    public static string Texto(this TipoEvaluacion t) => t switch
    {
        TipoEvaluacion.Examen => "Examen",
        TipoEvaluacion.Quiz => "Quiz",
        TipoEvaluacion.Tarea => "Tarea",
        TipoEvaluacion.Proyecto => "Proyecto",
        TipoEvaluacion.Laboratorio => "Laboratorio",
        TipoEvaluacion.Participacion => "Participación",
        _ => t.ToString()
    };

    public static string Texto(this EstadoAsistencia e) => e switch
    {
        EstadoAsistencia.Presente => "Presente",
        EstadoAsistencia.Tardanza => "Tardanza",
        EstadoAsistencia.Justificada => "Justificada",
        EstadoAsistencia.Ausente => "Ausente",
        _ => e.ToString()
    };

    public static string Texto(this NivelRiesgo n) => n switch
    {
        NivelRiesgo.Alto => "Riesgo alto",
        NivelRiesgo.Moderado => "Riesgo moderado",
        NivelRiesgo.Bajo => "En buen camino",
        _ => "Sin datos"
    };
}
