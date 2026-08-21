using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Dtos.SavingsAccounts;
using ArtemisBank.Core.Application.ViewModels.SavingsAccounts;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using AutoMapper;

namespace ArtemisBank.Core.Application.Mappings
{
    public class SavingsAccountProfile : Profile
    {
        public SavingsAccountProfile()
        {
            CreateMap<SavingsAccount, SavingsAccountDto>()
                .ForMember(d => d.Type, o => o.MapFrom(s => DisplayText.AccountType(s.Type)))
                .ForMember(d => d.Status, o => o.MapFrom(s => DisplayText.AccountStatus(s.Status)))

                .ForMember(d => d.ClientFullName, o => o.Ignore())
                .ForMember(d => d.Identification, o => o.Ignore());

            CreateMap<SavingsAccount, SavingsAccountViewModel>()
                .ForMember(d => d.Type, o => o.MapFrom(s => DisplayText.AccountType(s.Type)))
                .ForMember(d => d.Status, o => o.MapFrom(s => DisplayText.AccountStatus(s.Status)))
                .ForMember(d => d.IsPrincipal, o => o.MapFrom(s => s.Type == AccountType.Principal))
                .ForMember(d => d.IsActive, o => o.MapFrom(s => s.Status == AccountStatus.Activa))
                .ForMember(d => d.ClientFullName, o => o.Ignore())
                .ForMember(d => d.Identification, o => o.Ignore());

            CreateMap<SavingsAccountDto, SavingsAccountViewModel>()
                .ForMember(d => d.Id, o => o.MapFrom(s => string.IsNullOrEmpty(s.Id) ? 0 : int.Parse(s.Id)))
                .ForMember(d => d.IsPrincipal, o => o.MapFrom(s => s.Type == "Principal"))
                .ForMember(d => d.IsActive, o => o.MapFrom(s => s.Status == "Activa"));

            CreateMap<SavingsAccountDto, ViewModels.Transactions.AccountOptionViewModel>();
        }
    }
}
