using AccountPanel.Application.DTOs;
using FluentValidation;

namespace AccountPanel.Application.Validators;

public class ActualizarRolUsuarioDtoValidator : AbstractValidator<ActualizarRolUsuarioDto>
{
    public ActualizarRolUsuarioDtoValidator()
    {
        // Esta regla comprueba que el valor (ya sea string o número)
        // es un miembro válido y definido del enum RolUsuario.
        RuleFor(x => x.Rol)
            .IsInEnum()
            .WithMessage("El rol proporcionado no es válido.");
    }
}