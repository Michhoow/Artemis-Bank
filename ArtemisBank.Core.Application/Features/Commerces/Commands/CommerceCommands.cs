using ArtemisBank.Core.Application.Dtos.Commerces;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.Commerces.Commands
{
    public class CreateCommerceCommand : IRequest<CommerceDto>
    {
        public string Name { get; set; } = string.Empty;
        public string Rnc { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? AdminUserId { get; set; }
    }

    public class CreateCommerceCommandValidator : AbstractValidator<CreateCommerceCommand>
    {
        public CreateCommerceCommandValidator()
        {
            RuleFor(c => c.Name)
                .NotEmpty().WithMessage("El nombre del comercio es requerido.")
                .MaximumLength(200);

            RuleFor(c => c.Rnc)
                .NotEmpty().WithMessage("El RNC es requerido.")
                .Matches(@"^\d{9,11}$").WithMessage("El RNC debe contener entre 9 y 11 dígitos.");

            RuleFor(c => c.Email)
                .NotEmpty().WithMessage("El correo es requerido.")
                .EmailAddress().WithMessage("El correo indicado no tiene un formato válido.");

            RuleFor(c => c.PhoneNumber)
                .NotEmpty().WithMessage("El teléfono es requerido.")
                .Matches(@"^\d{10}$").WithMessage("El teléfono debe contener 10 dígitos.");
        }
    }

    public class CreateCommerceCommandHandler : IRequestHandler<CreateCommerceCommand, CommerceDto>
    {
        private readonly ICommerceService _service;

        public CreateCommerceCommandHandler(ICommerceService service) => _service = service;

        public Task<CommerceDto> Handle(CreateCommerceCommand request, CancellationToken cancellationToken)
            => _service.CreateAsync(new SaveCommerceDto
            {
                Name = request.Name,
                Rnc = request.Rnc,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Description = request.Description
            }, request.AdminUserId, cancellationToken);
    }

    public class UpdateCommerceCommand : IRequest<CommerceDto>
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Rnc { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class UpdateCommerceCommandValidator : AbstractValidator<UpdateCommerceCommand>
    {
        public UpdateCommerceCommandValidator()
        {
            RuleFor(c => c.Id)
                .GreaterThan(0).WithMessage("El identificador del comercio debe ser mayor que cero.");

            RuleFor(c => c.Name)
                .NotEmpty().WithMessage("El nombre del comercio es requerido.")
                .MaximumLength(200);

            RuleFor(c => c.Rnc)
                .NotEmpty().WithMessage("El RNC es requerido.")
                .Matches(@"^\d{9,11}$").WithMessage("El RNC debe contener entre 9 y 11 dígitos.");

            RuleFor(c => c.Email)
                .NotEmpty().WithMessage("El correo es requerido.")
                .EmailAddress().WithMessage("El correo indicado no tiene un formato válido.");

            RuleFor(c => c.PhoneNumber)
                .NotEmpty().WithMessage("El teléfono es requerido.")
                .Matches(@"^\d{10}$").WithMessage("El teléfono debe contener 10 dígitos.");
        }
    }

    public class UpdateCommerceCommandHandler : IRequestHandler<UpdateCommerceCommand, CommerceDto>
    {
        private readonly ICommerceService _service;

        public UpdateCommerceCommandHandler(ICommerceService service) => _service = service;

        public Task<CommerceDto> Handle(UpdateCommerceCommand request, CancellationToken cancellationToken)
            => _service.UpdateAsync(request.Id, new SaveCommerceDto
            {
                Name = request.Name,
                Rnc = request.Rnc,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Description = request.Description
            }, cancellationToken);
    }

    public class SetCommerceStatusCommand : IRequest<CommerceDto>
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
        public string? AdminUserId { get; set; }
    }

    public class SetCommerceStatusCommandValidator : AbstractValidator<SetCommerceStatusCommand>
    {
        public SetCommerceStatusCommandValidator()
        {
            RuleFor(c => c.Id)
                .GreaterThan(0).WithMessage("El identificador del comercio debe ser mayor que cero.");
        }
    }

    public class SetCommerceStatusCommandHandler : IRequestHandler<SetCommerceStatusCommand, CommerceDto>
    {
        private readonly ICommerceService _service;

        public SetCommerceStatusCommandHandler(ICommerceService service) => _service = service;

        public Task<CommerceDto> Handle(SetCommerceStatusCommand request, CancellationToken cancellationToken)
            => _service.SetStatusAsync(request.Id, request.IsActive, request.AdminUserId, cancellationToken);
    }
}
