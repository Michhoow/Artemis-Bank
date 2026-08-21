using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Dtos.Transactions;
using ArtemisBank.Core.Application.ViewModels.Transactions;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using AutoMapper;

namespace ArtemisBank.Core.Application.Mappings
{
    public class TransactionProfile : Profile
    {
        public TransactionProfile()
        {
            CreateMap<Transaction, TransactionDto>()
                .ForMember(d => d.Date, o => o.MapFrom(s => s.CreatedAt))
                .ForMember(d => d.TransactionType, o => o.MapFrom(s => DisplayText.Type(s.Type)))
                .ForMember(d => d.Status, o => o.MapFrom(s => DisplayText.Status(s.Status)));

            CreateMap<Transaction, TransactionViewModel>()
                .ForMember(d => d.Date, o => o.MapFrom(s => s.CreatedAt))
                .ForMember(d => d.Type, o => o.MapFrom(s => DisplayText.Type(s.Type)))
                .ForMember(d => d.Status, o => o.MapFrom(s => DisplayText.Status(s.Status)))
                .ForMember(d => d.IsCredit, o => o.MapFrom(s => s.Type == TransactionType.Credito))
                .ForMember(d => d.IsApproved, o => o.MapFrom(s => s.Status == TransactionStatus.Aprobada));

            CreateMap<TransactionDto, TransactionViewModel>()
                .ForMember(d => d.Id, o => o.MapFrom(s => string.IsNullOrEmpty(s.Id) ? 0 : int.Parse(s.Id)))
                .ForMember(d => d.Type, o => o.MapFrom(s => s.TransactionType))
                .ForMember(d => d.IsCredit, o => o.MapFrom(s => s.TransactionType == DisplayText.Credit))
                .ForMember(d => d.IsApproved, o => o.MapFrom(s => s.Status == DisplayText.Approved));
        }
    }
}
