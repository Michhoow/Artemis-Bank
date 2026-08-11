# Auditoría — parte de Michael, Artemis Banking Pro

Contraste contra (1) la arquitectura de `RealEstateApp` y (2) los requerimientos técnicos y los
criterios de la rúbrica. Incluye los defectos encontrados y su corrección.

---

## A. Paridad arquitectónica con RealEstateApp

| Aspecto de la referencia | Antes | Ahora |
|---|---|---|
| `*.Core.Domain` | `ArtemisBank.Domain` | `ArtemisBank.Core.Domain` ✔ |
| `*.Core.Application` | `ArtemisBank.Application` | `ArtemisBank.Core.Application` ✔ |
| `*.Infrastructure.Persistence` | `ArtemisBank.Persistence` | `ArtemisBank.Infrastructure.Persistence` ✔ |
| `*.Infrastructure.Identity` / `.Shared` | ✔ | ✔ |
| `Domain/Settings/{Jwt,Mail}Settings.cs` | MailSettings vivía en Shared | movido a `Core.Domain/Settings` ✔ |
| `Domain/{Entities,Interfaces,Common/Enums}` | ✔ | ✔ |
| `IGenericRepository<T>` + `GenericRepository<T>` | ✔ | ✔ |
| `IGenericService<TDto>` + `GenericService<TEntity,TDto>` | declarado pero **mal registrado** | corregido ✔ |
| Servicio de módulo hereda del genérico | no heredaba | `SavingsAccountService : GenericService<SavingsAccount, SavingsAccountDto>, ISavingsAccountService` ✔ |
| `ServicesRegistration.cs` por capa (extension method) | ✔ | ✔ |
| `Persistence/{Contexts,EntityConfigurations,Repositories}` | ✔ | ✔ |
| `Application/Mappings/{EntitiesAndDtos,DtosAndViewModels}` | perfiles sueltos | reorganizados ✔ |
| `WebApi/{Controllers/v1, Extensions/ServiceExtension, Extensions/AppExtension, BaseApiController}` | ✔ | ✔ |
| `WebApp/Models/ErrorViewModel.cs` | usaba ViewBag | ViewModel tipado ✔ |
| `Views/{Shared/_Layout,_ViewImports,_ViewStart,_ValidationScriptsPartial}` | ✔ | ✔ |

**Desviación consciente:** los hosts se llaman `ArtemisBank.WebApp` y `ArtemisBank.WebApi`
(la referencia usa `RealEstateApp` y `RealEstateApi`). Se mantuvo porque es lo que fija el
Plan Maestro del equipo y es el nombre que Monserrat y Manuel ya asumen en sus rutas de archivo.

**Verificado por script:** 0 violaciones de dirección de dependencias de Onion, 0 namespaces
incoherentes con su proyecto, 0 referencias a EF Core dentro de Domain, 0 `View("X")` sin vista,
0 `asp-action` apuntando a acciones inexistentes.

---

## B. Defectos encontrados y corregidos

| # | Severidad | Defecto | Corrección |
|---|---|---|---|
| 1 | **Bloqueante** | `services.AddScoped(typeof(IGenericService<>), typeof(GenericService<,>))`: aridad incompatible (1 vs 2 parámetros de tipo). Lanzaba `ArgumentException` al arrancar y tumbaba WebApp y WebApi. | Registro eliminado; el genérico se consume por herencia, exactamente como en la referencia. |
| 2 | **Bloqueante** | `new MapperConfiguration(cfg => ...)` en las pruebas: AutoMapper 14 exige `ILoggerFactory`. No compilaba el proyecto de tests. | Se pasa `NullLoggerFactory.Instance` y se agregó la referencia explícita a `Microsoft.Extensions.Logging.Abstractions`. |
| 3 | **Alta** | `RunPersistenceMigrationsAsync` llamaba `MigrateAsync()` siempre. Sin la migración inicial generada, creaba una base **vacía** con solo `__EFMigrationsHistory` y la app arrancaba contra un esquema inexistente. | Detecta si hay migraciones: si no las hay, cae a `EnsureCreatedAsync()`. |
| 4 | **Alta** | Serilog no registraba usuario, rol, endpoint ni identificador de correlación, exigidos de forma explícita por el requerimiento técnico de logs. | `RequestAuditMiddleware` en ambos hosts, insertado después de `UseAuthentication` para que los claims ya estén disponibles. Devuelve `X-Correlation-Id`. |
| 5 | Media | `ISavingsAccountService.GetByIdAsync(int, CancellationToken)` colisionaba con el del genérico al heredar. | Se eliminó el duplicado; ahora es un `override` que además completa nombre y cédula del titular. |
| 6 | Media | Al heredar del genérico, `AddAsync`/`UpdateAsync`/`DeleteAsync` quedaban expuestos y permitían crear una cuenta o mover un balance **saltándose** las reglas de negocio y el registro contable. | Los tres se sobrescriben y lanzan `BusinessRuleException` con el camino correcto (`CreateSecondaryAsync`, `ITransactionService`, cancelación). |
| 7 | Baja | `MailSettings` fuera de `Domain/Settings`, a diferencia de la referencia. | Movido. |
| 8 | Baja | `ErrorViewModel` inexistente; la vista de error usaba `ViewBag`. | Creado y tipado. |

---

## C. Requerimientos técnicos

| Requerimiento | Estado | Dónde |
|---|---|---|
| ViewModels en la capa de presentación con validaciones del framework | ✔ | `Core.Application/ViewModels/**` con DataAnnotations |
| EF Core Code First | ✔ | `ArtemisDbContext` + `EntityConfigurations` |
| Bootstrap | ✔ | Bootstrap 5.3 + Bootstrap Icons en `_Layout` |
| Onion 100 % consistente | ✔ | verificado por script |
| Repositorios genéricos **y** servicios genéricos | ✔ | `GenericRepository<T>`, `GenericService<TEntity,TDto>` |
| Servicios usados por los controladores de la WebApp | ✔ | ningún controlador toca repositorios ni `DbContext` |
| Identity / cookies / JWT | ⚠ Monserrat | esquemas configurados en `Infrastructure.Identity`; contratos ya consumidos |
| AutoMapper entre entidades, ViewModels y DTOs | ✔ | `Mappings/EntitiesAndDtos`, `Mappings/DtosAndViewModels` |
| CQRS + Mediator en endpoints de la API | ✔ | `Features/SavingsAccounts/{Commands,Queries}` |
| Behaviors + FluentValidation | ✔ | `ValidationBehavior`, `LoggingBehavior` |
| Swagger documentado | ✔ | `ServiceExtension` con seguridad JWT y XML comments |
| Global Exception Handler + Problem Details (API **y** WebApp) | ✔ | `WebApi/Middleware`, `WebApp/Common` |
| xUnit: Commands/Queries y servicios | ✔ | `Tests/Unit/**` |
| Pruebas de integración de repositorios | ✔ | `Tests/Integration` sobre SQLite en memoria |
| Serilog en WebApp y WebApi con datos de auditoría | ✔ | tras la corrección #4 |
| Sin datos sensibles en logs, respuestas, vistas ni correos | ✔ | tarjetas siempre por últimos 4; el número completo nunca sale del módulo de tarjetas |
| `decimal` con precisión de centavos | ✔ | `HasPrecision(18,2)` + barrido global en `OnModelCreating` |

---

## D. Criterios de rúbrica de Michael (76)

| Sección | Crit. | Estado |
|---|---|---|
| Home del administrador | 8 | ✔ los 11 indicadores, cálculo de deuda promedio y RD$0.00 sin clientes activos |
| Gestión de cuentas de ahorro WebApp | 10 | ✔ paginación de 20, filtros, número único de 9 dígitos como texto, crédito inicial, cancelación con traslado y registro cruzado, bloqueo de canceladas |
| Funcionalidades del cliente | 8 | ✔ Home, detalles, beneficiarios, express, pago a tarjeta y préstamo sin sobrepago, transacción a beneficiario, transferencia propia, correos |
| Funcionalidades del cajero | 10 | ✔ indicadores del día del cajero autenticado, depósito, retiro, ambos pagos, terceros, rechazos registrados, confirmaciones previas |
| API: cuentas de ahorro | 6 | ✔ los 4 endpoints con 200/201/204/400/401/403/404/409 |
| Reglas financieras y trazabilidad | 8 | ✔ DÉBITO/CRÉDITO, registro cruzado, anti sobrepago, ejecución transaccional, historial conservado, decimal(18,2) |
| Reglas técnicas y arquitectura | 5 | ✔ |
| CQRS, Mediator, Behaviors | 3 | ✔ |
| Validación de servicios por módulo | 3 | ✔ cuentas, transacciones y cajero con pruebas |
| Documentación, excepciones y logs | 3 | ✔ tras la corrección #4 |
| Pruebas unitarias — Commands y Queries | 2 | ✔ handlers y validadores |
| Pruebas unitarias — servicios de negocio | 3 | ✔ cuentas, balances, transferencias, depósitos, retiros, pagos |
| Pruebas de integración | 4 | ✔ persistencia, índices únicos, ceros a la izquierda, centavos, atomicidad ante fallo |
| Calidad final, entrega y ejecución | 3 | ⚠ falta ejecutar `dotnet ef migrations add InitialCreate` (ver E) |

---

## E. Lo único que queda abierto

**La migración inicial no está generada.** El contenedor donde se construyó esto no tiene el SDK de
.NET, así que no pude ejecutar `dotnet ef`. Es un comando y queda cubierto:

```bash
dotnet ef migrations add InitialCreate \
  -p ArtemisBank.Infrastructure.Persistence -s ArtemisBank.WebApp
dotnet ef database update \
  -p ArtemisBank.Infrastructure.Persistence -s ArtemisBank.WebApp
```

Preferí no escribir a mano el `ModelSnapshot`: una migración mal generada es peor que ninguna,
porque rompe en tiempo de ejecución y arrastra al resto del equipo. Mientras tanto el arranque
cae a `EnsureCreated` y la solución corre.

**Tampoco pude compilar.** El código está revisado línea por línea y verificado con scripts de
consistencia, pero espere algún ajuste menor en el primer `dotnet build`.

**Enlaces del menú a módulos ajenos.** `Gestión de usuarios`, `Gestión de préstamos`,
`Gestión de tarjetas` y `Avance de efectivo` apuntan a controladores que aún no existen: hasta que
Monserrat y Manuel los agreguen, esos enlaces se renderizan vacíos. Es el comportamiento correcto
para la integración; no requiere cambio.

---

## F. Correcciones tras la primera compilación real (14 errores)

La primera compilación en Visual Studio dio 14 errores, concentrados en solo 2 de los 8 proyectos.
`Core.Domain`, `Core.Application`, `Infrastructure.Persistence`, `Infrastructure.Shared`,
`Infrastructure.Identity` y `WebApi` compilaron limpios.

Tres causas raíz:

| Causa | Errores | Archivos | Corrección |
|---|---|---|---|
| **Yo me equivoqué sobre AutoMapper 14**: dije que `MapperConfiguration` exigía un `ILoggerFactory` y le pasé un segundo argumento. La versión 14.0.0 tiene el constructor de un solo argumento. | CS1729 | `Tests/Common/ServiceBuilder.cs` | Vuelto al constructor de un argumento. |
| **Secuela del renombrado de proyectos**: había referencias calificadas como `Application.Dtos.…` sin el prefijo `ArtemisBank.`. Antes resolvían porque existía el namespace `ArtemisBank.Application`; al pasar a `ArtemisBank.Core.Application` dejaron de resolver. | CS0246, CS1929, CS8130, CS8183 | `Tests/Unit/Services/SavingsAccountServiceTests.cs` | `using` de `Dtos.Common` y `Dtos.SavingsAccounts`, y nombres cortos. |
| **Misma secuela** en `Application.Services.Pending.DemoDirectory`. Al fallar esa línea, `demo` quedaba como tipo de error y el compilador reportaba las 3 líneas siguientes contra la sobrecarga `Claim(BinaryReader)` — de ahí los CS1503, que eran errores derivados, no independientes. | CS0103, CS1503 ×3 | `WebApp/Controllers/LoginController.cs` | `using ArtemisBank.Core.Application.Services.Pending;` y `DemoDirectory.Users`. |

**Verificación posterior:** barrido de toda la solución buscando referencias a namespaces propios sin
el prefijo `ArtemisBank.` (0 resultados) y validación de que cada `using ArtemisBank.*`, cada tipo
calificado y cada `@model` de las vistas apunta a un namespace realmente declarado (0 resultados
reales; 5 falsos positivos del script, revisados a mano).

**Lección para el equipo:** el renombrado masivo de proyectos solo alcanza las referencias que llevan
el prefijo completo. Las calificaciones parciales que se apoyaban en la resolución por namespace
padre quedan mudas hasta que compila. Si vuelven a renombrar algo, compilen antes de hacer push.
