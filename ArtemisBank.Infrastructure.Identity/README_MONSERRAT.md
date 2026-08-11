# ArtemisBank.Infrastructure.Identity — pendiente (Monserrat)

Este proyecto está creado y referenciado por WebApp y WebApi, pero **su contenido lo implementa
Monserrat**. Michael solo dejó el esqueleto y el punto de registro para no bloquear la integración.

## Lo que debe contener

- `Entities/AppUser.cs` (hereda de `IdentityUser`, con `FirstName`, `LastName`, `Identification`, `IsActive`)
- `Contexts/IdentityContext.cs` (DbContext propio, esquema `Identity`)
- `Services/AccountServiceForWebApp.cs`, `AccountServiceForWebApi.cs`, `JwtService.cs`
- `Seeds/DefaultRoles.cs` y usuarios por defecto activos
- `Migrations/**` (migraciones propias del contexto de Identity)
- `ServicesRegistration.cs` con `AddIdentityLayerIocForWebApp` / `AddIdentityLayerIocForWebApi`

## Contratos que Michael ya consume y que Identity debe implementar

| Contrato | Ubicación | Para qué lo usa Michael |
|---|---|---|
| `IUserReadService` | `ArtemisBank.Core.Application/Interfaces/Contracts` | Nombres y cédulas en el listado de cuentas, clientes activos e inactivos del Home admin, destinatarios de correo |
| `IAuthenticatedUser` | `ArtemisBank.Core.Application/Interfaces` | Asociar cada operación al cliente/cajero/administrador responsable |

`IAuthenticatedUser` ya tiene implementación en cada host (`HttpContextAuthenticatedUser`), leyendo
los claims estándar. Si el JWT usa claims propios, basta con ajustar esa clase.

## Registro

En `Program.cs` la capa Identity se registra **después** de `AddApplicationLayerIoc()`.
Como el contenedor de .NET resuelve el último registro de un mismo servicio, la implementación real
de `IUserReadService` sustituye automáticamente al puente temporal
`ArtemisBank.Core.Application/Services/Pending/PendingUserReadService`.

Cuando eso ocurra, borrar la carpeta `Services/Pending` completa y el seed de demostración
`ArtemisBank.Infrastructure.Persistence/Seeds/SavingsAccountSeed.cs` (o alinear sus `ClientId` con los reales).

## Cuenta principal automática

Al crear un usuario con rol `Cliente`, Identity debe crear su cuenta de ahorro **principal**.
Para no duplicar la lógica de generación de números únicos de 9 dígitos, use lo que ya existe:

```csharp
// Inyecte IAccountNumberGenerator (Application) y el repositorio de cuentas.
var accountNumber = await _accountNumberGenerator.GenerateAsync(cancellationToken);

await _savingsAccountRepository.AddAsync(new SavingsAccount
{
    AccountNumber = accountNumber,
    ClientId      = user.Id,
    Balance       = montoInicial,          // puede ser 0.00
    Type          = AccountType.Principal, // Michael nunca crea principales
    Status        = AccountStatus.Activa,
    CreatedAt     = DateTime.Now,
    CreatedByUserId = adminId
});
```

Si `montoInicial > 0`, registre además el `CRÉDITO` inicial usando
`ITransactionService.RegisterExternalCreditAsync`, para que el movimiento quede con la misma
trazabilidad que el resto del sistema.
