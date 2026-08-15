using ArtemisBank.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ArtemisBank.Infrastructure.Identity.Contexts
{
    /// <summary>
    /// DbContext exclusivo de ASP.NET Identity.
    /// Vive en el esquema <c>Identity</c> para no contaminar el esquema principal de negocio
    /// que administra Michael (<see cref="ArtemisDbContext"/>).
    /// Las migraciones de este contexto se generan con:
    ///   dotnet ef migrations add Init --context IdentityContext
    ///     --project ArtemisBank.Infrastructure.Identity
    ///     --startup-project ArtemisBank.WebApp
    ///     --output-dir Migrations
    /// </summary>
    public class IdentityContext : IdentityDbContext<AppUser>
    {
        public IdentityContext(DbContextOptions<IdentityContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Todas las tablas de Identity van al esquema "Identity"
            // para separación limpia con el esquema dbo (negocio de Michael).
            builder.HasDefaultSchema("Identity");

            // Renombrar tablas a nombres legibles (sin el prefijo AspNet)
            builder.Entity<AppUser>().ToTable("Users");
            builder.Entity<IdentityRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

            // Índice de unicidad sobre Identification (cédula / RNC)
            builder.Entity<AppUser>()
                .HasIndex(u => u.Identification)
                .IsUnique()
                .HasDatabaseName("IX_Users_Identification");
        }
    }
}
