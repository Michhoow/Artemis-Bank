using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.Users.Commands
{
    public class CreateCommerceUserCommand : IRequest<UserOperationResult>
    {
        public int CommerceId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Identification { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public bool SendTokenInsteadOfLink { get; set; } = true;
    }

    public class CreateCommerceUserCommandValidator : AbstractValidator<CreateCommerceUserCommand>
    {
        public CreateCommerceUserCommandValidator()
        {
            RuleFor(c => c.CommerceId)
                .GreaterThan(0).WithMessage("El identificador del comercio debe ser mayor que cero.");

            RuleFor(c => c.FirstName).NotEmpty().WithMessage("El nombre es requerido.");
            RuleFor(c => c.LastName).NotEmpty().WithMessage("El apellido es requerido.");

            RuleFor(c => c.Identification)
                .NotEmpty().WithMessage("La identificación es requerida.")
                .Matches(@"^\d+$").WithMessage("La identificación solo puede contener dígitos.");

            RuleFor(c => c.Email)
                .NotEmpty().WithMessage("El correo es requerido.")
                .EmailAddress().WithMessage("El correo indicado no tiene un formato válido.");

            RuleFor(c => c.UserName)
                .NotEmpty().WithMessage("El nombre de usuario es requerido.")
                .MinimumLength(4).WithMessage("El nombre de usuario debe tener al menos 4 caracteres.");

            RuleFor(c => c.Password)
                .NotEmpty().WithMessage("La contraseña es requerida.")
                .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.");

            RuleFor(c => c.ConfirmPassword)
                .Equal(c => c.Password).WithMessage(AppMessages.UserPasswordsDoNotMatch);
        }
    }

    public class CreateCommerceUserCommandHandler
        : IRequestHandler<CreateCommerceUserCommand, UserOperationResult>
    {
        private readonly IUserManagementService _service;

        public CreateCommerceUserCommandHandler(IUserManagementService service) => _service = service;

        public async Task<UserOperationResult> Handle(CreateCommerceUserCommand request,
            CancellationToken cancellationToken)
        {
            var result = await _service.CreateCommerceUserAsync(new CreateUserRequest
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Identification = request.Identification,
                Email = request.Email,
                UserName = request.UserName,
                Password = request.Password,
                ConfirmPassword = request.ConfirmPassword,
                Role = "Comercio"
            }, request.CommerceId, request.SendTokenInsteadOfLink, cancellationToken);

            if (!result.Succeeded)
            {
                if (result.Error == AppMessages.CommerceNotFound)
                    throw new NotFoundException(result.Error);

                if (result.Error == AppMessages.CommerceAlreadyHasUser ||
                    result.Error == AppMessages.UserNameAlreadyExists ||
                    result.Error == AppMessages.UserEmailAlreadyExists ||
                    result.Error == AppMessages.UserIdentificationAlreadyExists)
                    throw new ConflictException(result.Error);
            }

            return result;
        }
    }
}
