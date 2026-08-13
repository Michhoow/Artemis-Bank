# ARTEMIS BANKING PRO · ABP
## Plan Maestro de Distribución de Trabajo
*División equitativa, medible y sin ambigüedades del proyecto entre Monserrat, Michael y Manuel.*

---

## Stack y Arquitectura General
* **Stack Tecnológico:** ASP.NET Core MVC + Web API (.NET 9)
* **Arquitectura:** Onion Architecture · CQRS · EF Core
* **Rúbrica Total:** 223 criterios · 4460 puntos
* **Equipo:** 3 desarrolladores (Monserrat, Michael, Manuel)

---

## Resumen Ejecutivo

### Un proyecto, tres dueños, cero huérfanos
Este plan convierte el documento funcional de **Artemis Banking Pro** y su rúbrica de evaluación en un plan de ejecución donde cada requisito y cada punto de la rúbrica tiene un responsable y un revisor. La distribución sigue módulos cohesionados (*vertical slices*) para minimizar dependencias cruzadas y conflictos de Git.

**Documentos analizados:**
1. **Documento Funcional Artemis Banking Pro v1.0:** WebApp con roles Administrador/Cajero/Cliente, Web API con JWT y roles Administrador/Comercio, y el procesador Hermes Pay.
2. **Rúbrica de Evaluación:** 223 criterios de 20 puntos cada uno (4460 pts totales) agrupados en 25 secciones que cubren funcionalidad, seguridad, arquitectura, CQRS, documentación y pruebas.

*Nota:* No proporcionado: fecha de entrega y habilidades individuales del equipo → se usa una distribución neutral por complejidad y un cronograma relativo por días.

### Criterio de equidad y carga ponderada
No se reparte por igual número de tareas, sino por **carga ponderada** = *Esfuerzo (30%) + Complejidad (20%) + Riesgo (15%) + Dependencias (10%) + Rúbrica (15%) + Pruebas (10%)*. Manuel recibe menos criterios en bruto porque los suyos (amortización francesa, SHA-256, Hermes Pay, Azure Functions) son de mayor complejidad y riesgo por tarea. Monserrat concentra la fundación de la que todos dependen.

| Integrante | Tareas (Criterios Rúbrica) | Puntos Rúbrica | Carga Ponderada (%) | Revisor Principal |
| :--- | :---: | :---: | :---: | :--- |
| **Monserrat** | 75 | 1500 | **33.6%** | Manuel |
| **Michael** | 76 | 1520 | **34.1%** | Monserrat |
| **Manuel** | 72 | 1440 | **32.3%** | Michael |
| **TOTAL** | **223** | **4460** | **100.0%** | |

> **Veredicto de equidad:** Diferencia máxima entre integrantes = **1.8%** (muy por debajo del ±5% recomendado). Cada persona tiene implementación real, pruebas, documentación y revisión.

---

## Arquitectura y Distribución General

### Onion Architecture y propiedad por capas
La solución sigue Onion Architecture. Cada integrante es dueño principal de sus *vertical slices* (entidad → configuración → repositorio → servicio → CQRS → controlador → vista/endpoint → pruebas), reduciendo dependencias.

| Proyecto de la solución | Contiene | Dueño principal |
| :--- | :--- | :--- |
| `ArtemisBank.Domain` | Entidades y enums (sin dependencias) | Compartido · cada quien su entidad |
| `ArtemisBank.Application` | CQRS, servicios, interfaces, DTOs, VM, AutoMapper, FluentValidation | Compartido por features |
| `ArtemisBank.Persistence` | DbContext, configuraciones, migraciones, repositorios | **Michael** |
| `ArtemisBank.Infrastructure` | Identity/JWT, correo, cripto, Azure Functions | **Monserrat + Manuel** |
| `ArtemisBank.WebApp` | MVC, controladores, vistas, ViewModels | Compartido por rol |
| `ArtemisBank.WebApi` | Controllers REST, Swagger, GlobalExceptionHandler | Compartido por módulo |
| `ArtemisBank.Tests` | Unit + Integration (xUnit, InMemory/SQLite) | Compartido |

---

## Tarjetas del Equipo y Planes Individuales

### Monserrat — Líder de Identidad, Seguridad y Fundación
* **Métricas:** 75 tareas de rúbrica | 1500 puntos | 33.6% de carga ponderada
* **Revisor principal:** Manuel
* **Módulos asignados:**
  * Solución base y capas (Onion)
  * ASP.NET Identity + seeding de roles/usuarios
  * Seguridad WebApp (autorización por rol, Access Denied)
  * Seguridad API (JWT Bearer, 401/403)
  * Account Controller (login, confirm, reset)
  * Gestión de usuarios WebApp + API
  * Gestión de comercios (API)

#### Archivos a crear
* `ArtemisBank.Domain/Entities/User.cs`, `Role.cs`, `Commerce.cs`, `Token.cs`
* `ArtemisBank.Application/Interfaces/IAccountService.cs`, `IUserService.cs`
* `ArtemisBank.Application/Features/Account/**` (Login/Confirm/Reset Commands+Handlers)
* `ArtemisBank.Application/Features/Users/**` y `Features/Commerce/**`
* `ArtemisBank.Infrastructure.Identity/**` (`JwtService`, `IdentityService`)
* `ArtemisBank.WebApp/Controllers/AccountController.cs`, `UserController.cs`
* `ArtemisBank.WebApi/Controllers/AccountController.cs`, `UsersController.cs`, `CommerceController.cs`
* `ArtemisBank.Application/Mappings/GeneralProfile.cs`, `Behaviors/ValidationBehavior.cs`

#### Archivos a modificar (Coordinado)
* `Program.cs` (WebApp y WebApi) — Identity, JWT, Serilog, Swagger, DI
* `appsettings.json` — Jwt, ConnectionStrings, Mail (coordinado)

#### Tareas clave de Monserrat
1. **MON-01 | Seguridad e Identity (WebApp + API)**
   * *Objetivo:* Autenticación y autorización por rol en ambos frontales.
   * *Explicación técnica:* Configurar ASP.NET Identity, cookies para MVC y JWT Bearer para la API. Filtros `[Authorize(Roles=...)]`, pantalla Access Denied, respuestas 401/403 en API.
   * *Por qué es necesaria:* Sin esto nadie puede proteger sus módulos; es dependencia dura de todo el equipo.
   * *DoD / Criterios de Aceptación:* Login valida credenciales, estado activo y rol permitido; acceso directo por URL bloqueado por rol; JWT incluye id, usuario, rol y expiración; respuestas 401 sin token y 403 sin permiso.
2. **MON-02 | Seeding de roles y usuarios por defecto**
   * *Objetivo:* Datos base para poder entrar al sistema.
   * *Explicación técnica:* Seed de roles Administrador/Cajero/Cliente (WebApp) y Administrador/Comercio (API), más un usuario activo por rol.
   * *Por qué es necesaria:* Sin seed nadie puede probar login ni los módulos protegidos.
   * *DoD / Criterios de Aceptación:* Roles creados por seeding; usuario activo por rol; usuarios nuevos nacen inactivos.
3. **MON-03 | Account Controller API + tokens de un solo uso**
   * *Objetivo:* Login/confirm/reset sin JWT previo.
   * *Explicación técnica:* Endpoints públicos POST `/account/login|confirm|get-reset-token|reset-password`. Token de reset vigente 30 min y de un solo uso; el correo API envía el token en el cuerpo, no como enlace.
   * *Por qué es necesaria:* Habilita el flujo de acceso de la API; es contrato consumido por pruebas y por comercios.
   * *DoD / Criterios de Aceptación:* Login retorna JWT solo si activo; confirm activa y marca token usado; reset valida token y reactiva cuenta; códigos 200/204/400/401/403 correctos.
4. **MON-04 | Gestión de usuarios (WebApp + API) y comercios**
   * *Objetivo:* CRUD con reglas de unicidad y cuenta principal.
   * *Explicación técnica:* Listados paginados (máx 20, excluye Comercio en `/api/users`), unicidad de usuario/correo/cédula, no editar rol, cliente/comercio reciben cuenta principal automática; comercio con un solo usuario.
   * *Por qué es necesaria:* Es la puerta de entrada de todos los clientes del sistema; alimenta cuentas y productos.
   * *DoD / Criterios de Aceptación:* Cliente creado genera cuenta principal de 9 dígitos; correo de activación enviado; auto-modificación de estado bloqueada; RNC y correo de comercio únicos.

---

### Michael — Líder de Cuentas, Transacciones, Cajero y Base de Datos
* **Métricas:** 76 tareas de rúbrica | 1520 puntos | 34.1% de carga ponderada
* **Revisor principal:** Monserrat
* **Módulos asignados:**
  * Cuentas de ahorro WebApp + API
  * Beneficiarios y transacciones del cliente
  * Transferencia entre cuentas propias
  * Módulo Cajero (depósito, retiro, terceros, pagos)
  * Reglas financieras (DÉBITO/CRÉDITO, transaccional, decimal)
  * Home del administrador (indicadores)
  * Consolidación de DbContext y Migraciones

#### Archivos a crear
* `ArtemisBank.Domain/Entities/SavingsAccount.cs`, `Transaction.cs`, `Beneficiary.cs`
* `ArtemisBank.Persistence/Context/ArtemisDbContext.cs` + `Configurations/**`
* `ArtemisBank.Application/Features/SavingsAccounts/**`, `Transactions/**`, `Beneficiaries/**`
* `ArtemisBank.Application/Services/AccountService.cs`, `TransactionService.cs` (transaccional)
* `ArtemisBank.WebApp/Controllers/SavingsAccountController.cs`, `TransactionController.cs`, `CashierController.cs`, `AdminHomeController.cs`
* `ArtemisBank.WebApi/Controllers/SavingsAccountController.cs`
* `ArtemisBank.WebApp/Views/**` (cuentas, cajero, cliente, home admin)

#### Archivos a modificar (Coordinado)
* `ArtemisDbContext.cs` (consolida entidades de todo el equipo vía sus Configurations)
* Migraciones (dueño exclusivo de la carpeta `Migrations` para evitar conflictos)

#### Tareas clave de Michael
1. **MIC-01 | DbContext y migraciones consolidadas**
   * *Objetivo:* Un solo punto de verdad para el esquema.
   * *Explicación técnica:* Cada quien entrega su `IEntityTypeConfiguration`; Michael los registra en el DbContext y genera migraciones. Precisión `decimal(18,2)` en montos, cédulas y números de cuenta como texto.
   * *Por qué es necesaria:* Evita el conflicto de Git más peligroso del proyecto: migraciones simultáneas.
   * *DoD / Criterios de Aceptación:* Migraciones aplican y generan la BD esperada; montos como `decimal(18,2)`; números de cuenta/préstamo como texto.
2. **MIC-02 | Cuentas de ahorro (WebApp + API)**
   * *Objetivo:* Alta de secundarias, detalle, cancelación.
   * *Explicación técnica:* Solo se crean secundarias (la principal la crea Monserrat al crear cliente). Cancelar secundaria transfiere balance a la principal con registro cruzado DÉBITO/CRÉDITO. Bloqueo de operaciones sobre canceladas.
   * *Por qué es necesaria:* Las cuentas son el contenedor de saldo de todo el sistema.
   * *DoD / Criterios de Aceptación:* Requiere cuenta principal activa antes de secundaria; cancelación mueve balance a principal; número único de 9 dígitos como texto.
3. **MIC-03 | Transacciones cliente + reglas financieras**
   * *Objetivo:* Express, beneficiarios, transferencia propia.
   * *Explicación técnica:* Débito en origen / crédito en destino, validación de fondos, ejecución transaccional (rollback si falla una pata), intentos rechazados registrados sin afectar balances.
   * *Por qué es necesaria:* Es el corazón de la trazabilidad; un fallo aquí corrompe balances de todos.
   * *DoD / Criterios de Aceptación:* Registro cruzado correcto; no aplica parcialmente si falla; rechazos no modifican balances.
4. **MIC-04 | Módulo Cajero + Home admin**
   * *Objetivo:* Operaciones de ventanilla e indicadores.
   * *Explicación técnica:* Depósito, retiro, transacciones a terceros y pagos (reutilizando servicios de deuda de Manuel). Indicadores del admin agregando cuentas, préstamos y tarjetas.
   * *Por qué es necesaria:* Cierra la operativa presencial y da visibilidad ejecutiva.
   * *DoD / Criterios de Aceptación:* Operaciones asociadas al cajero autenticado; confirmación previa; indicadores del día por fecha del sistema.

---

### Manuel — Líder de Productos de Crédito y Pagos
* **Métricas:** 72 tareas de rúbrica | 1440 puntos | 32.3% de carga ponderada
* **Revisor principal:** Michael
* **Módulos asignados:**
  * Préstamos WebApp + API (sistema francés)
  * Tabla de amortización y control de mora
  * Tarjetas de crédito WebApp + API (SHA-256)
  * Avance de efectivo (interés 6.25%)
  * Hermes Pay (procesador de pago dual por rol)
  * Pagos cliente a tarjeta y préstamo
  * Azure Functions (proceso diario de mora)

#### Archivos a crear
* `ArtemisBank.Domain/Entities/Loan.cs`, `Installment.cs`, `CreditCard.cs`, `Consumption.cs`
* `ArtemisBank.Application/Features/Loans/**`, `CreditCards/**`, `HermesPay/**`
* `ArtemisBank.Application/Services/AmortizationService.cs`, `RiskService.cs`, `HermesPayService.cs`
* `ArtemisBank.Infrastructure.Shared/CryptoService.cs` (SHA-256 CVC)
* `ArtemisBank.AzureFunctions/OverdueInstallmentsFunction.cs`
* `ArtemisBank.WebApp/Controllers/LoanController.cs`, `CreditCardController.cs`, `CashAdvanceController.cs`
* `ArtemisBank.WebApi/Controllers/LoanController.cs`, `CreditCardController.cs`, `PayController.cs`
* `ArtemisBank.WebApi/Middleware/GlobalExceptionHandler.cs` (Problem Details con Monserrat)

#### Archivos a modificar (Coordinado)
* `Program.cs` API (registro de `GlobalExceptionHandler` / Problem Details, coordinado)
* `appsettings.json` (parámetros de negocio: interés avance, plazos)

#### Tareas clave de Manuel
1. **MAN-01 | Amortización francesa + tabla de cuotas**
   * *Objetivo:* Cálculo exacto de cuota fija.
   * *Explicación técnica:* $C = P \cdot \frac{r(1+r)^n}{(1+r)^n - 1}$; si tasa 0% ⇒ $C = P/n$. Genera $n$ cuotas con vencimiento mensual, interés/capital por cuota, redondeo a 2 decimales.
   * *Por qué es necesaria:* Es la fórmula central del producto préstamo; un error propaga a deuda, indicadores y mora.
   * *DoD / Criterios de Aceptación:* Cuota coincide con la fórmula francesa; primera cuota vence al mes siguiente; valores con precisión de centavos.
2. **MAN-02 | Alto riesgo y desembolso**
   * *Objetivo:* Evaluación de riesgo y crédito a cuenta.
   * *Explicación técnica:* Deuda proyectada = deuda actual + total a pagar del nuevo préstamo; compara contra deuda promedio. API responde 409 si alto riesgo y no se confirmó. Desembolsa a cuenta principal como CRÉDITO.
   * *Por qué es necesaria:* Protege la integridad crediticia y conecta préstamo con cuentas de Michael.
   * *DoD / Criterios de Aceptación:* 409 Conflict cuando aplica sin confirmar; desembolso registrado como crédito; solo cliente activo con cuenta principal.
3. **MAN-03 | Tarjetas: número, CVC hash, avance**
   * *Objetivo:* Emisión segura y consumo.
   * *Explicación técnica:* Número único de 16 dígitos, expiración +3 años (MM/AA), CVC de 3 dígitos guardado como hash SHA-256. Avance = monto + 6.25% interés, validando crédito disponible. Nunca exponer número completo ni CVC.
   * *Por qué es necesaria:* Maneja los datos más sensibles del sistema; requiere criptografía correcta.
   * *DoD / Criterios de Aceptación:* CVC nunca en texto plano; solo últimos 4 dígitos visibles; avance rechazado si excede crédito disponible.
4. **MAN-04 | Hermes Pay + control de mora**
   * *Objetivo:* Procesador dual y proceso diario.
   * *Explicación técnica:* `process-payment` usa `commerceId` del JWT si el rol es Comercio, o de la URL si es Administrador. Azure Function diaria marca cuotas atrasadas de préstamos activos.
   * *Por qué es necesaria:* Integra el mundo externo de comercios y mantiene el estado de mora al día.
   * *DoD / Criterios de Aceptación:* Rol Comercio ignora `commerceId` de URL; consumo aprobado aumenta deuda y acredita al comercio; cuota vencida no pagada se marca atrasada.

---

## Trazabilidad de la Rúbrica

### Matriz de métricas · 223 criterios asignados

| Sección de la rúbrica | Criterios | Puntos | Monserrat | Michael | Manuel |
| :--- | :---: | :---: | :---: | :---: | :---: |
| **Funcionalidades generales y seguridad WebApp** | 10 | 200 | 10 | - | - |
| **Home del administrador** | 8 | 160 | - | 8 | - |
| **Gestión de usuarios WebApp** | 10 | 200 | 10 | - | - |
| **Gestión de préstamos WebApp** | 10 | 200 | - | - | 10 |
| **Gestión de tarjetas de crédito WebApp** | 10 | 200 | - | - | 10 |
| **Gestión de cuentas de ahorro WebApp** | 10 | 200 | - | 10 | - |
| **Funcionalidades del cliente** | 11 | 220 | - | 8 | 3 |
| **Funcionalidades del cajero** | 10 | 200 | - | 10 | - |
| **Seguridad general de la Web API** | 8 | 160 | 8 | - | - |
| **Módulo API: Account Controller** | 6 | 120 | 6 | - | - |
| **Módulo API: Gestión de usuarios** | 10 | 200 | 10 | - | - |
| **Módulo API: Gestión de préstamos** | 7 | 140 | - | - | 7 |
| **Módulo API: Gestión de tarjetas de crédito** | 6 | 120 | - | - | 6 |
| **Módulo API: Gestión de cuentas de ahorro** | 6 | 120 | - | 6 | - |
| **Módulo API: Gestión de comercios** | 7 | 140 | 7 | - | - |
| **Módulo API: Procesador de pago Hermes Pay** | 11 | 220 | - | - | 11 |
| **Reglas financieras y trazabilidad** | 8 | 160 | - | 8 | - |
| **Reglas técnicas y arquitectura** | 12 | 240 | 5 | 5 | 2 |
| **CQRS, Mediator, Behaviors y validaciones** | 10 | 200 | 4 | 3 | 3 |
| **Validación de servicios por módulo** | 9 | 180 | 3 | 3 | 3 |
| **Documentación, excepciones y logs** | 9 | 180 | 3 | 3 | 3 |
| **Pruebas unitarias - Commands y Queries** | 8 | 160 | 3 | 2 | 3 |
| **Pruebas unitarias - Servicios de negocio** | 9 | 180 | 1 | 3 | 5 |
| **Pruebas de integración - Repositorios y persistencia** | 9 | 180 | 2 | 4 | 3 |
| **Calidad final, entrega y ejecución** | 9 | 180 | 3 | 3 | 3 |
| **TOTAL GENERAL** | **223** | **4460** | **75** | **76** | **72** |

---

### Matriz de Responsabilidad (RACI)
* **R** = Implementa · **A** = Responsable de entrega · **C** = Consultado · **I** = Informado
* *Revisiones cruzadas:* Monserrat revisa a Michael, Michael revisa a Manuel, Manuel revisa a Monserrat.

| Área / Módulo | Monserrat | Michael | Manuel |
| :--- | :---: | :---: | :---: |
| Fundación / Onion / Identity | **R / A** | C | I |
| Seguridad y JWT | **R / A** | I | C |
| Usuarios y comercios | **R / A** | I | C |
| Cuentas y transacciones | C | **R / A** | I |
| Cajero y reglas financieras | I | **R / A** | C |
| Base de datos y migraciones | C | **R / A** | C |
| Préstamos y amortización | I | C | **R / A** |
| Tarjetas y avance de efectivo | I | C | **R / A** |
| Hermes Pay y comercios (pago) | C | I | **R / A** |
| Testing e integración final | R | R | R |

---

## Plan de Ejecución y Cronograma Relativo

### Estrategia Git y flujo de trabajo
* `main`: Solo releases estables.
* `develop`: Rama principal de integración.
* `feature/mon-*`: Identidad, seguridad, usuarios (Monserrat).
* `feature/mic-*`: Cuentas, cajero, transacciones (Michael).
* `feature/man-*`: Préstamos, tarjetas, Hermes Pay (Manuel).

**Reglas de Git:**
1. Nadie trabaja en `main`.
2. Commits pequeños (`feat`, `fix`, `test`, `db`).
3. PR obligatorio con revisor asignado (no auto-aprobar).
4. Prohibido merge con build roto.
5. Prohibido subir secretos o carpetas `bin/obj`.

---

### Cronograma Relativo por Días (D1 - D11)

```
D1   D2   D3   D4   D5   D6   D7   D8   D9   D10  D11
[F0]----> [Análisis y Contratos]
     [F1]---------> [Arquitectura y Solución Base]
          [F2]---------> [Base de Datos y Migraciones]
               [F3]---------> [Seguridad, Identity y JWT]
                    [F4]---------> [Cuentas y Transacciones]
                    [F5]---------> [Préstamos, Tarjetas, Hermes Pay]
                         [F6]---------> [Usuarios, Comercios, Cajero, Cliente]
                              [F7]---------> [Integración y CQRS]
                                   [F8]---------> [Testing Unit + Integración]
                                        [F9]---------> [Docs, Logs, Correcciones]
                                             [F10]-----> [Entrega y Presentación]
```

---

### Puntos de Integración Críticos
| Contrato / Servicio | Provee | Consume |
| :--- | :--- | :--- |
| `IJwtService` / `Identity` | Monserrat | Todos |
| `SavingsAccount` + `Transaction` | Michael | Manuel · Cajero |
| Servicio de Deuda (Tarjeta/Préstamo) | Manuel | Cajero · Cliente |
| Cuenta Principal del Cliente | Monserrat | Michael · Manuel |
| `DbContext` / Migraciones | Michael | Todos |

---

## Calidad, Seguridad y Cierre

### Plan de Pruebas (Testing Cruzado)
Nadie prueba únicamente su propio código: cada módulo tiene un implementador y un tester cruzado.

| Módulo | Implementa | Prueba Cruzada |
| :--- | :--- | :--- |
| **Seguridad / Usuarios** | Monserrat | Manuel |
| **Cuentas / Transacciones** | Michael | Monserrat |
| **Préstamos / Tarjetas** | Manuel | Michael |

---

### Seguridad Bancaria (Nivel Académico)
* Autenticación/autorización por rol (Identity + JWT) — Monserrat
* CVC hasheado con SHA-256, número de tarjeta enmascarado — Manuel
* Operaciones transaccionales atómicas, sin sobrepagos — Michael
* Sin secretos ni datos sensibles en logs/correos/UI — Todos

---

### Definición Global de "Terminado" (Definition of Done)
1. Compila e integra en `develop` sin romper otras funciones.
2. Validaciones + seguridad + manejo de errores + pruebas completas.
3. Cumple arquitectura Onion/CQRS y el criterio de rúbrica asociado.
4. PR aprobado por el revisor correspondiente; sin secretos ni `TODO` pendientes.

---

### Checkpoints del Equipo
* **CP1:** Contratos congelados
* **CP2:** Base de datos lista
* **CP3:** Primera integración exitosa
* **CP4:** Seguridad y JWT cerrados
* **CP5:** Pruebas unitarias/integración
* **CP6:** Auditoría completa de rúbrica
* **CP-Final:** Entrega y presentación

---

## Registro de Riesgos y Mitigación

| Riesgo | Probabilidad | Impacto | Responsable | Mitigación |
| :--- | :---: | :---: | :---: | :--- |
| **Conflicto de migraciones simultáneas** | Media | Alto | Michael | Un solo dueño del `DbContext`; cada quien entrega sus `Configurations`. |
| **Cambio unilateral de un contrato compartido** | Media | Alto | Todos | Fase 'Contratos Congelados'; PR obligatorio para tocar interfaces/DTOs. |
| **Integración tardía** | Media | Alto | Todos | Merges a `develop` cada 2 días; checkpoints por fase. |
| **Fallo en amortización o reglas financieras** | Baja | Crítico | Manuel / Michael | Tests unitarios de cuota y de transaccionalidad antes de integrar. |
| **Exposición de datos sensibles (CVC, JWT)** | Baja | Crítico | Manuel / Monserrat | SHA-256 para CVC, nunca loguear secretos, revisión cruzada. |
| **Concentración de conocimiento en una persona** | Media | Medio | Todos | Revisor por módulo con conocimiento suficiente + DoD documentado. |

---

## Auditoría de Equidad y Verificación Final

* [x] Las tres personas tienen implementación real y pruebas.
* [x] Ningún requisito ni métrica queda sin responsable.
* [x] Cada tarea tiene asignado un revisor principal.
* [x] Dependencias entre capas y módulos documentadas.
* [x] Existe tiempo dedicado a integración y corrección de errores.
* [x] Cargas equilibradas: **Monserrat 33.6% · Michael 34.1% · Manuel 32.3%** (*diferencia máxima 1.8%*).

> **DISTRIBUCIÓN EQUITATIVA Y COMPLETA.**  
> Los 223 criterios (4460 pts) están totalmente asignados. Cualquier integrante del equipo puede consultar este documento y ejecutar sus tareas de manera autónoma.

---
*Artemis Banking Pro · Plan Maestro de Distribución de Trabajo · Monserrat · Michael · Manuel*
