using ArtemisBank.Core.Application.Dtos.CreditCards;
using ArtemisBank.Core.Application.Dtos.Loans;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArtemisBank.Tests.Integration
{
    public class LoanAndCardIntegrationTests
    {
        [Fact]
        public async Task ElNumeroDePrestamoEsUnicoEnLaBaseDeDatos()
        {
            var (context, connection) = TestContextFactory.CreateSqlite();
            using (connection)
            using (context)
            {
                context.Loans.Add(NewLoan("500000001", TestData.ClientOne));
                await context.SaveChangesAsync();

                context.Loans.Add(NewLoan("500000001", TestData.ClientTwo));

                var act = () => context.SaveChangesAsync();
                await act.Should().ThrowAsync<DbUpdateException>();
            }
        }

        [Fact]
        public async Task ElNumeroDeTarjetaEsUnicoEnLaBaseDeDatos()
        {
            var (context, connection) = TestContextFactory.CreateSqlite();
            using (connection)
            using (context)
            {
                context.CreditCards.Add(NewCard("4000000000000001", TestData.ClientOne));
                await context.SaveChangesAsync();

                context.CreditCards.Add(NewCard("4000000000000001", TestData.ClientTwo));

                var act = () => context.SaveChangesAsync();
                await act.Should().ThrowAsync<DbUpdateException>();
            }
        }

        [Fact]
        public async Task ElRncDelComercioEsUnicoEnLaBaseDeDatos()
        {
            var (context, connection) = TestContextFactory.CreateSqlite();
            using (connection)
            using (context)
            {
                context.Commerces.Add(NewCommerce("130123456", "uno@artemisbank.do"));
                await context.SaveChangesAsync();

                context.Commerces.Add(NewCommerce("130123456", "dos@artemisbank.do"));

                var act = () => context.SaveChangesAsync();
                await act.Should().ThrowAsync<DbUpdateException>();
            }
        }

        [Fact]
        public async Task AsignarUnPrestamo_PersisteElPrestamoYSusCuotas()
        {
            var (context, connection) = TestContextFactory.CreateSqlite();
            using (connection)
            using (context)
            {
                await TestData.SeedAsync(context);
                var builder = new ModuleBuilder(context);

                var loan = await builder.LoanService().CreateAsync(new CreateLoanDto
                {
                    ClientId = TestData.ClientOne,
                    CapitalAmount = 100000m,
                    TermInMonths = 12,
                    AnnualInterestRate = 12m,
                    ConfirmHighRisk = true
                }, TestData.AdminId);

                var stored = await context.Loans
                    .Include(l => l.Installments)
                    .FirstAsync(l => l.Id == loan.Id);

                stored.Installments.Should().HaveCount(12);
                stored.LoanNumber.Should().HaveLength(9);
                stored.Status.Should().Be(LoanStatus.Activo);
            }
        }

        [Fact]
        public async Task AsignarUnPrestamo_EsAtomicoConElDesembolso()
        {
            var (context, connection) = TestContextFactory.CreateSqlite();
            using (connection)
            using (context)
            {
                var (principal, _, _) = await TestData.SeedAsync(context);
                var balanceBefore = principal.Balance;
                var builder = new ModuleBuilder(context);

                await builder.LoanService().CreateAsync(new CreateLoanDto
                {
                    ClientId = TestData.ClientOne,
                    CapitalAmount = 75000m,
                    TermInMonths = 12,
                    AnnualInterestRate = 12m,
                    ConfirmHighRisk = true
                }, TestData.AdminId);

                (await context.Loans.CountAsync()).Should().Be(1);
                (await context.LoanInstallments.CountAsync()).Should().Be(12);

                var updated = await context.SavingsAccounts.FirstAsync(a => a.Id == principal.Id);
                updated.Balance.Should().Be(balanceBefore + 75000m);

                (await context.Transactions.CountAsync(t =>
                    t.Operation == TransactionOperation.DesembolsoPrestamo)).Should().Be(1);
            }
        }

        [Fact]
        public async Task ElAvanceDeEfectivo_PersisteConsumosYBalanceDeFormaConsistente()
        {
            var (context, connection) = TestContextFactory.CreateSqlite();
            using (connection)
            using (context)
            {
                var (principal, _, _) = await TestData.SeedAsync(context);
                var balanceBefore = principal.Balance;
                var builder = new ModuleBuilder(context);

                var card = await builder.CardService().CreateAsync(new CreateCreditCardDto
                {
                    ClientId = TestData.ClientOne,
                    CreditLimit = 50000m
                }, TestData.AdminId);

                await builder.CardService().CashAdvanceAsync(new CashAdvanceRequestDto
                {
                    CreditCardId = card.Card.Id,
                    TargetAccountNumber = principal.AccountNumber,
                    Amount = 8000m
                }, TestData.ClientOne);

                var updatedAccount = await context.SavingsAccounts.FirstAsync(a => a.Id == principal.Id);
                var updatedCard = await context.CreditCards.FirstAsync();

                updatedAccount.Balance.Should().Be(balanceBefore + 8000m);
                updatedCard.Debt.Should().Be(8500.00m);
                (await context.CardConsumptions.CountAsync()).Should().Be(2);
            }
        }

        [Fact]
        public async Task LasCuotasSeEliminanEnCascadaConSuPrestamo()
        {
            var (context, connection) = TestContextFactory.CreateSqlite();
            using (connection)
            using (context)
            {
                var loan = NewLoan("500000009", TestData.ClientOne);
                context.Loans.Add(loan);
                await context.SaveChangesAsync();

                context.LoanInstallments.Add(new LoanInstallment
                {
                    LoanId = loan.Id,
                    Number = 1,
                    DueDate = DateTime.Now.AddMonths(1),
                    TotalAmount = 10000m,
                    CapitalAmount = 9000m,
                    InterestAmount = 1000m,
                    Status = InstallmentStatus.Pendiente
                });
                await context.SaveChangesAsync();

                context.Loans.Remove(loan);
                await context.SaveChangesAsync();

                (await context.LoanInstallments.CountAsync()).Should().Be(0);
            }
        }

        [Fact]
        public async Task LosImportesConservanDosDecimalesAlPersistirse()
        {
            var (context, connection) = TestContextFactory.CreateSqlite();
            using (connection)
            using (context)
            {
                var loan = NewLoan("500000010", TestData.ClientOne);
                loan.ApprovedCapital = 12345.67m;
                loan.MonthlyInstallment = 1097.06m;
                context.Loans.Add(loan);
                await context.SaveChangesAsync();

                context.ChangeTracker.Clear();

                var stored = await context.Loans.FirstAsync();
                stored.ApprovedCapital.Should().Be(12345.67m);
                stored.MonthlyInstallment.Should().Be(1097.06m);
            }
        }

        [Fact]
        public async Task ElNumeroDePrestamoSePersisteComoTextoConCerosALaIzquierda()
        {
            var (context, connection) = TestContextFactory.CreateSqlite();
            using (connection)
            using (context)
            {
                context.Loans.Add(NewLoan("000000123", TestData.ClientOne));
                await context.SaveChangesAsync();

                context.ChangeTracker.Clear();

                (await context.Loans.FirstAsync()).LoanNumber.Should().Be("000000123");
            }
        }

        private static Loan NewLoan(string number, string clientId) => new Loan
        {
            LoanNumber = number,
            ClientId = clientId,
            ApprovedCapital = 100000m,
            AnnualInterestRate = 12m,
            TermInMonths = 12,
            MonthlyInstallment = 8884.88m,
            Status = LoanStatus.Activo,
            CreatedAt = DateTime.Now
        };

        private static CreditCard NewCard(string number, string clientId) => new CreditCard
        {
            CardNumber = number,
            ClientId = clientId,
            CreditLimit = 50000m,
            Debt = 0m,
            CvcHash = "hash-de-prueba",
            CvcSalt = "sal-de-prueba",
            ExpirationMonth = DateTime.Now.Month,
            ExpirationYear = DateTime.Now.Year + 3,
            Status = CardStatus.Activa,
            CreatedAt = DateTime.Now
        };

        private static Commerce NewCommerce(string rnc, string email) => new Commerce
        {
            Name = "Comercio de prueba",
            Rnc = rnc,
            Email = email,
            Phone = "8095551234",
            UserId = string.Empty,
            AccountNumber = string.Empty,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
    }
}
