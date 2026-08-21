using ArtemisBank.Core.Application.Common.Constants;
using ArtemisBank.Core.Application.Common.Exceptions;
using ArtemisBank.Core.Application.Interfaces;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Core.Domain.Interfaces;

namespace ArtemisBank.Core.Application.Services
{
    public class AccountNumberGenerator : IAccountNumberGenerator
    {
        private const int MaxAttempts = 25;
        private static readonly Random Randomizer = new Random();

        private readonly ISavingsAccountRepository _accountRepository;
        private readonly ILoanReadService _loanReadService;

        public AccountNumberGenerator(ISavingsAccountRepository accountRepository, ILoanReadService loanReadService)
        {
            _accountRepository = accountRepository;
            _loanReadService = loanReadService;
        }

        public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
        {
            for (var attempt = 0; attempt < MaxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                int value;
                lock (Randomizer)
                {
                    value = Randomizer.Next(0, 1_000_000_000);
                }
                var candidate = value.ToString("D9");

                if (await _accountRepository.AccountNumberExistsAsync(candidate)) continue;
                if (await _loanReadService.LoanNumberExistsAsync(candidate)) continue;

                return candidate;
            }

            throw new ConflictException(AppMessages.CouldNotGenerateAccountNumber);
        }
    }
}
