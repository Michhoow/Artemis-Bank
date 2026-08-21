using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Domain.Entities;
using AutoMapper;

namespace ArtemisBank.Core.Application.Mappings.EntitiesAndDtos
{
    public class CreditCardProfile : Profile
    {
        public CreditCardProfile()
        {
            CreateMap<CreditCard, CreditCardDto>()
                .ForMember(d => d.MaskedNumber, o => o.MapFrom(s => Money.MaskCard(s.CardNumber)))
                .ForMember(d => d.LastFourDigits, o => o.MapFrom(s => s.LastFourDigits))
                .ForMember(d => d.ExpirationDate, o => o.MapFrom(s => s.ExpirationDisplay))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()))
                .ForMember(d => d.AvailableCredit, o => o.MapFrom(s => s.AvailableCredit))
                .ForMember(d => d.IsActive, o => o.MapFrom(s => s.IsActive))

                .ForMember(d => d.ClientFullName, o => o.Ignore())
                .ForMember(d => d.ClientIdentification, o => o.Ignore());

            CreateMap<CardConsumption, CardConsumptionDto>()
                .ForMember(d => d.Type, o => o.MapFrom(s => s.Type.ToString()))
                .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));
        }
    }
}
