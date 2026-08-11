namespace ArtemisBank.Core.Application.Dtos.Common
{
    /// <summary>
    /// CONTRATO CONGELADO — proyeccion de solo lectura del usuario de Identity.
    /// Provee: Monserrat (Infrastructure.Identity). Consume: Michael y Manuel.
    /// </summary>
    public class UserInfoDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Identification { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public string FullName => (FirstName + " " + LastName).Trim();
    }
}
