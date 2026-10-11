using Microsoft.AspNetCore.Identity;

namespace HomeClass.Web.Identity;

/// <summary>Mensajes de Identity en español.</summary>
public class DescriptorErroresEs : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => Error(nameof(DefaultError), "Ocurrió un error inesperado.");
    public override IdentityError DuplicateEmail(string email) => Error(nameof(DuplicateEmail), $"El correo {email} ya está registrado.");
    public override IdentityError DuplicateUserName(string userName) => Error(nameof(DuplicateUserName), $"El correo {userName} ya está registrado.");
    public override IdentityError InvalidEmail(string? email) => Error(nameof(InvalidEmail), "El correo electrónico no es válido.");
    public override IdentityError InvalidUserName(string? userName) => Error(nameof(InvalidUserName), "El nombre de usuario no es válido.");
    public override IdentityError PasswordMismatch() => Error(nameof(PasswordMismatch), "La contraseña actual no es correcta.");
    public override IdentityError PasswordTooShort(int length) => Error(nameof(PasswordTooShort), $"La contraseña debe tener al menos {length} caracteres.");
    public override IdentityError PasswordRequiresDigit() => Error(nameof(PasswordRequiresDigit), "La contraseña debe incluir al menos un número.");
    public override IdentityError PasswordRequiresLower() => Error(nameof(PasswordRequiresLower), "La contraseña debe incluir al menos una letra minúscula.");
    public override IdentityError PasswordRequiresUpper() => Error(nameof(PasswordRequiresUpper), "La contraseña debe incluir al menos una letra mayúscula.");
    public override IdentityError PasswordRequiresNonAlphanumeric() => Error(nameof(PasswordRequiresNonAlphanumeric), "La contraseña debe incluir al menos un símbolo.");

    private static IdentityError Error(string code, string description) => new() { Code = code, Description = description };
}
