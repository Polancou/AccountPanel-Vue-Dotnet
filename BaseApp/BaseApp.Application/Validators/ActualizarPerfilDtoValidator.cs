using BaseApp.Application.DTOs;
using FluentValidation;

namespace BaseApp.Application.Validators;

/// <summary>
/// Define las reglas de validación para el DTO de actualizar usuario (ActualizarPerfilDto).
/// </summary>
public class ActualizarPerfilDtoValidator : AbstractValidator<ActualizarPerfilDto>
{
    public ActualizarPerfilDtoValidator()
    {
        RuleFor(x => x.NombreCompleto).NotEmpty().Length(3, 100);
        RuleFor(x => x.NumeroTelefono).NotEmpty();
    }
}