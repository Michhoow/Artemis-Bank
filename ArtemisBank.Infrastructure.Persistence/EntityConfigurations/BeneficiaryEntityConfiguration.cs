using ArtemisBank.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBank.Infrastructure.Persistence.EntityConfigurations
{
    public class BeneficiaryEntityConfiguration : IEntityTypeConfiguration<Beneficiary>
    {
        public void Configure(EntityTypeBuilder<Beneficiary> builder)
        {
            #region Configuracion basica
            builder.ToTable("Beneficiaries");
            builder.HasKey(b => b.Id);
            #endregion

            #region Propiedades
            builder.Property(b => b.ClientId).IsRequired().HasMaxLength(450);
            builder.Property(b => b.CreatedAt).IsRequired();
            builder.Property(b => b.CreatedByUserId).HasMaxLength(450);
            #endregion

            #region Relaciones
            builder.HasOne(b => b.SavingsAccount)
                .WithMany(a => a.RegisteredAsBeneficiary)
                .HasForeignKey(b => b.SavingsAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            #endregion

            #region Indices

            builder.HasIndex(b => new { b.ClientId, b.SavingsAccountId }).IsUnique();
            #endregion
        }
    }
}
