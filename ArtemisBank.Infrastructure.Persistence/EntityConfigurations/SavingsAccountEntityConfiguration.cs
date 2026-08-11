using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBank.Infrastructure.Persistence.EntityConfigurations
{
    public class SavingsAccountEntityConfiguration : IEntityTypeConfiguration<SavingsAccount>
    {
        public void Configure(EntityTypeBuilder<SavingsAccount> builder)
        {
            #region Configuracion basica
            builder.ToTable("SavingsAccounts");
            builder.HasKey(a => a.Id);
            #endregion

            #region Propiedades
            // Numero de 9 digitos almacenado como TEXTO: no se pueden perder ceros a la izquierda.
            builder.Property(a => a.AccountNumber)
                .IsRequired()
                .HasMaxLength(9)
                .IsUnicode(false)
                .IsFixedLength();

            builder.HasIndex(a => a.AccountNumber).IsUnique();

            builder.Property(a => a.ClientId).IsRequired().HasMaxLength(450);
            builder.HasIndex(a => a.ClientId);

            builder.Property(a => a.Balance).HasPrecision(18, 2).IsRequired();

            builder.Property(a => a.Type).HasConversion<int>().IsRequired();
            builder.Property(a => a.Status).HasConversion<int>().IsRequired();

            builder.Property(a => a.CreatedAt).IsRequired();
            builder.Property(a => a.CreatedByUserId).HasMaxLength(450);
            builder.Property(a => a.CancelledByUserId).HasMaxLength(450);

            // Propiedades calculadas: no se mapean.
            builder.Ignore(a => a.IsActive);
            builder.Ignore(a => a.IsPrincipal);
            builder.Ignore(a => a.LastFourDigits);
            #endregion

            #region Relaciones
            builder.HasMany(a => a.Transactions)
                .WithOne(t => t.SavingsAccount)
                .HasForeignKey(t => t.SavingsAccountId)
                // El historial NUNCA se elimina, ni siquiera al cancelar la cuenta.
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(a => a.RegisteredAsBeneficiary)
                .WithOne(b => b.SavingsAccount)
                .HasForeignKey(b => b.SavingsAccountId)
                .OnDelete(DeleteBehavior.Restrict);
            #endregion

            #region Indices de consulta
            // Cubre el listado del administrador y el Home del cliente.
            builder.HasIndex(a => new { a.ClientId, a.Status, a.Type });
            builder.HasIndex(a => a.CreatedAt);
            #endregion
        }
    }
}
