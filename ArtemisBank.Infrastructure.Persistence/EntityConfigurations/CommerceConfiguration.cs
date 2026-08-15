using ArtemisBank.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ArtemisBank.Infrastructure.Persistence.EntityConfigurations
{
    /// <summary>
    /// Configuración EF Core para la entidad <see cref="Commerce"/>.
    /// RNC y Email tienen índices de unicidad.
    /// </summary>
    public class CommerceConfiguration : IEntityTypeConfiguration<Commerce>
    {
        public void Configure(EntityTypeBuilder<Commerce> builder)
        {
            builder.ToTable("Commerces");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(c => c.Rnc)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(c => c.Email)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(c => c.Phone)
                .HasMaxLength(20);

            builder.Property(c => c.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(c => c.AccountNumber)
                .IsRequired()
                .HasMaxLength(9);

            builder.HasIndex(c => c.Rnc)
                .IsUnique()
                .HasDatabaseName("IX_Commerces_Rnc");

            builder.HasIndex(c => c.Email)
                .IsUnique()
                .HasDatabaseName("IX_Commerces_Email");
        }
    }
}
