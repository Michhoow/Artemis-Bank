using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Infrastructure.Persistence.Repositories;
using ArtemisBank.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArtemisBank.Tests.Integration
{
    public class ModuleRepositoryIntegrationTests
    {
        private static Loan NuevoPrestamo(string numero, string clienteId,
            LoanStatus estado = LoanStatus.Activo) => new()
        {
            LoanNumber = numero,
            ClientId = clienteId,
            ApprovedCapital = 60000m,
            AnnualInterestRate = 12m,
            TermInMonths = 12,
            MonthlyInstallment = 5330.36m,
            TotalToPay = 63964.32m,
            PaidAmount = 0m,
            Status = estado,
            DisbursementAccountNumber = "123456789",
            FirstDueDate = DateTime.Now.Date.AddMonths(1)
        };

        private static CreditCard NuevaTarjeta(string numero, string clienteId,
            CardStatus estado = CardStatus.Activa, decimal deuda = 0m) => new()
        {
            CardNumber = numero,
            ClientId = clienteId,
            CreditLimit = 50000m,
            Debt = deuda,
            ExpirationMonth = 12,
            ExpirationYear = DateTime.Now.Year + 3,
            CvcHash = "hash-de-prueba",
            CvcSalt = "sal-de-prueba",
            Status = estado
        };

        private static Commerce NuevoComercio(string rnc, string usuarioId = "") => new()
        {
            Name = $"Comercio {rnc}",
            Description = "Alta de prueba",
            Rnc = rnc,
            Email = $"comercio{rnc}@correo.do",
            Phone = "8095550100",
            UserId = usuarioId,
            AccountNumber = "987654321",
            IsActive = true
        };

        [Fact]
        public async Task LoanRepository_GuardaYRecuperaElPrestamoPorSuNumero()
        {
            using var context = TestContextFactory.CreateInMemory();
            var repo = new LoanRepository(context);

            await repo.AddAsync(NuevoPrestamo("100000001", TestData.ClientOne));

            var recuperado = await repo.GetByLoanNumberAsync("100000001");

            recuperado.Should().NotBeNull();
            recuperado!.ClientId.Should().Be(TestData.ClientOne);
            recuperado.ApprovedCapital.Should().Be(60000m);
        }

        [Fact]
        public async Task LoanRepository_GetWithInstallmentsCargaLasCuotasRelacionadas()
        {
            using var context = TestContextFactory.CreateInMemory();
            var repo = new LoanRepository(context);

            var prestamo = NuevoPrestamo("100000002", TestData.ClientOne);
            await repo.AddAsync(prestamo);

            for (var i = 1; i <= 3; i++)
            {
                context.LoanInstallments.Add(new LoanInstallment
                {
                    LoanId = prestamo.Id,
                    Number = i,
                    DueDate = DateTime.Now.Date.AddMonths(i),
                    CapitalAmount = 20000m,
                    InterestAmount = 500m,
                    TotalAmount = 20500m,
                    RemainingCapital = 60000m - (20000m * i)
                });
            }
            await context.SaveChangesAsync();

            var conCuotas = await repo.GetWithInstallmentsAsync(prestamo.Id);

            conCuotas.Should().NotBeNull();
            conCuotas!.Installments.Should().HaveCount(3);
        }

        [Fact]
        public async Task LoanRepository_HasActiveLoanDistingueActivoDeCompletado()
        {
            using var context = TestContextFactory.CreateInMemory();
            var repo = new LoanRepository(context);

            await repo.AddAsync(NuevoPrestamo("100000003", TestData.ClientOne, LoanStatus.Completado));

            (await repo.HasActiveLoanAsync(TestData.ClientOne)).Should().BeFalse();

            await repo.AddAsync(NuevoPrestamo("100000004", TestData.ClientOne));

            (await repo.HasActiveLoanAsync(TestData.ClientOne)).Should().BeTrue();
        }

        [Fact]
        public async Task LoanRepository_LoanNumberExistsDetectaElNumeroYaUsado()
        {
            using var context = TestContextFactory.CreateInMemory();
            var repo = new LoanRepository(context);

            await repo.AddAsync(NuevoPrestamo("100000005", TestData.ClientOne));

            (await repo.LoanNumberExistsAsync("100000005")).Should().BeTrue();
            (await repo.LoanNumberExistsAsync("999999999")).Should().BeFalse();
        }

        [Fact]
        public async Task LoanInstallmentRepository_GetOverdueSoloDevuelveVencidasSinPagar()
        {
            using var context = TestContextFactory.CreateInMemory();
            var prestamos = new LoanRepository(context);
            var cuotas = new LoanInstallmentRepository(context);

            var prestamo = NuevoPrestamo("100000006", TestData.ClientOne);
            await prestamos.AddAsync(prestamo);

            context.LoanInstallments.Add(new LoanInstallment
            {
                LoanId = prestamo.Id, Number = 1,
                DueDate = DateTime.Now.Date.AddDays(-10),
                CapitalAmount = 5000m, InterestAmount = 100m,
                TotalAmount = 5100m, PaidAmount = 0m
            });

            context.LoanInstallments.Add(new LoanInstallment
            {
                LoanId = prestamo.Id, Number = 2,
                DueDate = DateTime.Now.Date.AddDays(-5),
                CapitalAmount = 5000m, InterestAmount = 100m,
                TotalAmount = 5100m, PaidAmount = 5100m
            });

            context.LoanInstallments.Add(new LoanInstallment
            {
                LoanId = prestamo.Id, Number = 3,
                DueDate = DateTime.Now.Date.AddMonths(1),
                CapitalAmount = 5000m, InterestAmount = 100m,
                TotalAmount = 5100m, PaidAmount = 0m
            });

            await context.SaveChangesAsync();

            var vencidas = await cuotas.GetOverdueAsync(DateTime.Now.Date);

            vencidas.Should().HaveCount(1);
            vencidas[0].Number.Should().Be(1);
        }

        [Fact]
        public async Task LoanInstallmentRepository_GetByLoanDevuelveLasCuotasOrdenadas()
        {
            using var context = TestContextFactory.CreateInMemory();
            var prestamos = new LoanRepository(context);
            var cuotas = new LoanInstallmentRepository(context);

            var prestamo = NuevoPrestamo("100000007", TestData.ClientOne);
            await prestamos.AddAsync(prestamo);

            foreach (var n in new[] { 3, 1, 2 })
            {
                context.LoanInstallments.Add(new LoanInstallment
                {
                    LoanId = prestamo.Id, Number = n,
                    DueDate = DateTime.Now.Date.AddMonths(n),
                    CapitalAmount = 1000m, InterestAmount = 50m, TotalAmount = 1050m
                });
            }
            await context.SaveChangesAsync();

            var listado = await cuotas.GetByLoanAsync(prestamo.Id);

            listado.Select(c => c.Number).Should().ContainInOrder(1, 2, 3);
        }

        [Fact]
        public async Task CreditCardRepository_RecuperaLaTarjetaPorSuNumero()
        {
            using var context = TestContextFactory.CreateInMemory();
            var repo = new CreditCardRepository(context);

            await repo.AddAsync(NuevaTarjeta("4000000000000001", TestData.ClientOne));

            var recuperada = await repo.GetByCardNumberAsync("4000000000000001");

            recuperada.Should().NotBeNull();
            recuperada!.ClientId.Should().Be(TestData.ClientOne);

            recuperada.CvcHash.Should().NotBeNullOrWhiteSpace();
            recuperada.CvcSalt.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task CreditCardRepository_GetActiveByClientExcluyeLasCanceladas()
        {
            using var context = TestContextFactory.CreateInMemory();
            var repo = new CreditCardRepository(context);

            await repo.AddAsync(NuevaTarjeta("4000000000000002", TestData.ClientOne));
            await repo.AddAsync(NuevaTarjeta("4000000000000003", TestData.ClientOne,
                CardStatus.Cancelada));

            var activas = await repo.GetActiveByClientAsync(TestData.ClientOne);
            var todas = await repo.GetAllByClientAsync(TestData.ClientOne);

            activas.Should().HaveCount(1);

            todas.Should().HaveCount(2);
        }

        [Fact]
        public async Task CreditCardRepository_GetDebtByClientSumaSoloLasActivas()
        {
            using var context = TestContextFactory.CreateInMemory();
            var repo = new CreditCardRepository(context);

            await repo.AddAsync(NuevaTarjeta("4000000000000004", TestData.ClientOne,
                CardStatus.Activa, deuda: 1500m));
            await repo.AddAsync(NuevaTarjeta("4000000000000005", TestData.ClientOne,
                CardStatus.Activa, deuda: 2500m));

            var deuda = await repo.GetDebtByClientAsync(TestData.ClientOne);

            deuda.Should().Be(4000m);
        }

        [Fact]
        public async Task CardConsumptionRepository_GuardaYRecuperaLosConsumosDeLaTarjeta()
        {
            using var context = TestContextFactory.CreateInMemory();
            var tarjetas = new CreditCardRepository(context);
            var consumos = new CardConsumptionRepository(context);

            var tarjeta = NuevaTarjeta("4000000000000006", TestData.ClientOne);
            await tarjetas.AddAsync(tarjeta);

            await consumos.AddAsync(new CardConsumption
            {
                CreditCardId = tarjeta.Id,
                Amount = 1200m,
                Type = ConsumptionType.Consumo,
                Status = ConsumptionStatus.Aprobado,
                Description = "Compra de prueba",
                CommerceName = "Comercio de prueba",
                OperationReference = Guid.NewGuid().ToString()
            });

            var listado = await consumos.GetByCardAsync(tarjeta.Id);

            listado.Should().HaveCount(1);
            listado[0].Amount.Should().Be(1200m);
            listado[0].Status.Should().Be(ConsumptionStatus.Aprobado);
        }

        [Fact]
        public async Task CardConsumptionRepository_ConservaLosConsumosRechazados()
        {
            using var context = TestContextFactory.CreateInMemory();
            var tarjetas = new CreditCardRepository(context);
            var consumos = new CardConsumptionRepository(context);

            var tarjeta = NuevaTarjeta("4000000000000007", TestData.ClientOne);
            await tarjetas.AddAsync(tarjeta);

            await consumos.AddAsync(new CardConsumption
            {
                CreditCardId = tarjeta.Id,
                Amount = 999999m,
                Type = ConsumptionType.Consumo,
                Status = ConsumptionStatus.Rechazado,
                Description = "Intento rechazado",
                RejectionReason = "Excede el crédito disponible",
                OperationReference = Guid.NewGuid().ToString()
            });

            var listado = await consumos.GetByCardAsync(tarjeta.Id);

            listado.Should().ContainSingle(c => c.Status == ConsumptionStatus.Rechazado);
            listado[0].RejectionReason.Should().NotBeNullOrWhiteSpace();

            var almacenada = await context.CreditCards.FirstAsync(c => c.Id == tarjeta.Id);
            almacenada.Debt.Should().Be(0m);
        }

        [Fact]
        public async Task TransactionRepository_PersisteDebitoYCreditoConSuReferencia()
        {
            using var context = TestContextFactory.CreateInMemory();
            var (principal, secundaria, _) = await TestData.SeedAsync(context);
            var repo = new TransactionRepository(context);

            var referencia = Guid.NewGuid().ToString();

            await repo.AddAsync(new Transaction
            {
                SavingsAccountId = principal.Id,
                Amount = 500m,
                Type = TransactionType.Debito,
                Operation = TransactionOperation.TransferenciaEntreCuentas,
                Origin = principal.AccountNumber,
                Beneficiary = secundaria.AccountNumber,
                OperationReference = referencia,
                PerformedByUserId = TestData.ClientOne
            });

            await repo.AddAsync(new Transaction
            {
                SavingsAccountId = secundaria.Id,
                Amount = 500m,
                Type = TransactionType.Credito,
                Operation = TransactionOperation.TransferenciaEntreCuentas,
                Origin = principal.AccountNumber,
                Beneficiary = secundaria.AccountNumber,
                OperationReference = referencia,
                PerformedByUserId = TestData.ClientOne
            });

            var movimientos = await context.Transactions
                .Where(t => t.OperationReference == referencia)
                .ToListAsync();

            movimientos.Should().HaveCount(2);
            movimientos.Should().ContainSingle(t => t.Type == TransactionType.Debito);
            movimientos.Should().ContainSingle(t => t.Type == TransactionType.Credito);
        }

        [Fact]
        public async Task TransactionRepository_CuentaLaTransferenciaComoUnaSolaOperacion()
        {
            using var context = TestContextFactory.CreateInMemory();
            var (principal, secundaria, _) = await TestData.SeedAsync(context);
            var repo = new TransactionRepository(context);

            var referencia = Guid.NewGuid().ToString();

            foreach (var (cuenta, tipo) in new[]
            {
                (principal, TransactionType.Debito),
                (secundaria, TransactionType.Credito)
            })
            {
                await repo.AddAsync(new Transaction
                {
                    SavingsAccountId = cuenta.Id,
                    Amount = 250m,
                    Type = tipo,
                    Operation = TransactionOperation.TransferenciaEntreCuentas,
                    Origin = principal.AccountNumber,
                    Beneficiary = secundaria.AccountNumber,
                    OperationReference = referencia,
                    PerformedByUserId = TestData.CashierId,
                    PerformedByRole = Roles.Cajero
                });
            }

            var operaciones = await repo.CountByCashierAsync(TestData.CashierId, DateTime.Now.Date);

            operaciones.Should().Be(1);
        }

        [Fact]
        public async Task CommerceRepository_RecuperaElComercioPorSuRnc()
        {
            using var context = TestContextFactory.CreateInMemory();
            var repo = new CommerceRepository(context);

            await repo.AddAsync(NuevoComercio("130111111"));

            var recuperado = await repo.GetByRncAsync("130111111");

            recuperado.Should().NotBeNull();
            recuperado!.Name.Should().Be("Comercio 130111111");
            recuperado.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task CommerceRepository_DetectaRncYCorreoYaRegistrados()
        {
            using var context = TestContextFactory.CreateInMemory();
            var repo = new CommerceRepository(context);

            var comercio = NuevoComercio("130222222");
            await repo.AddAsync(comercio);

            (await repo.RncExistsAsync("130222222")).Should().BeTrue();
            (await repo.RncExistsAsync("130999999")).Should().BeFalse();

            (await repo.EmailExistsAsync(comercio.Email)).Should().BeTrue();
            (await repo.EmailExistsAsync("libre@correo.do")).Should().BeFalse();
        }

        [Fact]
        public async Task CommerceRepository_RecuperaElComercioPorSuUsuarioAsociado()
        {
            using var context = TestContextFactory.CreateInMemory();
            var repo = new CommerceRepository(context);

            await repo.AddAsync(NuevoComercio("130333333", usuarioId: "usuario-comercio-1"));

            var recuperado = await repo.GetByUserIdAsync("usuario-comercio-1");

            recuperado.Should().NotBeNull();
            recuperado!.Rnc.Should().Be("130333333");
        }

        [Fact]
        public async Task CommerceRepository_InactivarConservaElRegistroYSuHistorial()
        {
            using var context = TestContextFactory.CreateInMemory();
            var repo = new CommerceRepository(context);

            var comercio = NuevoComercio("130444444");
            await repo.AddAsync(comercio);

            comercio.IsActive = false;
            await repo.UpdateEntityAsync(comercio);

            var recuperado = await repo.GetByRncAsync("130444444");

            recuperado.Should().NotBeNull();
            recuperado!.IsActive.Should().BeFalse();
            recuperado.Name.Should().Be("Comercio 130444444");
        }
    }
}
