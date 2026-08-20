using ArtemisBank.Core.Application.Common.Models;
using ArtemisBank.Core.Application.Dtos.SavingsAccounts;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Application.Services;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Persistence.Contexts;
using ArtemisBank.Infrastructure.Persistence.Repositories;
using ArtemisBank.Infrastructure.Shared.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ArtemisBank.Tests.Common
{
    /// <summary>
    /// Arma los servicios de productos de credito de Manuel (prestamos, tarjetas, Hermes Pay) sobre un
    /// contexto de prueba real, con dobles para los contratos de Michael (cuentas, transacciones),
    /// Monserrat (usuarios, comercios) y correo. El calculo de amortizacion y el hash son reales.
    /// </summary>
    public class CreditProductsServiceBuilder
    {
        public ArtemisDbContext Context { get; }
        public ILoanRepository LoanRepository { get; }
        public ICreditCardRepository CreditCardRepository { get; }
        public IUnitOfWork UnitOfWork { get; }
        public ICryptoService Crypto { get; } = new CryptoService();
        public IAmortizationService Amortization { get; } = new AmortizationService();

        public Mock<IUserReadService> Users { get; } = new();
        public Mock<ISavingsAccountService> Accounts { get; } = new();
        public Mock<ITransactionService> Transactions { get; } = new();
        public Mock<ICommerceRepository> Commerces { get; } = new();
        public Mock<IAccountNumberGenerator> NumberGenerator { get; } = new();
        public Mock<IEmailService> Emails { get; } = new();

        private int _generatedNumber = 100000000;

        public CreditProductsServiceBuilder(ArtemisDbContext context)
        {
            Context = context;
            LoanRepository = new LoanRepository(context);
            CreditCardRepository = new CreditCardRepository(context);
            UnitOfWork = new UnitOfWork(context);

            // Correo siempre exitoso.
            Emails.Setup(e => e.SendAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);

            // Usuarios: cliente activo por defecto.
            Users.Setup(u => u.GetByIdAsync(It.IsAny<string>()))
                 .ReturnsAsync((string id) => TestData.User(id, "001" + id));
            Users.Setup(u => u.GetByIdsAsync(It.IsAny<IEnumerable<string>>()))
                 .ReturnsAsync((IEnumerable<string> ids) =>
                     ids.Select(id => TestData.User(id, "001" + id)).ToList());
            Users.Setup(u => u.GetActiveClientIdsAsync())
                 .ReturnsAsync(new List<string> { TestData.ClientOne, TestData.ClientTwo });
            Users.Setup(u => u.GetClientsAsync(It.IsAny<bool>()))
                 .ReturnsAsync(new List<Core.Application.Dtos.Common.UserInfoDto>
                 {
                     TestData.User(TestData.ClientOne, "001"),
                     TestData.User(TestData.ClientTwo, "002")
                 });

            // Generador de numeros de 9 digitos incremental y unico.
            NumberGenerator.Setup(g => g.GenerateAsync(It.IsAny<CancellationToken>()))
                           .ReturnsAsync(() => (++_generatedNumber).ToString());

            // Credito externo exitoso por defecto (desembolsos, avances, Hermes Pay).
            Transactions.Setup(t => t.RegisterExternalCreditAsync(It.IsAny<ExternalCreditRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(OperationResult.Success());

            // Por defecto el cliente tiene una cuenta principal activa.
            Accounts.Setup(a => a.GetActiveByClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new List<SavingsAccountDto>
                    {
                        new SavingsAccountDto
                        {
                            AccountNumber = "900000001",
                            ClientId = TestData.ClientOne,
                            Type = "Principal",
                            Status = "Activa",
                            Balance = 0m
                        }
                    });
        }

        public void GivenAccountByNumber(SavingsAccount account)
            => Accounts.Setup(a => a.GetEntityByNumberAsync(account.AccountNumber, It.IsAny<CancellationToken>()))
                       .ReturnsAsync(account);

        public LoanService LoanService()
            => new LoanService(LoanRepository, CreditCardRepository, Users.Object, Accounts.Object,
                Transactions.Object, Amortization, NumberGenerator.Object, Emails.Object, UnitOfWork,
                NullLogger<LoanService>.Instance);

        public CreditCardService CreditCardService()
            => new CreditCardService(CreditCardRepository, Users.Object, Accounts.Object, Transactions.Object,
                Crypto, Emails.Object, UnitOfWork, NullLogger<CreditCardService>.Instance);

        public HermesPayService HermesPayService()
            => new HermesPayService(CreditCardRepository, Commerces.Object, Accounts.Object, Transactions.Object,
                Users.Object, Crypto, Emails.Object, UnitOfWork, NullLogger<HermesPayService>.Instance);
    }
}
