using System.Reflection;
using ArtemisBank.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBank.Infrastructure.Persistence.Contexts
{
    public class ArtemisDbContext : DbContext
    {
        public ArtemisDbContext(DbContextOptions<ArtemisDbContext> options) : base(options) { }

        #region Cuentas y transacciones (Michael)
        public DbSet<SavingsAccount> SavingsAccounts => Set<SavingsAccount>();
        public DbSet<Transaction> Transactions => Set<Transaction>();
        public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
        #endregion

        #region Productos de credito y pagos (Manuel)
        public DbSet<Loan> Loans => Set<Loan>();
        public DbSet<LoanInstallment> LoanInstallments => Set<LoanInstallment>();
        public DbSet<CreditCard> CreditCards => Set<CreditCard>();
        public DbSet<CardConsumption> CardConsumptions => Set<CardConsumption>();
        #endregion

        #region Comercios (Monserrat)
        public DbSet<Commerce> Commerces => Set<Commerce>();
        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

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
