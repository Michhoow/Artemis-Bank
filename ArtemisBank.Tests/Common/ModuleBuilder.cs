using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Application.Mappings;
using ArtemisBank.Core.Application.Mappings.EntitiesAndDtos;
using ArtemisBank.Core.Application.Services;
using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Persistence.Contexts;
using ArtemisBank.Infrastructure.Persistence.Repositories;
using ArtemisBank.Infrastructure.Shared.Services;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ArtemisBank.Tests.Common
{
    public class ModuleBuilder
    {
        public ArtemisDbContext Context { get; }

        public ISavingsAccountRepository AccountRepository { get; }
        public ITransactionRepository TransactionRepository { get; }
        public ILoanRepository LoanRepository { get; }
        public ILoanInstallmentRepository InstallmentRepository { get; }
        public ICreditCardRepository CardRepository { get; }
        public ICardConsumptionRepository ConsumptionRepository { get; }
        public ICommerceRepository CommerceRepository { get; }
        public IUnitOfWork UnitOfWork { get; }
        public IMapper Mapper { get; }
        public ICryptoService Crypto { get; } = new CryptoService();

        public Mock<IUserReadService> Users { get; } = new();
        public Mock<IEmailService> Emails { get; } = new();
        public Mock<IUserManagementService> UserManagement { get; } = new();

        public ModuleBuilder(ArtemisDbContext context)
        {
            Context = context;

            AccountRepository = new SavingsAccountRepository(context);
            TransactionRepository = new TransactionRepository(context);
            LoanRepository = new LoanRepository(context);
            InstallmentRepository = new LoanInstallmentRepository(context);
            CardRepository = new CreditCardRepository(context);
            ConsumptionRepository = new CardConsumptionRepository(context);
            CommerceRepository = new CommerceRepository(context);
            UnitOfWork = new UnitOfWork(context);

            var configuration = new MapperConfiguration(config =>
            {
                config.AddProfile<SavingsAccountProfile>();
                config.AddProfile<TransactionProfile>();
                config.AddProfile<LoanProfile>();
                config.AddProfile<CreditCardProfile>();
                config.AddProfile<CommerceProfile>();
            });
            Mapper = configuration.CreateMapper();

            Emails.Setup(e => e.SendAsync(It.IsAny<EmailRequest>(), It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true);

            Users.Setup(u => u.GetByIdAsync(It.IsAny<string>()))
                 .ReturnsAsync((string id) => TestData.User(id, "001" + id));

            Users.Setup(u => u.GetByIdsAsync(It.IsAny<IEnumerable<string>>()))
                 .ReturnsAsync((IEnumerable<string> ids) =>
                     ids.Select(id => TestData.User(id, "001" + id)).ToList());

            Users.Setup(u => u.GetActiveClientIdsAsync())
                 .ReturnsAsync(new List<string> { TestData.ClientOne, TestData.ClientTwo });

            Users.Setup(u => u.GetByIdentificationAsync(It.IsAny<string>()))
                 .ReturnsAsync((string identification) =>
                     identification == "001" + TestData.ClientOne
                         ? TestData.User(TestData.ClientOne, identification)
                         : null);

            UserManagement.Setup(u => u.DeactivateUsersOfCommerceAsync(
                    It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<string> ids, CancellationToken _) => ids.Count());
        }

        public LoanReadService LoanReadService()
            => new LoanReadService(LoanRepository, InstallmentRepository,
                NullLogger<LoanReadService>.Instance);

        public CreditCardReadService CardReadService()
            => new CreditCardReadService(CardRepository, ConsumptionRepository,
                NullLogger<CreditCardReadService>.Instance);

        public IAccountNumberGenerator NumberGenerator()
            => new AccountNumberGenerator(AccountRepository, LoanReadService());

        public TransactionService TransactionService()
            => new TransactionService(AccountRepository, TransactionRepository, UnitOfWork,
                Users.Object, CardReadService(), LoanReadService(), Emails.Object, Mapper,
                NullLogger<TransactionService>.Instance);

        public RiskAssessmentService RiskService()
            => new RiskAssessmentService(LoanRepository, CardRepository, Users.Object);

        public LoanService LoanService()
            => new LoanService(LoanRepository, InstallmentRepository, AccountRepository,
                Users.Object, RiskService(), NumberGenerator(), TransactionService(),
                Emails.Object, UnitOfWork, new GenericRepository<Loan>(Context), Mapper,
                NullLogger<LoanService>.Instance);

        public CreditCardService CardService()
            => new CreditCardService(CardRepository, ConsumptionRepository, AccountRepository,
                Users.Object, Crypto, TransactionService(), Emails.Object, UnitOfWork,
                new GenericRepository<CreditCard>(Context), Mapper,
                NullLogger<CreditCardService>.Instance);

        public CommerceService CommerceService()
            => new CommerceService(CommerceRepository, UserManagement.Object, Users.Object,
                new GenericRepository<Commerce>(Context), Mapper,
                NullLogger<CommerceService>.Instance);

        public HermesPayService HermesPayService()
            => new HermesPayService(CardRepository, ConsumptionRepository, CommerceRepository,
                AccountRepository, Users.Object, Crypto, TransactionService(), Emails.Object,
                UnitOfWork, NullLogger<HermesPayService>.Instance);
    }
}
