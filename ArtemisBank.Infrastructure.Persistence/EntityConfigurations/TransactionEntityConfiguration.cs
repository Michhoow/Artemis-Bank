using ArtemisBank.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBank.Infrastructure.Persistence.EntityConfigurations
{
    public class TransactionEntityConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            #region Configuracion basica
            builder.ToTable("Transactions");
            builder.HasKey(t => t.Id);
            #endregion

            #region Propiedades
            builder.Property(t => t.Amount).HasPrecision(18, 2).IsRequired();

            builder.Property(t => t.Type).HasConversion<int>().IsRequired();
            builder.Property(t => t.Status).HasConversion<int>().IsRequired();
            builder.Property(t => t.Operation).HasConversion<int>().IsRequired();

            builder.Property(t => t.Origin).IsRequired().HasMaxLength(50);
            builder.Property(t => t.Beneficiary).IsRequired().HasMaxLength(50);

            builder.Property(t => t.RejectionReason).HasMaxLength(250);

            builder.Property(t => t.OperationReference).IsRequired().HasMaxLength(64).IsUnicode(false);

            builder.Property(t => t.PerformedByUserId).HasMaxLength(450);
            builder.Property(t => t.PerformedByRole).HasConversion<int?>();

            builder.Property(t => t.CreatedAt).IsRequired();
            builder.Property(t => t.CreatedByUserId).HasMaxLength(450);

            builder.Ignore(t => t.IsPayment);
            #endregion

            #region Relaciones
            builder.HasOne(t => t.SavingsAccount)
                .WithMany(a => a.Transactions)
                .HasForeignKey(t => t.SavingsAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            #endregion

            #region Indices de consulta

            builder.HasIndex(t => new { t.SavingsAccountId, t.CreatedAt });

            builder.HasIndex(t => t.OperationReference);
            builder.HasIndex(t => new { t.CreatedAt, t.Operation, t.Status });

            builder.HasIndex(t => new { t.PerformedByUserId, t.CreatedAt });
            #endregion
        }
    }
}
