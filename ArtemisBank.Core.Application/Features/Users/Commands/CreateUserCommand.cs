using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.Users.Commands
{
    public class CreateUserCommand : IRequest<UserOperationResult>
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Identification { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string Role { get; set; } = "Cliente";

        public decimal? InitialAmount { get; set; }

        public string? CreatedByUserId { get; set; }

        public bool SendTokenInsteadOfLink { get; set; }
    }

    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        private static readonly string[] AllowedRoles = { "administrador", "cajero", "cliente" };

        public CreateUserCommandValidator()
        {
            RuleFor(c => c.FirstName).NotEmpty().WithMessage("El nombre es requerido.")
                .MaximumLength(100);

            RuleFor(c => c.LastName).NotEmpty().WithMessage("El apellido es requerido.")
                .MaximumLength(100);

            RuleFor(c => c.Identification)
                .NotEmpty().WithMessage("La cédula es requerida.")
                .Matches(@"^\d{11}$").WithMessage("La cédula debe contener 11 dígitos.");

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

            RuleFor(c => c.Role)
                .Must(r => !string.IsNullOrWhiteSpace(r) && AllowedRoles.Contains(r.Trim().ToLowerInvariant()))
                .WithMessage("El rol solo puede ser Administrador, Cajero o Cliente.");

            RuleFor(c => c.InitialAmount)
                .GreaterThanOrEqualTo(0m).When(c => c.InitialAmount.HasValue)
                .WithMessage(AppMessages.NegativeInitialBalance);
        }
    }

    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserOperationResult>
    {
        private readonly IUserManagementService _service;

        public CreateUserCommandHandler(IUserManagementService service) => _service = service;

        public async Task<UserOperationResult> Handle(CreateUserCommand request,
            CancellationToken cancellationToken)
        {
            var result = await _service.CreateAsync(new CreateUserRequest
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Identification = request.Identification,
                Email = request.Email,
                UserName = request.UserName,
                Password = request.Password,
                ConfirmPassword = request.ConfirmPassword,
                Role = request.Role,
                InitialAmount = request.InitialAmount
            }, request.CreatedByUserId, request.SendTokenInsteadOfLink, cancellationToken);

            if (!result.Succeeded && IsUniquenessError(result.Error))
                throw new ConflictException(result.Error!);

            return result;
        }

        private static bool IsUniquenessError(string? error) =>
            error == AppMessages.UserNameAlreadyExists ||
            error == AppMessages.UserEmailAlreadyExists ||
            error == AppMessages.UserIdentificationAlreadyExists;
    }
}
