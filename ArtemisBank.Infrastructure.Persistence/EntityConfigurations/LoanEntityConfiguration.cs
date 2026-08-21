using ArtemisBank.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBank.Infrastructure.Persistence.EntityConfigurations
{
    public class LoanEntityConfiguration : IEntityTypeConfiguration<Loan>
    {
        public void Configure(EntityTypeBuilder<Loan> builder)
        {
            #region Configuracion basica
            builder.ToTable("Loans");
            builder.HasKey(l => l.Id);
            #endregion

            #region Propiedades

            builder.Property(l => l.LoanNumber)
                .IsRequired()
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength();

            builder.HasIndex(l => l.LoanNumber).IsUnique();

            builder.Property(l => l.ClientId).IsRequired().HasMaxLength(450);
            builder.HasIndex(l => l.ClientId);

            builder.Property(l => l.ApprovedCapital).HasPrecision(18, 2).IsRequired();
            builder.Property(l => l.AnnualInterestRate).HasPrecision(9, 4).IsRequired();
            builder.Property(l => l.MonthlyInstallment).HasPrecision(18, 2).IsRequired();
            builder.Property(l => l.TotalToPay).HasPrecision(18, 2).IsRequired();
            builder.Property(l => l.PaidAmount).HasPrecision(18, 2).IsRequired();

            builder.Property(l => l.TermInMonths).IsRequired();
            builder.Property(l => l.Status).HasConversion<int>().IsRequired();

            builder.Property(l => l.DisbursementAccountNumber)
                .HasMaxLength(9).IsUnicode(false);

            builder.Property(l => l.FirstDueDate).IsRequired();
            builder.Property(l => l.CreatedAt).IsRequired();
            builder.Property(l => l.CreatedByUserId).HasMaxLength(450);

            builder.Ignore(l => l.IsActive);
            builder.Ignore(l => l.PendingAmount);
            builder.Ignore(l => l.TotalInstallments);
            builder.Ignore(l => l.PaidInstallments);
            builder.Ignore(l => l.IsOverdue);
            #endregion

            #region Relaciones
            builder.HasMany(l => l.Installments)
                .WithOne(i => i.Loan)
                .HasForeignKey(i => i.LoanId)

                .OnDelete(DeleteBehavior.Cascade);
            #endregion

            #region Indices de consulta

            builder.HasIndex(l => new { l.ClientId, l.Status });
            builder.HasIndex(l => l.CreatedAt);
            #endregion
        }
    }
}
