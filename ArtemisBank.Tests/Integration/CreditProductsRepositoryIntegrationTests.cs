using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Infrastructure.Persistence.Contexts;
using ArtemisBank.Infrastructure.Persistence.Repositories;
using ArtemisBank.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArtemisBank.Tests.Integration
{
    /// <summary>
    /// Integracion de los repositorios de productos de credito (Manuel) sobre SQLite en memoria:
    /// valida indices unicos, relaciones e includes con una base relacional real.
    /// </summary>
    public class CreditProductsRepositoryIntegrationTests : IDisposable
    {
        private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
        private readonly ArtemisDbContext _context;

        public CreditProductsRepositoryIntegrationTests()
        {
            var (context, connection) = TestContextFactory.CreateSqlite();
            _context = context;
            _connection = connection;
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }

        private static Loan Loan(string number, string clientId, decimal capital = 60000m)
            => new Loan
            {
                LoanNumber = number,
                ClientId = clientId,
                ApprovedCapital = capital,
                AnnualInterestRate = 12m,
                TermInMonths = 12,
                Status = LoanStatus.Activo,
                CreatedAt = DateTime.Now,
                Installments = new List<Installment>
                {
                    new()
                    {
                        InstallmentNumber = 1,
                        DueDate = DateTime.Now.AddMonths(1),
                        InstallmentAmount = 5000m,
                        InterestAmount = 600m,
                        CapitalAmount = 4400m,
                        PendingAmount = 5000m,
                        Status = InstallmentStatus.Pendiente,
                        CreatedAt = DateTime.Now
                    }
                }
            };

        private static CreditCard Card(string number, string clientId)
            => new CreditCard
            {
                CardNumber = number,
                ClientId = clientId,
                CreditLimit = 50000m,
                Debt = 0m,
                CvcHash = new string('A', 64),
                ExpirationDate = "12/29",
                Status = CreditCardStatus.Activa,
                CreatedAt = DateTime.Now
            };

        [Fact]
        public async Task Loan_PersisteConSusCuotas_YSeRecuperaConInclude()
        {
            var repository = new LoanRepository(_context);
            await repository.AddAsync(Loan("100000001", TestData.ClientOne));

            var found = await repository.GetByLoanNumberAsync("100000001");

            found.Should().NotBeNull();
            found!.Installments.Should().HaveCount(1);
        }

        [Fact]
        public async Task Loan_NumeroDuplicado_ViolaIndiceUnico()
        {
            var repository = new LoanRepository(_context);
            await repository.AddAsync(Loan("100000002", TestData.ClientOne));

            var act = async () => await repository.AddAsync(Loan("100000002", TestData.ClientTwo));

            await act.Should().ThrowAsync<DbUpdateException>();
        }

        [Fact]
        public async Task Loan_DeudaPendientePorCliente_SumaSoloActivos()
        {
            var repository = new LoanRepository(_context);
            await repository.AddAsync(Loan("100000003", TestData.ClientOne));

            var debt = await repository.GetActivePendingDebtByClientAsync(TestData.ClientOne);

            debt.Should().Be(5000m);
        }

        [Fact]
        public async Task Card_NumeroDuplicado_ViolaIndiceUnico()
        {
            var repository = new CreditCardRepository(_context);
            await repository.AddAsync(Card("4000000000000001", TestData.ClientOne));

            var act = async () => await repository.AddAsync(Card("4000000000000001", TestData.ClientTwo));

            await act.Should().ThrowAsync<DbUpdateException>();
        }

        [Fact]
        public async Task Card_ConsumoAprobado_SeConsultaPorComercio()
        {
            var cardRepo = new CreditCardRepository(_context);
            var card = await cardRepo.AddAsync(Card("4000000000000002", TestData.ClientOne));

            await cardRepo.AddConsumptionAsync(new Consumption
            {
                CreditCardId = card.Id,
                Amount = 1500m,
                CommerceName = "Comercio Demo",
                CommerceId = 7,
                Status = ConsumptionStatus.Aprobado,
                CreatedAt = DateTime.Now
            });

            var transactions = await cardRepo.QueryConsumptionsByCommerce(7).ToListAsync();

            transactions.Should().ContainSingle();
            transactions[0].Amount.Should().Be(1500m);
        }

        [Fact]
        public async Task Card_DeudaActivaPorCliente_SumaTarjetasActivas()
        {
            var cardRepo = new CreditCardRepository(_context);
            var card = Card("4000000000000003", TestData.ClientOne);
            card.Debt = 2500m;
            await cardRepo.AddAsync(card);

            var debt = await cardRepo.GetActiveDebtByClientAsync(TestData.ClientOne);

            debt.Should().Be(2500m);
        }
    }
}
