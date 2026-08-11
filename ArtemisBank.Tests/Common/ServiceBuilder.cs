using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Application.Mappings;
using ArtemisBank.Core.Application.Services;
using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Persistence.Contexts;
using ArtemisBank.Infrastructure.Persistence.Repositories;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ArtemisBank.Tests.Common
{
    /// <summary>Arma los servicios reales sobre un contexto de prueba y dobles para los contratos ajenos.</summary>
    public class ServiceBuilder
    {
        public ArtemisDbContext Context { get; }
        public ISavingsAccountRepository AccountRepository { get; }
        public ITransactionRepository TransactionRepository { get; }
        public IBeneficiaryRepository BeneficiaryRepository { get; }
        public IUnitOfWork UnitOfWork { get; }
        public IMapper Mapper { get; }

        public Mock<IUserReadService> Users { get; } = new();
        public Mock<ILoanReadService> Loans { get; } = new();
        public Mock<ICreditCardReadService> Cards { get; } = new();
        public Mock<IEmailService> Emails { get; } = new();

        public ServiceBuilder(ArtemisDbContext context)
        {
            Context = context;
            AccountRepository = new SavingsAccountRepository(context);
            TransactionRepository = new TransactionRepository(context);
            BeneficiaryRepository = new BeneficiaryRepository(context);
            UnitOfWork = new UnitOfWork(context);

            var configuration = new MapperConfiguration(config =>
            {
                config.AddProfile<SavingsAccountProfile>();
                config.AddProfile<TransactionProfile>();
                config.AddProfile<ClientProfile>();
            });
            Mapper = configuration.CreateMapper();

            // Dobles por defecto: sin prestamos ni tarjetas, correo siempre exitoso.
            Loans.Setup(l => l.LoanNumberExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            Loans.Setup(l => l.CountActiveAsync()).ReturnsAsync(0);
            Loans.Setup(l => l.GetPendingDebtByClientAsync(It.IsAny<string>())).ReturnsAsync(0m);
            Cards.Setup(c => c.CountActiveAsync()).ReturnsAsync(0);
            Cards.Setup(c => c.GetDebtByClientAsync(It.IsAny<string>())).ReturnsAsync(0m);
            Emails.Setup(e => e.SendAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);

            Users.Setup(u => u.GetByIdAsync(It.IsAny<string>()))
                 .ReturnsAsync((string id) => TestData.User(id, "001" + id));
            Users.Setup(u => u.GetByIdsAsync(It.IsAny<IEnumerable<string>>()))
                 .ReturnsAsync((IEnumerable<string> ids) =>
                     ids.Select(id => TestData.User(id, "001" + id)).ToList());
        }

        public IAccountNumberGenerator NumberGenerator()
            => new AccountNumberGenerator(AccountRepository, Loans.Object);

        public SavingsAccountService SavingsAccountService()
            => new SavingsAccountService(AccountRepository, TransactionRepository, NumberGenerator(),
                Users.Object, UnitOfWork, Mapper, NullLogger<SavingsAccountService>.Instance);

        public TransactionService TransactionService()
            => new TransactionService(AccountRepository, TransactionRepository, UnitOfWork, Users.Object,
                Cards.Object, Loans.Object, Emails.Object, Mapper, NullLogger<TransactionService>.Instance);

        public BeneficiaryService BeneficiaryService()
            => new BeneficiaryService(BeneficiaryRepository, AccountRepository, Users.Object,
                NullLogger<BeneficiaryService>.Instance);

        public AdminHomeService AdminHomeService()
            => new AdminHomeService(TransactionRepository, AccountRepository, Users.Object,
                Loans.Object, Cards.Object);

        public CashierHomeService CashierHomeService()
            => new CashierHomeService(TransactionRepository);
    }
}
