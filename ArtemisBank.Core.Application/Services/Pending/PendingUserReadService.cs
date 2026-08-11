using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.Interfaces.Contracts;

namespace ArtemisBank.Core.Application.Services.Pending
{
    /// <summary>
    /// TEMPORAL — implementacion puente de IUserReadService.
    /// La implementacion definitiva la provee Monserrat en ArtemisBank.Infrastructure.Identity
    /// y, al registrarse despues de la capa Application, sobrescribe automaticamente a esta.
    /// </summary>
    public class PendingUserReadService : IUserReadService
    {
        public Task<UserInfoDto?> GetByIdAsync(string userId)
            => Task.FromResult(DemoDirectory.Users.FirstOrDefault(u => u.Id == userId));

        public Task<UserInfoDto?> GetByIdentificationAsync(string identification)
            => Task.FromResult(DemoDirectory.Users.FirstOrDefault(u => u.Identification == identification));

        public Task<List<UserInfoDto>> GetByIdsAsync(IEnumerable<string> userIds)
        {
            var set = userIds.ToHashSet(StringComparer.Ordinal);
            return Task.FromResult(DemoDirectory.Users.Where(u => set.Contains(u.Id)).ToList());
        }

        public Task<List<UserInfoDto>> GetClientsAsync(bool onlyActive = true)
            => Task.FromResult(DemoDirectory.Users
                .Where(u => u.Role == "Cliente" && (!onlyActive || u.IsActive))
                .ToList());

        public Task<int> CountClientsAsync(bool? isActive = null)
            => Task.FromResult(DemoDirectory.Users
                .Count(u => u.Role == "Cliente" && (isActive == null || u.IsActive == isActive)));

        public Task<List<string>> GetActiveClientIdsAsync()
            => Task.FromResult(DemoDirectory.Users
                .Where(u => u.Role == "Cliente" && u.IsActive)
                .Select(u => u.Id)
                .ToList());
    }
}
