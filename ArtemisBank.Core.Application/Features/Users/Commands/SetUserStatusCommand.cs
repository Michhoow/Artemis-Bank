using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.Users.Commands
{
    public class SetUserStatusCommand : IRequest<UserOperationResult>
    {
        public string TargetUserId { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public string? CallerUserId { get; set; }
    }

    public class SetUserStatusCommandValidator : AbstractValidator<SetUserStatusCommand>
    {
        public SetUserStatusCommandValidator()
        {
            RuleFor(c => c.TargetUserId)
                .NotEmpty().WithMessage("El identificador del usuario es requerido.");
        }
    }

    public class SetUserStatusCommandHandler : IRequestHandler<SetUserStatusCommand, UserOperationResult>
    {
        private readonly IUserManagementService _service;

        public SetUserStatusCommandHandler(IUserManagementService service) => _service = service;

        public async Task<UserOperationResult> Handle(SetUserStatusCommand request,
            CancellationToken cancellationToken)
        {
            var result = await _service.SetActiveAsync(request.TargetUserId, request.IsActive,
                request.CallerUserId, cancellationToken);

            if (!result.Succeeded)
            {
                if (result.Error == AppMessages.UserNotFound)
                    throw new NotFoundException(result.Error);

                if (result.Error == AppMessages.UserCannotModifySelf)
                    throw new ForbiddenException(result.Error);
            }

            return result;
        }
    }
}
