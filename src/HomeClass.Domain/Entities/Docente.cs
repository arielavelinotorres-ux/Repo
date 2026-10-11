namespace HomeClass.Domain.Entities;

public class Docente
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser Usuario { get; set; } = null!;

    public string Especialidad { get; set; } = string.Empty;

    public ICollection<Curso> Cursos { get; set; } = new List<Curso>();
}
