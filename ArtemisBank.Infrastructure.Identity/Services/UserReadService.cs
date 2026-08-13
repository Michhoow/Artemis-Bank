using ArtemisBank.Core.Application.Dtos.Common;
using ArtemisBank.Core.Application.Interfaces.Contracts;
using ArtemisBank.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBank.Infrastructure.Identity.Services
{
    /// <summary>
    /// Implementación definitiva de <see cref="IUserReadService"/>.
    /// Sustituye automáticamente a <c>PendingUserReadService</c> de la capa Application
    /// porque se registra DESPUÉS de <c>AddApplicationLayerIoc()</c> en Program.cs.
    /// Solo lectura — no modifica datos de Identity.
    /// </summary>
    public class UserReadService : IUserReadService
    {
        private readonly UserManager<AppUser> _userManager;

        public UserReadService(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        /// <inheritdoc/>
        public async Task<UserInfoDto?> GetByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return null;
            return await ToDto(user);
        }

        /// <inheritdoc/>
        public async Task<UserInfoDto?> GetByIdentificationAsync(string identification)
        {
            var user = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Identification == identification);
            if (user is null) return null;
            return await ToDto(user);
        }

        /// <inheritdoc/>
        public async Task<List<UserInfoDto>> GetByIdsAsync(IEnumerable<string> userIds)
        {
            var ids = userIds.ToHashSet(StringComparer.Ordinal);
            var users = await _userManager.Users
                .AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .ToListAsync();

            var dtos = new List<UserInfoDto>(users.Count);
            foreach (var u in users)
                dtos.Add(await ToDto(u));
            return dtos;
        }

        /// <inheritdoc/>
        public async Task<List<UserInfoDto>> GetClientsAsync(bool onlyActive = true)
        {
            // Obtiene todos los usuarios del rol Cliente y aplica el filtro de activo
            var clientIds = await GetUserIdsByRoleAsync("Cliente");

            var query = _userManager.Users
                .AsNoTracking()
                .Where(u => clientIds.Contains(u.Id));

            if (onlyActive)
                query = query.Where(u => u.IsActive);

            var users = await query.ToListAsync();
            var dtos = new List<UserInfoDto>(users.Count);
            foreach (var u in users)
                dtos.Add(await ToDto(u));
            return dtos;
        }

        /// <inheritdoc/>
        public async Task<int> CountClientsAsync(bool? isActive = null)
        {
            var clientIds = await GetUserIdsByRoleAsync("Cliente");

            var query = _userManager.Users
                .AsNoTracking()
                .Where(u => clientIds.Contains(u.Id));

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            return await query.CountAsync();
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetActiveClientIdsAsync()
        {
            var clientIds = await GetUserIdsByRoleAsync("Cliente");

            return await _userManager.Users
                .AsNoTracking()
                .Where(u => clientIds.Contains(u.Id) && u.IsActive)
                .Select(u => u.Id)
                .ToListAsync();
        }

        // ── Helpers privados ────────────────────────────────────────────────

        private async Task<UserInfoDto> ToDto(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return new UserInfoDto
            {
                Id             = user.Id,
                FirstName      = user.FirstName,
                LastName       = user.LastName,
                Identification = user.Identification,
                Email          = user.Email ?? string.Empty,
                UserName       = user.UserName ?? string.Empty,
                Role           = roles.FirstOrDefault() ?? string.Empty,
                IsActive       = user.IsActive
            };
        }

        private async Task<HashSet<string>> GetUserIdsByRoleAsync(string roleName)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
            return usersInRole.Select(u => u.Id).ToHashSet(StringComparer.Ordinal);
        }
    }
}
