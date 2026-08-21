using ArtemisBank.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBank.Infrastructure.Persistence.EntityConfigurations
{
    public class LoanInstallmentEntityConfiguration : IEntityTypeConfiguration<LoanInstallment>
    {
        public void Configure(EntityTypeBuilder<LoanInstallment> builder)
        {
            #region Configuracion basica
            builder.ToTable("LoanInstallments");
            builder.HasKey(i => i.Id);
            #endregion

            #region Propiedades
            builder.Property(i => i.LoanId).IsRequired();
            builder.Property(i => i.Number).IsRequired();
            builder.Property(i => i.DueDate).IsRequired();

            builder.Property(i => i.CapitalAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(i => i.InterestAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(i => i.TotalAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(i => i.PaidAmount).HasPrecision(18, 2).IsRequired();
            builder.Property(i => i.RemainingCapital).HasPrecision(18, 2).IsRequired();

            builder.Property(i => i.Status).HasConversion<int>().IsRequired();
            builder.Property(i => i.IsOverdue).IsRequired();
            builder.Property(i => i.CreatedAt).IsRequired();
            builder.Property(i => i.CreatedByUserId).HasMaxLength(450);

            builder.Ignore(i => i.PendingAmount);
            builder.Ignore(i => i.IsSettled);
            #endregion

            #region Indices de consulta

            builder.HasIndex(i => new { i.LoanId, i.Number }).IsUnique();

            builder.HasIndex(i => new { i.IsOverdue, i.DueDate });
            #endregion
        }
    }
}
