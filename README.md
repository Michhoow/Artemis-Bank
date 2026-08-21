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
