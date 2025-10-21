using BaseApp.Application.DTOs;
using FluentValidation;

namespace BaseApp.Application.Validators;

/// <summary>
/// Define las reglas de validación para el DTO de registro de usuario (RegistroUsuarioDto).
/// Hereda de AbstractValidator, que es la clase base de FluentValidation.
/// </summary>
public class RegistroUsuarioDtoValidator : AbstractValidator<RegistroUsuarioDto>
{
    public RegistroUsuarioDtoValidator()
    {
        // Regla para NombreCompleto
        RuleFor(x => x.NombreCompleto)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .Length(3, 100).WithMessage("El nombre debe tener entre 3 y 100 caracteres.");

        // Regla para Email
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El formato del email no es válido.");

        // Regla para Password
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.")
            .Matches("[A-Z]").WithMessage("La contraseña debe contener al menos una mayúscula.")
            .Matches("[a-z]").WithMessage("La contraseña debe contener al menos una minúscula.")
            .Matches("[0-9]").WithMessage("La contraseña debe contener al menos un número.");

        // Regla para NumeroTelefono
        RuleFor(x => x.NumeroTelefono)
            .NotEmpty().WithMessage("El número de teléfono es obligatorio.");
    }
}