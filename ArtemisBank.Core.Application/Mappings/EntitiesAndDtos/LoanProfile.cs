using ArtemisBank.Core.Application.Dtos.Loans;
using ArtemisBank.Core.Domain.Entities;
using AutoMapper;

namespace ArtemisBank.Core.Application.Mappings.EntitiesAndDtos
{
    public class LoanProfile : Profile
    {
        public LoanProfile()
        {
            CreateMap<Loan, LoanDto>()
                .ForMember(d => d.CapitalAmount, o => o.MapFrom(s => s.ApprovedCapital))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.PendingAmount, o => o.MapFrom(s => s.PendingAmount))
                .ForMember(d => d.TotalInstallments, o => o.MapFrom(s => s.TotalInstallments))
                .ForMember(d => d.PaidInstallments, o => o.MapFrom(s => s.PaidInstallments))

                .ForMember(d => d.ClientFullName, o => o.Ignore())
                .ForMember(d => d.ClientIdentification, o => o.Ignore())
                .ForMember(d => d.ClientPaymentStatus, o => o.Ignore())
                .ReverseMap()
                .ForMember(d => d.ApprovedCapital, o => o.MapFrom(s => s.CapitalAmount))
                .ForMember(d => d.Status, o => o.Ignore())
                .ForMember(d => d.Installments, o => o.Ignore());

            CreateMap<LoanInstallment, LoanInstallmentDto>()
                .ForMember(d => d.PaymentStatus, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.PendingAmount, o => o.MapFrom(s => s.PendingAmount));
        }
    }
}
