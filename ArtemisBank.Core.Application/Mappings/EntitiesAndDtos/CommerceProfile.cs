using ArtemisBank.Core.Application.Dtos.Commerces;
using ArtemisBank.Core.Domain.Entities;
using AutoMapper;

namespace ArtemisBank.Core.Application.Mappings.EntitiesAndDtos
{
    public class CommerceProfile : Profile
    {
        public CommerceProfile()
        {
            CreateMap<Commerce, CommerceDto>()
                .ForMember(d => d.PhoneNumber, o => o.MapFrom(s => s.Phone))
                .ForMember(d => d.HasAssociatedUser,
                    o => o.MapFrom(s => s.UserId != null && s.UserId != ""));

            CreateMap<Commerce, CommerceDetailDto>()
                .ForMember(d => d.PhoneNumber, o => o.MapFrom(s => s.Phone))
                .ForMember(d => d.HasAssociatedUser,
                    o => o.MapFrom(s => s.UserId != null && s.UserId != ""))

                .ForMember(d => d.AssociatedUser, o => o.Ignore());

            CreateMap<SaveCommerceDto, Commerce>()
                .ForMember(d => d.Phone, o => o.MapFrom(s => s.PhoneNumber))
                .ForMember(d => d.Id, o => o.Ignore())
                .ForMember(d => d.UserId, o => o.Ignore())
                .ForMember(d => d.AccountNumber, o => o.Ignore())

                .ForMember(d => d.IsActive, o => o.Ignore())
                .ForMember(d => d.CreatedAt, o => o.Ignore())
                .ForMember(d => d.CreatedByUserId, o => o.Ignore());
        }
    }
}
