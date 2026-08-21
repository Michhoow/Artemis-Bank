using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Dtos.Loans;
using ArtemisBank.Core.Application.Interfaces;
using FluentValidation;
using MediatR;

namespace ArtemisBank.Core.Application.Features.Loans.Queries
{
    public class GetLoanByIdQuery : IRequest<LoanDetailDto>
    {
        public int Id { get; set; }
    }

    public class GetLoanByIdQueryValidator : AbstractValidator<GetLoanByIdQuery>
    {
        public GetLoanByIdQueryValidator()
        {
            RuleFor(q => q.Id)
                .GreaterThan(0).WithMessage("El identificador del préstamo debe ser mayor que cero.");
        }
    }

    public class GetLoanByIdQueryHandler : IRequestHandler<GetLoanByIdQuery, LoanDetailDto>
    {
        private readonly ILoanService _service;

        public GetLoanByIdQueryHandler(ILoanService service) => _service = service;

        public async Task<LoanDetailDto> Handle(GetLoanByIdQuery request, CancellationToken cancellationToken)
            => await _service.GetDetailAsync(request.Id, cancellationToken)
               ?? throw new NotFoundException(AppMessages.LoanNotFound);
    }
}
