using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Infrastructure.Persistence.Repositories;
using ArtemisBank.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ArtemisBank.Tests.Integration
{
    /// <summary>
    /// Pruebas de integracion sobre SQLite en memoria: base relacional de verdad,
    /// sin depender de SQL Server ni de ninguna instalacion externa.
    /// </summary>
    public class RepositoryIntegrationTests : IDisposable
    {
        private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
        private readonly ArtemisBank.Infrastructure.Persistence.Contexts.ArtemisDbContext _context;

        public RepositoryIntegrationTests()
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

        [Fact]
        public async Task Repositorio_PersisteYRecuperaCuentaPorNumero()
        {
            var repository = new SavingsAccountRepository(_context);
            await repository.AddAsync(TestData.Account("123456789", TestData.ClientOne, 100m));

            var found = await repository.GetByAccountNumberAsync("123456789");

            found.Should().NotBeNull();
            found!.Balance.Should().Be(100m);
        }

        [Fact]
        public async Task Repositorio_NumeroDeCuentaDuplicado_ViolaElIndiceUnico()
        {
            var repository = new SavingsAccountRepository(_context);
            await repository.AddAsync(TestData.Account("123456789", TestData.ClientOne, 100m));

            var act = async () => await repository.AddAsync(
                TestData.Account("123456789", TestData.ClientTwo, 50m));

            await act.Should().ThrowAsync<DbUpdateException>();
        }

        [Fact]
        public async Task Repositorio_ConservaCerosALaIzquierdaDelNumeroDeCuenta()
        {
            var repository = new SavingsAccountRepository(_context);
            await repository.AddAsync(TestData.Account("000123456", TestData.ClientOne, 10m));

            var found = await repository.GetByAccountNumberAsync("000123456");

            found.Should().NotBeNull();
            found!.AccountNumber.Should().Be("000123456");
        }

        [Fact]
        public async Task Repositorio_MontosConservanPrecisionDeCentavos()
        {
            var repository = new SavingsAccountRepository(_context);
            await repository.AddAsync(TestData.Account("123456780", TestData.ClientOne, 1234.56m));

            var found = await repository.GetByAccountNumberAsync("123456780");

            found!.Balance.Should().Be(1234.56m);
        }

        [Fact]
        public async Task Repositorio_PersisteTransaccionesFinancierasConSuTrazabilidad()
        {
            var builder = new ServiceBuilder(_context);
            var (principal, _, other) = await TestData.SeedAsync(_context);

            await builder.TransactionService().TransferAsync(new TransferRequest
            {
                SourceAccountNumber = principal.AccountNumber,
                TargetAccountNumber = other.AccountNumber,
                Amount = 400m,
                Operation = TransactionOperation.TransaccionTerceros,
                Actor = OperationActor.Of(TestData.CashierId, Roles.Cajero)
            });

            var movements = await _context.Transactions.AsNoTracking().ToListAsync();

            movements.Should().HaveCount(2);
            movements.Should().OnlyContain(m => m.PerformedByUserId == TestData.CashierId);
            movements.Should().OnlyContain(m => m.PerformedByRole == Roles.Cajero);
            movements.Select(m => m.OperationReference).Distinct().Should().ContainSingle();
        }

        [Fact]
        public async Task OperacionTransaccional_SiFallaUnaPata_NoSeAplicaNingunaParcialmente()
        {
            var builder = new ServiceBuilder(_context);
            var (principal, _, other) = await TestData.SeedAsync(_context);

            var originalSource = principal.Balance;
            var originalTarget = other.Balance;

            // Se fuerza el fallo desde el modulo de tarjetas justo despues del debito.
            builder.Cards.Setup(c => c.GetByIdAsync(9)).ReturnsAsync(
                new ArtemisBank.Core.Application.Dtos.Common.CreditCardInfoDto
                {
                    Id = 9, ClientId = TestData.ClientOne, LastFourDigits = "9999",
                    Debt = 1000m, IsActive = true
                });

            builder.Cards.Setup(c => c.ApplyPaymentAsync(9, It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new InvalidOperationException("fallo simulado del módulo de tarjetas"));

            var act = async () => await builder.TransactionService().PayCreditCardAsync(new CardPaymentRequest
            {
                SourceAccountNumber = principal.AccountNumber,
                CreditCardId = 9,
                Amount = 500m,
                Actor = OperationActor.Of(TestData.ClientOne, Roles.Cliente)
            });

            await act.Should().ThrowAsync<InvalidOperationException>();

            _context.ChangeTracker.Clear();

            var reloaded = await _context.SavingsAccounts.AsNoTracking()
                .FirstAsync(a => a.AccountNumber == principal.AccountNumber);

            reloaded.Balance.Should().Be(originalSource);
            (await _context.SavingsAccounts.AsNoTracking()
                .FirstAsync(a => a.Id == other.Id)).Balance.Should().Be(originalTarget);

            // No debe quedar ningun movimiento aprobado del pago fallido.
            (await _context.Transactions.AsNoTracking()
                .CountAsync(t => t.Operation == TransactionOperation.PagoTarjeta
                                 && t.Status == TransactionStatus.Aprobada)).Should().Be(0);
        }

        [Fact]
        public async Task Repositorio_BeneficiarioDuplicado_ViolaElIndiceUnicoCompuesto()
        {
            var (_, _, other) = await TestData.SeedAsync(_context);
            var repository = new BeneficiaryRepository(_context);

            await repository.AddAsync(new Beneficiary
            {
                ClientId = TestData.ClientOne, SavingsAccountId = other.Id, CreatedAt = DateTime.Now
            });

            var act = async () => await repository.AddAsync(new Beneficiary
            {
                ClientId = TestData.ClientOne, SavingsAccountId = other.Id, CreatedAt = DateTime.Now
            });

            await act.Should().ThrowAsync<DbUpdateException>();
        }

        [Fact]
        public async Task Repositorio_CancelarCuentaNoEliminaSuHistorial()
        {
            var builder = new ServiceBuilder(_context);
            var (principal, secondary, _) = await TestData.SeedAsync(_context);

            await builder.TransactionService().DepositAsync(new DepositRequest
            {
                TargetAccountNumber = secondary.AccountNumber,
                Amount = 100m,
                Actor = OperationActor.Of(TestData.CashierId, Roles.Cajero)
            });

            await builder.SavingsAccountService().CancelSecondaryAsync(secondary.AccountNumber, TestData.AdminId);

            _context.ChangeTracker.Clear();

            // La cuenta sigue existiendo fisicamente y su historial esta intacto.
            var cancelled = await _context.SavingsAccounts.AsNoTracking()
                .FirstAsync(a => a.AccountNumber == secondary.AccountNumber);

            cancelled.Status.Should().Be(AccountStatus.Cancelada);
            (await _context.Transactions.AsNoTracking()
                .CountAsync(t => t.SavingsAccountId == secondary.Id)).Should().BeGreaterThan(0);
        }
    }
}
