using ArtemisBank.Core.Application.Dtos.Common;

namespace ArtemisBank.Core.Application.Services.Pending
{
    /// <summary>
    /// TEMPORAL — directorio de usuarios de demostracion.
    ///
    /// Existe unicamente para que los modulos de Michael (cuentas, transacciones, cajero,
    /// home del administrador) se puedan ejecutar y probar ANTES de que Monserrat integre
    /// ASP.NET Identity. En cuanto la capa Identity registre su IUserReadService real,
    /// esta implementacion queda sin uso y este archivo debe eliminarse.
    ///
    /// Los Id coinciden con los que usa el seeding de cuentas de ahorro de Persistence.
    /// </summary>
    public static class DemoDirectory
    {
        public const string AdminId = "demo-admin-0001";
        public const string CashierId = "demo-cajero-0001";
        public const string ClientOneId = "demo-cliente-0001";
        public const string ClientTwoId = "demo-cliente-0002";
        public const string ClientInactiveId = "demo-cliente-0003";

        public static readonly List<UserInfoDto> Users = new()
        {
            new UserInfoDto { Id = AdminId, FirstName = "Ana", LastName = "Suárez", Identification = "00100000001",
                Email = "admin@artemisbank.do", UserName = "admin", Role = "Administrador", IsActive = true },
            new UserInfoDto { Id = CashierId, FirstName = "Luis", LastName = "Peña", Identification = "00100000002",
                Email = "cajero@artemisbank.do", UserName = "cajero", Role = "Cajero", IsActive = true },
            new UserInfoDto { Id = ClientOneId, FirstName = "María", LastName = "Gómez", Identification = "00187654321",
                Email = "maria@artemisbank.do", UserName = "mgomez", Role = "Cliente", IsActive = true },
            new UserInfoDto { Id = ClientTwoId, FirstName = "Pedro", LastName = "Martínez", Identification = "00112345678",
                Email = "pedro@artemisbank.do", UserName = "pmartinez", Role = "Cliente", IsActive = true },
            new UserInfoDto { Id = ClientInactiveId, FirstName = "Carla", LastName = "Reyes", Identification = "00199999999",
                Email = "carla@artemisbank.do", UserName = "creyes", Role = "Cliente", IsActive = false }
        };
    }
}
