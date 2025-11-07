using AccountPanel.Application.DTOs;
using FluentValidation;

namespace AccountPanel.Application.Validators;

public class CambiarPasswordDtoValidator : AbstractValidator<CambiarPasswordDto>
{
    public CambiarPasswordDtoValidator()
    {
        RuleFor(x => x.OldPassword)
            .NotEmpty().WithMessage("La contraseña actual es obligatoria.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("La nueva contraseña es obligatoria.")
            .MinimumLength(8).WithMessage("La nueva contraseña debe tener al menos 8 caracteres.")
            .Matches("[A-Z]").WithMessage("La nueva contraseña debe contener al menos una mayúscula.")
            .Matches("[a-z]").WithMessage("La nueva contraseña debe contener al menos una minúscula.")
            .Matches("[0-9]").WithMessage("La nueva contraseña debe contener al menos un número.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Las contraseñas nuevas no coinciden.");
    }
}
