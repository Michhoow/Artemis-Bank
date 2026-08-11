using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Domain.Common.Enums;
using ArtemisBank.Tests.Common;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ArtemisBank.Tests.Unit.Services
{
    /// <summary>Unit tests de transferencias, beneficiarios, depositos, retiros y pagos.</summary>
    public class TransactionServiceTests
    {
        // ---------------------------------------------------------------- transferencias

        [Fact]
        public async Task Transferencia_Aprobada_RegistraDebitoYCreditoConLaMismaReferencia()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (principal, _, other) = await TestData.SeedAsync(context);

            var result = await builder.TransactionService().TransferAsync(new TransferRequest
            {
                SourceAccountNumber = principal.AccountNumber,
                TargetAccountNumber = other.AccountNumber,
                Amount = 1000m,
                Operation = TransactionOperation.TransaccionExpress,
                Actor = OperationActor.Of(TestData.ClientOne, Roles.Cliente)
            });

            result.Succeeded.Should().BeTrue();

            (await context.SavingsAccounts.FirstAsync(a => a.Id == principal.Id)).Balance.Should().Be(9000m);
            (await context.SavingsAccounts.FirstAsync(a => a.Id == other.Id)).Balance.Should().Be(1500m);

            var movements = await context.Transactions.ToListAsync();
            movements.Should().HaveCount(2);
            movements.Select(m => m.OperationReference).Distinct().Should().ContainSingle();

            var debit = movements.Single(m => m.Type == TransactionType.Debito);
            var credit = movements.Single(m => m.Type == TransactionType.Credito);

            debit.Beneficiary.Should().Be(other.AccountNumber);
            credit.Origin.Should().Be(principal.AccountNumber);
            movements.Should().OnlyContain(m => m.Status == TransactionStatus.Aprobada);
        }

        [Fact]
        public async Task Transferencia_SinFondos_SeRechazaYNoModificaBalances()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (principal, _, other) = await TestData.SeedAsync(context);

            var result = await builder.TransactionService().TransferAsync(new TransferRequest
            {
                SourceAccountNumber = principal.AccountNumber,
                TargetAccountNumber = other.AccountNumber,
                Amount = 999_999m,
                Operation = TransactionOperation.TransaccionExpress,
                Actor = OperationActor.Of(TestData.ClientOne, Roles.Cliente)
            });

            result.Succeeded.Should().BeFalse();
            result.ErrorMessage.Should().Be(AppMessages.InsufficientFundsExpress);

            (await context.SavingsAccounts.FirstAsync(a => a.Id == principal.Id)).Balance.Should().Be(10000m);
            (await context.SavingsAccounts.FirstAsync(a => a.Id == other.Id)).Balance.Should().Be(500m);

            // El intento queda registrado en la cuenta de origen.
            var rejected = await context.Transactions.SingleAsync();
            rejected.Status.Should().Be(TransactionStatus.Rechazada);
            rejected.SavingsAccountId.Should().Be(principal.Id);
            rejected.RejectionReason.Should().Be(AppMessages.RejectedInsufficientFunds);
        }

        [Fact]
        public async Task Transferencia_HaciaCuentaCancelada_SeRechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (principal, _, other) = await TestData.SeedAsync(context);

            other.Status = AccountStatus.Cancelada;
            await context.SaveChangesAsync();

            var result = await builder.TransactionService().TransferAsync(new TransferRequest
            {
                SourceAccountNumber = principal.AccountNumber,
                TargetAccountNumber = other.AccountNumber,
                Amount = 100m,
                Operation = TransactionOperation.TransaccionExpress,
                Actor = OperationActor.Of(TestData.ClientOne, Roles.Cliente)
            });

            result.Succeeded.Should().BeFalse();
            (await context.SavingsAccounts.FirstAsync(a => a.Id == principal.Id)).Balance.Should().Be(10000m);
        }

        [Fact]
        public async Task Transferencia_MismaCuentaOrigenYDestino_SeRechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (principal, _, _) = await TestData.SeedAsync(context);

            var result = await builder.TransactionService().TransferAsync(new TransferRequest
            {
                SourceAccountNumber = principal.AccountNumber,
                TargetAccountNumber = principal.AccountNumber,
                Amount = 100m,
                Operation = TransactionOperation.TransaccionExpress,
                Actor = OperationActor.Of(TestData.ClientOne, Roles.Cliente)
            });

            result.Succeeded.Should().BeFalse();
            result.ErrorMessage.Should().Be(AppMessages.SameAccountNotAllowed);
        }

        [Fact]
        public async Task Transferencia_CuentaDeOrigenAjena_SeRechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (_, _, other) = await TestData.SeedAsync(context);

            var result = await builder.TransactionService().TransferAsync(new TransferRequest
            {
                SourceAccountNumber = other.AccountNumber,
                TargetAccountNumber = "100000001",
                Amount = 10m,
                Operation = TransactionOperation.TransaccionExpress,
                Actor = OperationActor.Of(TestData.ClientOne, Roles.Cliente),
                ExpectedSourceOwnerId = TestData.ClientOne
            });

            result.Succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task Transferencia_FalloDeCorreo_NoRevierteLaOperacion()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (principal, _, other) = await TestData.SeedAsync(context);

            builder.Emails.Setup(e => e.SendAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(false);

            var result = await builder.TransactionService().TransferAsync(new TransferRequest
            {
                SourceAccountNumber = principal.AccountNumber,
                TargetAccountNumber = other.AccountNumber,
                Amount = 250m,
                Operation = TransactionOperation.TransaccionExpress,
                Actor = OperationActor.Of(TestData.ClientOne, Roles.Cliente)
            });

            result.Succeeded.Should().BeTrue();
            result.Warning.Should().Be(AppMessages.TransactionOkMailFailed);
            (await context.SavingsAccounts.FirstAsync(a => a.Id == other.Id)).Balance.Should().Be(750m);
        }

        // ---------------------------------------------------------------- deposito y retiro

        [Fact]
        public async Task Deposito_AcreditaConOrigenDepositoYBeneficiarioCuenta()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (principal, _, _) = await TestData.SeedAsync(context);

            var result = await builder.TransactionService().DepositAsync(new DepositRequest
            {
                TargetAccountNumber = principal.AccountNumber,
                Amount = 3000m,
                Actor = OperationActor.Of(TestData.CashierId, Roles.Cajero)
            });

            result.Succeeded.Should().BeTrue();
            (await context.SavingsAccounts.FirstAsync(a => a.Id == principal.Id)).Balance.Should().Be(13000m);

            var movement = await context.Transactions.SingleAsync();
            movement.Type.Should().Be(TransactionType.Credito);
            movement.Origin.Should().Be(DisplayText.Deposit);
            movement.Beneficiary.Should().Be(principal.AccountNumber);
            movement.PerformedByUserId.Should().Be(TestData.CashierId);
            movement.PerformedByRole.Should().Be(Roles.Cajero);
        }

        [Fact]
        public async Task Retiro_DebitaConBeneficiarioRetiro()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (principal, _, _) = await TestData.SeedAsync(context);

            var result = await builder.TransactionService().WithdrawAsync(new WithdrawalRequest
            {
                SourceAccountNumber = principal.AccountNumber,
                Amount = 2000m,
                Actor = OperationActor.Of(TestData.CashierId, Roles.Cajero)
            });

            result.Succeeded.Should().BeTrue();
            (await context.SavingsAccounts.FirstAsync(a => a.Id == principal.Id)).Balance.Should().Be(8000m);

            var movement = await context.Transactions.SingleAsync();
            movement.Type.Should().Be(TransactionType.Debito);
            movement.Origin.Should().Be(principal.AccountNumber);
            movement.Beneficiary.Should().Be(DisplayText.Withdrawal);
        }

        [Fact]
        public async Task Retiro_SinFondos_RegistraRechazoSinTocarBalance()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (principal, _, _) = await TestData.SeedAsync(context);

            var result = await builder.TransactionService().WithdrawAsync(new WithdrawalRequest
            {
                SourceAccountNumber = principal.AccountNumber,
                Amount = 50_000m,
                Actor = OperationActor.Of(TestData.CashierId, Roles.Cajero)
            });

            result.Succeeded.Should().BeFalse();
            result.ErrorMessage.Should().Be(AppMessages.InsufficientFundsAccount);
            (await context.SavingsAccounts.FirstAsync(a => a.Id == principal.Id)).Balance.Should().Be(10000m);
            (await context.Transactions.SingleAsync()).Status.Should().Be(TransactionStatus.Rechazada);
        }

        // ---------------------------------------------------------------- pagos

        [Fact]
        public async Task PagoTarjeta_MontoMayorALaDeuda_SoloDebitaLaDeudaReal()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (principal, _, _) = await TestData.SeedAsync(context);

            builder.Cards.Setup(c => c.GetByIdAsync(7)).ReturnsAsync(new CreditCardInfoDto
            {
                Id = 7, ClientId = TestData.ClientOne, LastFourDigits = "4321",
                CreditLimit = 50000m, Debt = 500m, AvailableCredit = 49500m, IsActive = true
            });

            var result = await builder.TransactionService().PayCreditCardAsync(new CardPaymentRequest
            {
                SourceAccountNumber = principal.AccountNumber,
                CreditCardId = 7,
                Amount = 1000m,
                Actor = OperationActor.Of(TestData.ClientOne, Roles.Cliente)
            });

            result.Succeeded.Should().BeTrue();

            // Solo se debitan RD$500.00: el excedente no se descuenta.
            (await context.SavingsAccounts.FirstAsync(a => a.Id == principal.Id)).Balance.Should().Be(9500m);

            var movement = await context.Transactions.SingleAsync();
            movement.Amount.Should().Be(500m);
            movement.Beneficiary.Should().Be("4321");
            movement.Operation.Should().Be(TransactionOperation.PagoTarjeta);

            builder.Cards.Verify(c => c.ApplyPaymentAsync(7, 500m, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PagoTarjeta_SinDeuda_SeRechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (principal, _, _) = await TestData.SeedAsync(context);

            builder.Cards.Setup(c => c.GetByIdAsync(7)).ReturnsAsync(new CreditCardInfoDto
            {
                Id = 7, ClientId = TestData.ClientOne, LastFourDigits = "4321", Debt = 0m, IsActive = true
            });

            var result = await builder.TransactionService().PayCreditCardAsync(new CardPaymentRequest
            {
                SourceAccountNumber = principal.AccountNumber,
                CreditCardId = 7,
                Amount = 100m,
                Actor = OperationActor.Of(TestData.ClientOne, Roles.Cliente)
            });

            result.Succeeded.Should().BeFalse();
            result.ErrorMessage.Should().Be(AppMessages.CardWithoutDebt);
            builder.Cards.Verify(c => c.ApplyPaymentAsync(It.IsAny<int>(), It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task PagoPrestamo_MontoMayorAlPendiente_SoloDebitaElPendienteReal()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (principal, _, _) = await TestData.SeedAsync(context);

            builder.Loans.Setup(l => l.GetByIdAsync(3)).ReturnsAsync(new LoanInfoDto
            {
                Id = 3, LoanNumber = "300000001", ClientId = TestData.ClientOne,
                PendingAmount = 2000m, IsActive = true
            });

            var result = await builder.TransactionService().PayLoanAsync(new LoanPaymentRequest
            {
                SourceAccountNumber = principal.AccountNumber,
                LoanId = 3,
                Amount = 3000m,
                Actor = OperationActor.Of(TestData.ClientOne, Roles.Cliente)
            });

            result.Succeeded.Should().BeTrue();
            (await context.SavingsAccounts.FirstAsync(a => a.Id == principal.Id)).Balance.Should().Be(8000m);

            var movement = await context.Transactions.SingleAsync();
            movement.Amount.Should().Be(2000m);
            movement.Beneficiary.Should().Be("300000001");

            builder.Loans.Verify(l => l.ApplyPaymentAsync(3, 2000m, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task PagoPrestamo_SinCuotasPendientes_SeRechaza()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (principal, _, _) = await TestData.SeedAsync(context);

            builder.Loans.Setup(l => l.GetByIdAsync(3)).ReturnsAsync(new LoanInfoDto
            {
                Id = 3, LoanNumber = "300000001", ClientId = TestData.ClientOne,
                PendingAmount = 0m, IsActive = true
            });

            var result = await builder.TransactionService().PayLoanAsync(new LoanPaymentRequest
            {
                SourceAccountNumber = principal.AccountNumber,
                LoanId = 3,
                Amount = 100m,
                Actor = OperationActor.Of(TestData.ClientOne, Roles.Cliente)
            });

            result.Succeeded.Should().BeFalse();
            result.ErrorMessage.Should().Be(AppMessages.LoanWithoutPendingInstallments);
        }

        [Fact]
        public async Task MontoCero_SeRechazaSiempre()
        {
            using var context = TestContextFactory.CreateInMemory();
            var builder = new ServiceBuilder(context);
            var (principal, _, other) = await TestData.SeedAsync(context);
            var service = builder.TransactionService();

            (await service.DepositAsync(new DepositRequest
            {
                TargetAccountNumber = principal.AccountNumber, Amount = 0m
            })).Succeeded.Should().BeFalse();

            (await service.WithdrawAsync(new WithdrawalRequest
            {
                SourceAccountNumber = principal.AccountNumber, Amount = 0m
            })).Succeeded.Should().BeFalse();

            (await service.TransferAsync(new TransferRequest
            {
                SourceAccountNumber = principal.AccountNumber,
                TargetAccountNumber = other.AccountNumber,
                Amount = 0m
            })).Succeeded.Should().BeFalse();
        }
    }
}
