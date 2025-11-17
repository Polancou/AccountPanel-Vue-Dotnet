using AccountPanel.Application.DTOs;
using FluentValidation;

namespace AccountPanel.Application.Validators;

public class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordDtoValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("El token de restablecimiento es obligatorio.");
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
