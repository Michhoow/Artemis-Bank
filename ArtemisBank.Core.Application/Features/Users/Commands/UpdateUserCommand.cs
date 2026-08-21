using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.Users.Commands
{
    public class UpdateUserCommand : IRequest<UserOperationResult>
    {
        public string UserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Identification { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }

        public decimal? AdditionalAmount { get; set; }
        public string? UpdatedByUserId { get; set; }
    }

    public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator()
        {
            RuleFor(c => c.UserId).NotEmpty().WithMessage("El identificador del usuario es requerido.");
            RuleFor(c => c.FirstName).NotEmpty().WithMessage("El nombre es requerido.");
            RuleFor(c => c.LastName).NotEmpty().WithMessage("El apellido es requerido.");

            RuleFor(c => c.Identification)
                .NotEmpty().WithMessage("La cédula es requerida.")
                .Matches(@"^\d+$").WithMessage("La cédula solo puede contener dígitos.");

            RuleFor(c => c.Email)
                .NotEmpty().WithMessage("El correo es requerido.")
                .EmailAddress().WithMessage("El correo indicado no tiene un formato válido.");

            RuleFor(c => c.UserName)
                .NotEmpty().WithMessage("El nombre de usuario es requerido.")
                .MinimumLength(4).WithMessage("El nombre de usuario debe tener al menos 4 caracteres.");

            RuleFor(c => c.Password)
                .MinimumLength(8).When(c => !string.IsNullOrWhiteSpace(c.Password))
                .WithMessage("La contraseña debe tener al menos 8 caracteres.");

            RuleFor(c => c.ConfirmPassword)
                .Equal(c => c.Password).When(c => !string.IsNullOrWhiteSpace(c.Password))
                .WithMessage(AppMessages.UserPasswordsDoNotMatch);

            RuleFor(c => c.AdditionalAmount)
                .GreaterThanOrEqualTo(0m).When(c => c.AdditionalAmount.HasValue)
                .WithMessage("El monto adicional no puede ser negativo.");
        }
    }

    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserOperationResult>
    {
        private readonly IUserManagementService _service;

        public UpdateUserCommandHandler(IUserManagementService service) => _service = service;

        public async Task<UserOperationResult> Handle(UpdateUserCommand request,
            CancellationToken cancellationToken)
        {
            var result = await _service.UpdateAsync(new UpdateUserRequest
            {
                UserId = request.UserId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Identification = request.Identification,
                Email = request.Email,
                UserName = request.UserName,
                Password = request.Password,
                ConfirmPassword = request.ConfirmPassword,
                AdditionalAmount = request.AdditionalAmount
            }, request.UpdatedByUserId, cancellationToken);

            if (!result.Succeeded)
            {
                if (result.Error == AppMessages.UserNotFound)
                    throw new NotFoundException(result.Error);

                if (result.Error == AppMessages.UserNameAlreadyExists ||
                    result.Error == AppMessages.UserEmailAlreadyExists ||
                    result.Error == AppMessages.UserIdentificationAlreadyExists)
                    throw new ConflictException(result.Error);
            }

            return result;
        }
    }
}
