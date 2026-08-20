using ArtemisBank.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBank.Infrastructure.Persistence.EntityConfigurations
{
    public class InstallmentEntityConfiguration : IEntityTypeConfiguration<Installment>
    {
        public void Configure(EntityTypeBuilder<Installment> builder)
        {
            #region Configuracion basica
            builder.ToTable("Installments");
            builder.HasKey(i => i.Id);
            #endregion

            #region Propiedades
            builder.Property(i => i.InstallmentNumber).IsRequired();
            builder.Property(i => i.DueDate).IsRequired();

            builder.Property(i => i.InstallmentAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(i => i.InterestAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(i => i.CapitalAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(i => i.PendingAmount).HasPrecision(18, 2).IsRequired();

            builder.Property(i => i.Status).HasConversion<int>().IsRequired();
            builder.Property(i => i.IsLate).IsRequired();

            builder.Property(i => i.CreatedAt).IsRequired();
            builder.Property(i => i.CreatedByUserId).HasMaxLength(450);

            builder.Ignore(i => i.IsFullyPaid);
            #endregion

            #region Indices de consulta
            builder.HasIndex(i => new { i.LoanId, i.InstallmentNumber }).IsUnique();
            // Acelera el proceso diario de mora: cuotas por vencimiento y estado.
            builder.HasIndex(i => new { i.Status, i.DueDate });
            #endregion
        }
    }
}
