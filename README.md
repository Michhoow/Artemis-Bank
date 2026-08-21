# Artemis Banking Pro (ABP)

Sistema bancario construido con ASP.NET Core sobre .NET 9, siguiendo arquitectura Onion. Incluye una aplicación web MVC para administradores, cajeros y clientes, y una Web API REST para administradores y comercios, con el procesador de pagos Hermes Pay.

Proyecto final de Programación 3.

---

## Integrantes

| Nombre | Matrícula |
|---|---|
| J. Michael Martinez Almonte | 2024-2440 |
| Montserrat Margarita Nunez De Leon | 2025-1379 |
| Manuel Antonio Medrano Ortiz | 2024-2398 |

---

## Funcionalidades

**Administrador** — Indicadores del sistema, gestión de usuarios, préstamos, tarjetas de crédito y cuentas de ahorro.

**Cajero** — Indicadores del día, depósitos, retiros, pagos a tarjeta y préstamo, y transacciones a cuentas de terceros.

**Cliente** — Consulta de productos financieros, beneficiarios, transacción express, pagos, avance de efectivo y transferencia entre cuentas propias.

**Comercio** — Cobros mediante Hermes Pay y consulta de sus transacciones, exclusivamente a través de la Web API.

### Reglas de negocio implementadas

- Amortización por sistema francés, con tabla de cuotas y control de mora
- Evaluación de riesgo del cliente antes de aprobar un préstamo
- CVC almacenado como hash SHA-256; el número de tarjeta nunca se expone completo
- Avance de efectivo con interés del 6.25 %
- Operaciones transaccionales atómicas con registro cruzado de débito y crédito
- Los intentos rechazados quedan registrados sin alterar los balances

---

## Arquitectura


**Stack:** .NET 9, ASP.NET Core MVC y Web API, Entity Framework Core (Code First), ASP.NET Identity, JWT, MediatR, FluentValidation, AutoMapper, Serilog, Swagger, xUnit.

---

## Requisitos

- SDK de .NET 9
- SQL Server LocalDB o una instancia de SQL Server

---

## Cómo ejecutar

```bash
dotnet restore
dotnet build
```

Aplicar las migraciones de ambos contextos:

```bash
dotnet ef database update -c ArtemisDbContext \
  -p ArtemisBank.Infrastructure.Persistence -s ArtemisBank.WebApp

dotnet ef database update -c IdentityContext \
  -p ArtemisBank.Infrastructure.Identity -s ArtemisBank.WebApp
```

Si `dotnet ef` no está disponible:

```bash
dotnet tool install --global dotnet-ef
```

Ejecutar cada aplicación en su propia terminal:

```bash
dotnet run --project ArtemisBank.WebApp
dotnet run --project ArtemisBank.WebApi
```

| Aplicación | Dirección |
|---|---|
| Aplicación web | https://localhost:7101 |
| Web API (Swagger) | https://localhost:7201/swagger |

Los roles y usuarios de prueba se crean automáticamente al iniciar la aplicación web.

---

## Usuarios de prueba

El nombre de usuario es el correo electrónico completo.

| Rol | Usuario | Contraseña | Acceso |
|---|---|---|---|
| Administrador | `admin@artemisbank.do` | `Admin@12345!` | Web y API |
| Cajero | `cajero@artemisbank.do` | `Cajero@12345!` | Web |
| Cliente | `cliente@artemisbank.do` | `Cliente@12345!` | Web |
| Comercio | `comercio@artemisbank.do` | `Comercio@12345!` | Solo API |

El rol Comercio no inicia sesión en la aplicación web. Según el documento funcional, ese rol es exclusivo de la Web API y del procesador Hermes Pay.

### Autenticación en la Web API

1. Ejecutar `POST /api/v1/Account/login` con las credenciales.
2. Copiar el valor del campo `token` de la respuesta.
3. Pulsar **Authorize** en Swagger e ingresar `Bearer {token}`.

El token incluye identificador de usuario, nombre de usuario, rol y expiración a las dos horas.

---

## Pruebas

```bash
dotnet test
```

454 pruebas unitarias y de integración que cubren los 30 handlers CQRS de los siete módulos, los servicios de negocio, los validadores y los repositorios de persistencia e Identity.

---

## Configuración

La cadena de conexión y el resto de parámetros se definen en `appsettings.json` y `appsettings.Development.json` de cada aplicación.

El envío de correos requiere configurar la sección `MailSettings`. Por seguridad, el repositorio no incluye credenciales reales. Con `MailSettings` sin configurar, la aplicación funciona con normalidad y los envíos quedan registrados en el log.

Los usuarios creados desde la aplicación nacen inactivos y requieren activación por correo. Un administrador también puede activarlos manualmente desde Gestión de usuarios.
