namespace ArtemisBank.Core.Application.Interfaces
{
    public interface IAccountNumberGenerator
    {
        Task<string> GenerateAsync(CancellationToken cancellationToken = default);
    }
}
