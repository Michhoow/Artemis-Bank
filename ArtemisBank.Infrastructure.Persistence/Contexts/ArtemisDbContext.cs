using System.Reflection;
using ArtemisBank.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBank.Infrastructure.Persistence.Contexts
{
    /// <summary>
    /// Punto unico de verdad del esquema de negocio (Code First).
    ///
    /// PROPIEDAD: Michael. Es el archivo con mayor riesgo de conflicto de Git del proyecto,
    /// por eso NADIE mas lo edita directamente.
    ///
    /// COMO AGREGAR UNA ENTIDAD (Monserrat / Manuel):
    ///   1. Cree su entidad en ArtemisBank.Core.Domain/Entities.
    ///   2. Cree su IEntityTypeConfiguration en Persistence/EntityConfigurations.
    ///      ApplyConfigurationsFromAssembly la toma automaticamente: no hay que registrarla aqui.
    ///   3. Pida a Michael que agregue el DbSet en la region correspondiente y genere la migracion.
    ///
    /// Las migraciones las genera SIEMPRE Michael desde ArtemisBank.WebApp.
    /// </summary>
    public class ArtemisDbContext : DbContext
    {
        public ArtemisDbContext(DbContextOptions<ArtemisDbContext> options) : base(options) { }

        #region Cuentas y transacciones (Michael)
        public DbSet<SavingsAccount> SavingsAccounts => Set<SavingsAccount>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
        #endregion

        #region Productos de credito y pagos (Manuel)
        // public DbSet<Loan> Loans => Set<Loan>();
        // public DbSet<Installment> Installments => Set<Installment>();
        // public DbSet<CreditCard> CreditCards => Set<CreditCard>();
        // public DbSet<Consumption> Consumptions => Set<Consumption>();
        #endregion

        #region Comercios (Monserrat)
        // public DbSet<Commerce> Commerces => Set<Commerce>();
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Toma todas las IEntityTypeConfiguration del ensamblado, sin importar de quien sean.
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            // Red de seguridad: todo decimal del modelo queda en decimal(18,2).
            // Evita que un montoterminado en float o sin precision se cuele en una migracion.
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                    {
                        if (property.GetPrecision() == null) property.SetPrecision(18);
                        if (property.GetScale() == null) property.SetScale(2);
                    }
                }
            }
        }
    }
}
