using BaseApp.Application.DTOs;
using FluentValidation;

namespace BaseApp.Application.Validators;

/// <summary>
/// Define las reglas de validación para el DTO de inicio de sesión (LoginUsuarioDto).
/// </summary>
public class LoginUsuarioDtoValidator : AbstractValidator<LoginUsuarioDto>
{
    public LoginUsuarioDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio.")
            .EmailAddress().WithMessage("El formato del email no es válido.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.");
    }
}