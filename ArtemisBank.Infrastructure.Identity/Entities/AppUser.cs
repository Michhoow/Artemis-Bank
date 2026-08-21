using Microsoft.AspNetCore.Identity;

namespace ArtemisBank.Infrastructure.Identity.Entities
{
    public class AppUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Identification { get; set; } = string.Empty;

        public bool IsActive { get; set; } = false;

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
