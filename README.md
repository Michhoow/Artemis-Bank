# Artemis Banking Pro — Parte de Michael

Cuentas de ahorro, transacciones, cajero, reglas financieras, Home del administrador y base de datos.
**76 criterios de rúbrica · 1520 puntos · 34.1 % de la carga.**

Rama de trabajo: `feature/mic-*` → PR a `develop`. Revisora principal: **Monserrat**.

---

## 1. Cómo ejecutar

```bash
# Restaurar y compilar
dotnet restore
dotnet build

# WebApp  → https://localhost:7101
dotnet run --project ArtemisBank.WebApp

# WebApi  → https://localhost:7201/swagger
dotnet run --project ArtemisBank.WebApi

# Pruebas
dotnet test
```

En `Development` la solución arranca con **InMemory + datos de demostración**, así que corre sin
SQL Server. Para usar SQL Server, ponga `UseInMemoryDatabase: false` en `appsettings.Development.json`
y genere la base:

```bash
dotnet ef migrations add InitialCreate -p ArtemisBank.Infrastructure.Persistence -s ArtemisBank.WebApp
dotnet ef database update -p ArtemisBank.Infrastructure.Persistence -s ArtemisBank.WebApp
```

> **La carpeta `Migrations` tiene un solo dueño: Michael.** Nadie más ejecuta `migrations add`.
> Es el conflicto de Git más caro del proyecto y por eso está centralizado.

Mientras el login real no exista, `EnableDevLogin: true` habilita un selector de usuario de prueba
en `/Login` para entrar como administrador, cajero o cliente. **Se elimina al integrar Identity.**

---

## 2. Arquitectura

Onion, calcada de la estructura de referencia de `RealEstateApp` y extendida con CQRS + Mediator.

```
ArtemisBank.Core.Domain                 entidades, enums, interfaces de repositorio (sin dependencias)
ArtemisBank.Core.Application            CQRS, servicios, DTOs, ViewModels, AutoMapper, FluentValidation, Behaviors
ArtemisBank.Infrastructure.Persistence            DbContext, configurations, repositorios, UnitOfWork, migraciones   ← Michael
ArtemisBank.Infrastructure.Identity  Identity, JWT, seeding                                          ← Monserrat
ArtemisBank.Infrastructure.Shared    correo, cripto                                                  ← compartido
ArtemisBank.WebApp                 MVC, controladores, vistas Razor + Bootstrap 5
ArtemisBank.WebApi                 REST, Swagger, versionado, Global Exception Handler
ArtemisBank.Tests                  xUnit: unitarias + integración
```

Dirección de dependencias: `WebApp/WebApi → Infrastructure → Application → Domain`.
El dominio no referencia nada; la aplicación no conoce EF más allá de `IQueryable`.

---

## 3. Contratos congelados con el equipo

Michael **no creó ninguna entidad de Manuel ni de Monserrat.** Consume interfaces y sus DTOs:

| Contrato | Lo implementa | Lo usa Michael para |
|---|---|---|
| `IUserReadService` | Monserrat (Identity) | Nombres y cédulas en listados, clientes activos/inactivos, destinatarios de correo |
| `IAuthenticatedUser` | ya implementado en cada host | Asociar cada operación al responsable |
| `ILoanReadService` | Manuel (préstamos) | Pago a préstamo, indicadores, unicidad del número de 9 dígitos |
| `ICreditCardReadService` | Manuel (tarjetas) | Pago a tarjeta, indicadores, deuda por cliente |
| `IEmailService` | Infrastructure.Shared | Notificaciones transaccionales |

Para que la parte de Michael corra sola hoy, `Application/Services/Pending` trae implementaciones
puente. **En el contenedor de .NET gana el último registro**, así que en `Program.cs` las capas de
Identity y de los módulos de crédito se registran *después* de `AddApplicationLayerIoc()` y sustituyen
los puentes automáticamente. Al integrar, borrar `Services/Pending` y `Persistence/Seeds`.

Manuel también puede reutilizar la trazabilidad de Michael para el desembolso de préstamos, el avance
de efectivo y la acreditación de Hermes Pay, con
`ITransactionService.RegisterExternalCreditAsync` / `RegisterExternalDebitAsync`.

---

## 4. Decisiones de diseño que conviene conocer

**`OperationReference`.** Cada operación de negocio genera un correlativo compartido por sus dos
patas. Una transferencia escribe dos filas en el libro mayor pero cuenta como **una** transacción en
los indicadores. Esto también da la traza cruzada exigida por la rúbrica.

**Conteo de indicadores.** El documento dice transacciones *"registradas"* y pagos *"procesados
correctamente"*, así que se implementó literal: las transacciones incluyen aprobadas y rechazadas;
los pagos solo aprobados y solo de tipo `PagoTarjeta` o `PagoPrestamo`.

**Regla anti sobrepago.** El monto efectivo es siempre `min(monto digitado, deuda real)`. El excedente
nunca se debita ni se registra.

**Rechazos.** Se persisten fuera de la transacción de negocio: quedan en el historial de la cuenta de
origen y jamás tocan un balance.

**Correos.** Se envían después del commit. Un fallo devuelve un aviso al usuario pero nunca revierte.

**`decimal(18,2)` global.** El `OnModelCreating` recorre el modelo y fuerza precisión y escala en toda
propiedad decimal, sea de quien sea. Números de cuenta y de préstamo son texto de longitud fija.

---

## 5. Mapeo a la rúbrica

| Sección | Criterios | Dónde está |
|---|---|---|
| Home del administrador | 8 | `AdminHomeService`, `AdminHomeController`, `Views/AdminHome` |
| Gestión de cuentas de ahorro WebApp | 10 | `SavingsAccountService`, `SavingsAccountController`, `Views/SavingsAccount` |
| Funcionalidades del cliente | 8 | `ClientHomeController`, `TransactionController`, `TransferController`, `BeneficiaryService` |
| Funcionalidades del cajero | 10 | `CashierController`, `CashierHomeService`, `Views/Cashier` |
| API: cuentas de ahorro | 6 | `WebApi/Controllers/v1/SavingsAccountController` + `Features/SavingsAccounts` |
| Reglas financieras y trazabilidad | 8 | `TransactionService`, `TransactionEntityConfiguration` |
| Reglas técnicas y arquitectura | 5 | Onion, EF Code First, ViewModels, DTOs, AutoMapper, repos y servicios genéricos |
| CQRS, Mediator, Behaviors | 3 | `Features/SavingsAccounts`, `Behaviors/` |
| Validación de servicios por módulo | 3 | Servicios de cuentas, transacciones y cajero + sus pruebas |
| Documentación, excepciones y logs | 3 | `GlobalExceptionHandler` (Problem Details), Serilog en ambos hosts |
| Pruebas unitarias — Commands y Queries | 2 | `Tests/Unit/Features` |
| Pruebas unitarias — servicios | 3 | `Tests/Unit/Services` |
| Pruebas de integración | 4 | `Tests/Integration` (SQLite en memoria) |
| Calidad final y entrega | 3 | Migraciones, seed, appsettings por ambiente |

---

## 6. Endpoints de la API (módulo de Michael)

| Método | Ruta | Respuestas |
|---|---|---|
| GET | `/api/savings-account` | 200 · 400 · 401 · 403 |
| POST | `/api/savings-account` | 201 · 400 · 401 · 403 · 404 · 409 |
| GET | `/api/savings-account/{accountNumber}/transactions` | 200 · 400 · 401 · 403 · 404 |
| PATCH | `/api/savings-account/{accountNumber}/cancel` | 204 · 400 · 401 · 403 · 404 |

Todos exigen JWT y rol `Administrador`. Los errores salen en Problem Details (RFC 7807) con
`correlationId`.

---

## 7. Pendiente al integrar

1. Borrar `Application/Services/Pending` y `Persistence/Seeds/SavingsAccountSeed.cs`.
2. Borrar `DevSignIn` de `LoginController` y `EnableDevLogin` de los appsettings.
3. Alinear `SavingsAccountSeed` (o eliminarlo) con los Id reales del seeding de Identity.
4. Añadir los `DbSet` de Manuel y Monserrat en `ArtemisDbContext` y generar la migración.
5. Mover claves SMTP y JWT a *user-secrets*: hoy están vacías en `appsettings.json` a propósito.
