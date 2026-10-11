namespace HomeClass.Domain.Constants;

/// <summary>Nombres de los tres roles de la plataforma.</summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Docente = "Docente";
    public const string Estudiante = "Estudiante";

    public static readonly string[] Todos = [Admin, Docente, Estudiante];
}
