using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.ViewModels.Client;
using ArtemisBank.Core.Application.ViewModels.Transactions;
using AutoMapper;

namespace ArtemisBank.Core.Application.Mappings
{
    public class ClientProfile : Profile
    {
        public ClientProfile()
        {
            CreateMap<LoanInfoDto, ClientLoanViewModel>()
                .ForMember(d => d.Status, o => o.MapFrom(s => s.IsOverdue ? "En mora" : "Al día"));

            CreateMap<CreditCardInfoDto, ClientCreditCardViewModel>()
                .ForMember(d => d.MaskedNumber, o => o.MapFrom(s => s.MaskedNumber));

            CreateMap<LoanInfoDto, LoanOptionViewModel>();

            CreateMap<CreditCardInfoDto, CreditCardOptionViewModel>()
                .ForMember(d => d.MaskedNumber, o => o.MapFrom(s => s.MaskedNumber));
        }
    }
}
