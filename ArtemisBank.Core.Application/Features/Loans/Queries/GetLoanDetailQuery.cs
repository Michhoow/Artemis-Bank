using ArtemisBank.Core.Application.Dtos.Loans;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.Loans.Queries
{
    /// <summary>GET /api/loan/{id} — detalle de un prestamo con su tabla de amortizacion.</summary>
    public class GetLoanDetailQuery : IRequest<LoanDetailDto?>
    {
        public int Id { get; set; }
    }

    public class GetLoanDetailQueryValidator : AbstractValidator<GetLoanDetailQuery>
    {
        public GetLoanDetailQueryValidator()
        {
            RuleFor(q => q.Id).GreaterThan(0).WithMessage("El identificador del préstamo no es válido.");
        }
    }

    public class GetLoanDetailQueryHandler : IRequestHandler<GetLoanDetailQuery, LoanDetailDto?>
    {
        private readonly ILoanService _service;

        public GetLoanDetailQueryHandler(ILoanService service) => _service = service;

        public Task<LoanDetailDto?> Handle(GetLoanDetailQuery request, CancellationToken cancellationToken)
            => _service.GetDetailAsync(request.Id, cancellationToken);
    }
}
