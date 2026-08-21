using ArtemisBank.Core.Application.Interfaces.Contracts;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.Account.Commands
{
    public class LoginCommand : IRequest<LoginResult>
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResult
    {
        public bool Succeeded { get; set; }
        public string? Token { get; set; }
        public string? Error { get; set; }
    }

    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(c => c.UserName)
                .NotEmpty().WithMessage("El usuario es requerido.");

            RuleFor(c => c.Password)
                .NotEmpty().WithMessage("La contraseña es requerida.");
        }
    }

    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
    {
        private readonly IAccountAuthService _accountService;

        public LoginCommandHandler(IAccountAuthService accountService)
            => _accountService = accountService;

        public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var (token, error) = await _accountService.LoginAsync(request.UserName, request.Password);

            return new LoginResult
            {
                Succeeded = token is not null,
                Token = token,
                Error = error
            };
        }
    }

    public class ConfirmAccountCommand : IRequest<AccountOperationResult>
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class AccountOperationResult
    {
        public bool Succeeded { get; set; }
        public string? Error { get; set; }

        public static AccountOperationResult Ok() => new() { Succeeded = true };
        public static AccountOperationResult Fail(string? error) => new() { Succeeded = false, Error = error };
    }

    public class ConfirmAccountCommandValidator : AbstractValidator<ConfirmAccountCommand>
    {
        public ConfirmAccountCommandValidator()
        {
            RuleFor(c => c.UserId)
                .NotEmpty().WithMessage("El identificador del usuario es requerido.");

            RuleFor(c => c.Token)
                .NotEmpty().WithMessage("El token de activación es requerido.");
        }
    }

    public class ConfirmAccountCommandHandler : IRequestHandler<ConfirmAccountCommand, AccountOperationResult>
    {
        private readonly IAccountAuthService _accountService;

        public ConfirmAccountCommandHandler(IAccountAuthService accountService)
            => _accountService = accountService;

        public async Task<AccountOperationResult> Handle(ConfirmAccountCommand request,
            CancellationToken cancellationToken)
        {
            var (success, error) = await _accountService.ConfirmAccountAsync(request.UserId, request.Token);
            return success ? AccountOperationResult.Ok() : AccountOperationResult.Fail(error);
        }
    }

    public class GetResetTokenCommand : IRequest<GetResetTokenResult>
    {
        public string Email { get; set; } = string.Empty;
    }

    public class GetResetTokenResult
    {
        public bool Succeeded { get; set; }
        public string? Token { get; set; }
        public string? UserId { get; set; }
        public string? Error { get; set; }
    }

    public class GetResetTokenCommandValidator : AbstractValidator<GetResetTokenCommand>
    {
        public GetResetTokenCommandValidator()
        {
            RuleFor(c => c.Email)
                .NotEmpty().WithMessage("El correo es requerido.")
                .EmailAddress().WithMessage("El correo no tiene un formato válido.");
        }
    }

    public class GetResetTokenCommandHandler : IRequestHandler<GetResetTokenCommand, GetResetTokenResult>
    {
        private readonly IAccountAuthService _accountService;

        public GetResetTokenCommandHandler(IAccountAuthService accountService)
            => _accountService = accountService;

        public async Task<GetResetTokenResult> Handle(GetResetTokenCommand request,
            CancellationToken cancellationToken)
        {
            var (token, userId, error) = await _accountService.GetResetTokenAsync(request.Email);

            return new GetResetTokenResult
            {
                Succeeded = token is not null,
                Token = token,
                UserId = userId,
                Error = error
            };
        }
    }

    public class ResetPasswordCommand : IRequest<AccountOperationResult>
    {
        public string UserId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
    {
        public ResetPasswordCommandValidator()
        {
            RuleFor(c => c.UserId)
                .NotEmpty().WithMessage("El identificador del usuario es requerido.");

            RuleFor(c => c.Token)
                .NotEmpty().WithMessage("El token de restablecimiento es requerido.");

            RuleFor(c => c.NewPassword)
                .NotEmpty().WithMessage("La contraseña es requerida.")
                .MinimumLength(8).WithMessage("La contraseña debe tener al menos 8 caracteres.");

            RuleFor(c => c.ConfirmPassword)
                .Equal(c => c.NewPassword)
                .WithMessage("La contraseña y su confirmación no coinciden.");
        }
    }

    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, AccountOperationResult>
    {
        private readonly IAccountAuthService _accountService;

        public ResetPasswordCommandHandler(IAccountAuthService accountService)
            => _accountService = accountService;

        public async Task<AccountOperationResult> Handle(ResetPasswordCommand request,
            CancellationToken cancellationToken)
        {
            var (success, error) = await _accountService.ResetPasswordAsync(
                request.UserId, request.Token, request.NewPassword);

            return success ? AccountOperationResult.Ok() : AccountOperationResult.Fail(error);
        }
    }
}
