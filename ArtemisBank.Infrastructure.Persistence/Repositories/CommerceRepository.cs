using ArtemisBank.Core.Domain.Entities;
using ArtemisBank.Core.Domain.Interfaces;
using ArtemisBank.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBank.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Repositorio de EF Core para la entidad <see cref="Commerce"/>.
    /// Propiedad: Monserrat.
    /// </summary>
    public class CommerceRepository : GenericRepository<Commerce>, ICommerceRepository
    {
        public CommerceRepository(ArtemisDbContext dbContext) : base(dbContext) { }

        public async Task<Commerce?> GetByRncAsync(string rnc, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Commerces
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Rnc == rnc, cancellationToken);
        }

        public async Task<Commerce?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Commerces
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        }

        public async Task<bool> RncExistsAsync(string rnc, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Commerces
                .AnyAsync(c => c.Rnc == rnc, cancellationToken);
        }

        public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _dbContext.Commerces
                .AnyAsync(c => c.Email == email, cancellationToken);
        }
    }
}
