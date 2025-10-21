using BaseApp.Application.DTOs;
using FluentValidation;

namespace BaseApp.Application.Validators;

/// <summary>
/// Define las reglas de validación para el DTO de login externo (ExternalLoginDto).
/// </summary>
public class ExternalLoginDtoValidator : AbstractValidator<ExternalLoginDto>
{
    public ExternalLoginDtoValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("El proveedor es obligatorio.");

        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("El token de ID es obligatorio.");
    }
}