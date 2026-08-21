using ArtemisBank.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBank.Infrastructure.Persistence.EntityConfigurations
{
    public class CardConsumptionEntityConfiguration : IEntityTypeConfiguration<CardConsumption>
    {
        public void Configure(EntityTypeBuilder<CardConsumption> builder)
        {
            #region Configuracion basica
            builder.ToTable("CardConsumptions");
            builder.HasKey(k => k.Id);
            #endregion

            #region Propiedades
            builder.Property(k => k.CreditCardId).IsRequired();
            builder.Property(k => k.Amount).HasPrecision(18, 2).IsRequired();

            builder.Property(k => k.Type).HasConversion<int>().IsRequired();
            builder.Property(k => k.Status).HasConversion<int>().IsRequired();

            builder.Property(k => k.Description).IsRequired().HasMaxLength(200);
            builder.Property(k => k.CommerceName).HasMaxLength(200);
            builder.Property(k => k.RejectionReason).HasMaxLength(200);
            builder.Property(k => k.OperationReference).HasMaxLength(64).IsUnicode(false);

            builder.Property(k => k.CreatedAt).IsRequired();
            builder.Property(k => k.CreatedByUserId).HasMaxLength(450);

            builder.Ignore(k => k.IsApproved);
            #endregion

            #region Indices de consulta

            builder.HasIndex(k => new { k.CreditCardId, k.CreatedAt });
            builder.HasIndex(k => k.OperationReference);
            #endregion
        }
    }
}
