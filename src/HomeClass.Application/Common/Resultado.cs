namespace HomeClass.Application.Common;

/// <summary>Resultado de una operación: evita usar excepciones para errores de negocio esperados.</summary>
public class Resultado
{
    public IReadOnlyList<string> Errores { get; init; } = Array.Empty<string>();
    public bool Exitoso => Errores.Count == 0;
    public string PrimerError => Errores.FirstOrDefault() ?? string.Empty;

    public static Resultado Ok() => new();
    public static Resultado Falla(params string[] errores) => new() { Errores = errores };
    public static Resultado Falla(IEnumerable<string> errores) => new() { Errores = errores.ToList() };
}

public class Resultado<T> : Resultado
{
    public T? Valor { get; init; }

    public static Resultado<T> Ok(T valor) => new() { Valor = valor };
    public new static Resultado<T> Falla(params string[] errores) => new() { Errores = errores };
    public new static Resultado<T> Falla(IEnumerable<string> errores) => new() { Errores = errores.ToList() };
}
