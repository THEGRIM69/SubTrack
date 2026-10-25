# SubTrack 1.0 — Especificación de contratos de Core

**Documento:** CORE-CONTRATOS v1
**Estado:** PROPUESTA — requiere aprobación de GRIM, ZYKO y COSMO
**Autor:** COSMO (Infrastructure)
**Fecha:** 25 de octubre de 2026
**Base normativa:** D1 (75 decisiones aprobadas), CONTRATOS-INTEGRACION v1.0, ARQUITECTURA, migración `0001_esquema_inicial_dominio`

> **Este documento no autoriza implementación.** Es la propuesta que cierra los
> «Pendientes técnicos antes de implementar» de CONTRATOS-INTEGRACION §227.
> Hasta que GRIM lo apruebe, nadie modifica código por causa de este documento.

---

## 0. Qué resuelve

| Pendiente de CONTRATOS §227 | Sección de este documento |
|---|---|
| Firmas exactas de interfaces y servicios C# | §7 |
| Estados y enumeraciones | §4 |
| Contratos para cobros programados | §6, §7, §8.2 |
| Tratamiento de ingresos futuros y gastos previstos | §4, §8.6, §8.7 |
| Convenciones de errores y validaciones | §3 |
| Forma de las entidades | §5 |
| Correspondencia con SQLite | §9 |

Además resuelve siete conflictos detectados entre decisiones y documentos previos (§11).
Sin esas resoluciones, ZYKO y COSMO implementarían reglas distintas.

---

## 1. Fronteras entre capas

```text
WinForms (GRIM)
   │  usa interfaces y DTOs de Core
   ▼
Core (ZYKO) ──────────────►  Infrastructure (COSMO)
   • entidades                  • implementa las interfaces de §7
   • enumeraciones              • repositorios internos
   • Result<T> y códigos        • transacciones y SQL
   • cálculos puros             • mapeo enum ↔ código persistido
   • interfaces de servicio     • traducción de errores SQLite
```

Reglas obligatorias:

1. Core **no** referencia SQLite, WinForms ni Infrastructure.
2. WinForms **no** ejecuta SQL ni abre conexiones.
3. Los repositorios son **internos de Infrastructure** (ver C-02).
4. Ningún importe se representa con `double`, `float` ni `decimal` en la capa de datos:
   siempre `long` en centavos (F0-T30).
5. Las fechas de negocio son `DateOnly`; las fechas técnicas son `DateTimeOffset` en UTC.
6. Toda operación de escritura es asíncrona y devuelve `Result<T>` (F0-T03, F0-15).

---

## 2. Convenciones

### 2.1 Tipos

| Concepto | Tipo C# | Persistencia | Decisión |
|---|---|---|---|
| Identificador | `Guid` | `TEXT` 36, minúsculas, formato `D` | Contratos «Identificadores» |
| Importe | `long` (centavos) | `INTEGER` | F0-T30 |
| Fecha de negocio | `DateOnly` | `TEXT` `yyyy-MM-dd` | F0-T12, F0-T30 |
| Fecha técnica | `DateTimeOffset` (UTC) | `TEXT` ISO UTC | F0-T12, F0-T32 |
| Booleano | `bool` | `INTEGER` 0/1 | — |
| Texto | `string` | `TEXT` | — |
| Clave de idempotencia | `Guid` | `TEXT` (formato `D`) | F0-T04, F0-27 |

### 2.2 Idioma de los identificadores

| Ámbito | Idioma | Ejemplo |
|---|---|---|
| Tipos y miembros C# | Inglés | `ScheduledCharge.AmountCents` |
| Tablas y columnas SQLite | Español `snake_case` | `cobros_programados.monto_centavos` |
| Códigos de error | Español mayúsculas | `COBRO_YA_PAGADO` |
| Textos de interfaz | Español | «Este cobro ya fue registrado como pagado.» |

Motivo: D1 §4 nombra las entidades en inglés (`ScheduledCharge`, `Payment`) y el código
existente de ZYKO ya usa inglés (`BillingFrequency`, `CalculateNextDate`). Se mantiene esa
línea para no reescribir su trabajo. Ver conflicto **C-03**.

### 2.3 Correspondencia enumeración ↔ código persistido

El mapeo vive **solo en Infrastructure**. Core nunca ve el código de texto.

| Enumeración | Valor C# | Código SQLite |
|---|---|---|
| `AmountType` | `Fixed` | `FIJO` |
| | `Variable` | `VARIABLE` |
| `BillingFrequency` | `Daily` | `DIARIA` |
| | `Weekly` | `SEMANAL` |
| | `Monthly` | `MENSUAL` |
| | `Quarterly` | `TRIMESTRAL` |
| | `SemiAnnual` | `SEMESTRAL` |
| | `Annual` | `ANUAL` |
| `ScheduledChargeStatus` | `Pending` | `PENDIENTE` |
| | `Paid` | `PAGADO` |
| | `Cancelled` | `CANCELADO` |
| `PaymentStatus` | `Active` | `VIGENTE` |
| | `Cancelled` | `ANULADO` |
| `ExpenseStatus` | `Planned` | `PLANIFICADO` |
| | `Confirmed` | `CONFIRMADO` |
| | `Cancelled` | `ANULADO` |
| `IncomeStatus` | `Planned` | `PREVISTO` |
| | `Confirmed` | `CONFIRMADO` |
| | `Cancelled` | `ANULADO` |
| `IncomeType` | `Monthly` | `MENSUAL` |
| | `Biweekly` | `QUINCENAL` |
| | `Extraordinary` | `EXTRAORDINARIO` |

Los valores numéricos de las enumeraciones son **estables** y no deben reordenarse.

---

## 3. `Result<T>` y errores

### 3.1 Tipos

```csharp
namespace SubTrack.Core.Results;

public sealed record DomainError(string Code, string Message, string? Field = null);

public sealed class Result
{
    private Result(bool isSuccess, IReadOnlyList<DomainError> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<DomainError> Errors { get; }

    public static Result Ok() => new(true, Array.Empty<DomainError>());

    public static Result Fail(string code, string message) =>
        new(false, [new DomainError(code, message)]);

    public static Result Fail(DomainError error) => new(false, [error]);

    public static Result Fail(IReadOnlyList<DomainError> errors) =>
        new(false, errors);
}

public sealed class Result<T>
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, IReadOnlyList<DomainError> errors)
    {
        IsSuccess = isSuccess;
        _value = value;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<DomainError> Errors { get; }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException(
            "No se puede leer Value de un Result fallido. Compruebe IsSuccess.");

    public static Result<T> Ok(T value) => new(true, value, Array.Empty<DomainError>());

    public static Result<T> Fail(string code, string message) =>
        new(false, default, [new DomainError(code, message)]);

    public static Result<T> Fail(DomainError error) => new(false, default, [error]);

    public static Result<T> Fail(IReadOnlyList<DomainError> errors) =>
        new(false, default, errors);
}
```

### 3.2 Reglas de uso

1. Los errores **esperados** de negocio se devuelven como `Result` fallido. No se lanzan excepciones.
2. Las **excepciones** se reservan para fallos técnicos inesperados (I/O, corrupción, programación).
3. Infrastructure traduce toda excepción de SQLite a un `Result` fallido antes de cruzar la frontera.
4. Nunca se expone el mensaje crudo de SQLite a la interfaz (F0-15).
5. `Errors` puede contener más de un error de validación a la vez.
6. Un `Result` fallido **no** contiene valor.

### 3.3 Catálogo de códigos

Los códigos son estables: no se renombran ni se reutilizan para otro significado.

**Validación**

| Código | Situación | Decisión |
|---|---|---|
| `NOMBRE_REQUERIDO` | Nombre vacío o solo espacios | F0-T33 |
| `NOMBRE_DEMASIADO_LARGO` | Supera 200 caracteres (100 en categorías) | F0-T33 |
| `IMPORTE_INVALIDO` | Importe ≤ 0 o fuera de rango | F0-T30, F0-T33 |
| `FECHA_REQUERIDA` | Falta una fecha obligatoria | F0-T33 |
| `FECHA_FUTURA_NO_PERMITIDA` | Fecha real posterior a hoy | C-04 |
| `RANGO_EXCEDIDO` | Generación mayor a 12 meses | F0-28 |
| `MOTIVO_REQUERIDO` | Anulación sin motivo | F0-T02 |
| `DIA_ANCLA_INVALIDO` | Día fuera de 1..31 | F0-T31 |
| `DIAS_DE_INGRESO_INVALIDOS` | Quincenal distinto de 15 o último día | F0-10, F0-T01 |
| `CATEGORIA_REQUERIDA` | Gasto sin categoría | F0-13 |
| `ESTADO_INICIAL_INVALIDO` | Combinación estado/fecha incoherente | F0-T08 |
| `OPERACION_INVALIDA` | Operación de idempotencia vacía o mal formada | F0-27 |

**Suscripciones**

| Código | Situación | Decisión |
|---|---|---|
| `SUSCRIPCION_NO_ENCONTRADA` | Id inexistente | — |
| `SUSCRIPCION_INACTIVA` | Se intenta modificar una inactiva | F0-10 |
| `SUSCRIPCION_YA_INACTIVA` | Se intenta desactivar dos veces | F0-10 |
| `FECHA_EFECTIVA_ANTERIOR` | Cambio con fecha anterior a hoy | F0-09, F0-23 |

**Cobros programados**

| Código | Situación | Decisión |
|---|---|---|
| `COBRO_NO_ENCONTRADO` | Id inexistente | — |
| `COBRO_YA_PAGADO` | Ya tiene pago vigente | F0-02, F0-18 |
| `COBRO_CANCELADO` | Se intenta pagar un cobro cancelado | F0-T01 |
| `COBRO_IMPORTE_NO_COINCIDE` | Importe distinto al programado | F0-04 |
| `COBRO_CON_PAGOS` | Se intenta eliminar un cobro con historial | F0-T02 |
| `COBRO_DUPLICADO` | Ya existe ese cobro para la suscripción y fecha | F0-01, F0-28 |
| `AJUSTE_NO_PERMITIDO` | Se intenta ajustar el importe en una suscripción fija | F0-22 |

**Pagos**

| Código | Situación | Decisión |
|---|---|---|
| `PAGO_NO_ENCONTRADO` | Id inexistente | — |
| `COMPROBANTE_DUPLICADO` | Número de comprobante repetido | F0-03 |
| `OPERACION_YA_PROCESADA` | Reintento sobre una operación ya anulada | F0-27 |

**Gastos**

| Código | Situación | Decisión |
|---|---|---|
| `GASTO_NO_ENCONTRADO` | Id inexistente | — |
| `GASTO_YA_CONFIRMADO` | Se confirma dos veces | F0-T08 |
| `GASTO_YA_ANULADO` | Se modifica un gasto anulado | F0-T02 |

**Ingresos**

| Código | Situación | Decisión |
|---|---|---|
| `INGRESO_NO_ENCONTRADO` | Id inexistente | — |
| `INGRESO_YA_CONFIRMADO` | Se confirma dos veces | F0-06, F0-19 |
| `INGRESO_YA_ANULADO` | Se modifica un ingreso anulado | F0-T02 |
| `PLANTILLA_NO_ENCONTRADA` | Plantilla de ingreso inexistente | F0-T07 |

**Categorías**

| Código | Situación | Decisión |
|---|---|---|
| `CATEGORIA_NO_ENCONTRADA` | Id inexistente | — |
| `CATEGORIA_INACTIVA` | Se usa una categoría desactivada | F0-T11 |
| `CATEGORIA_CON_GASTOS` | Se intenta eliminar una categoría con gastos | F0-T11 |
| `CATEGORIA_DUPLICADA` | Nombre ya existente | F0-T13 |

**Saldo inicial**

| Código | Situación | Decisión |
|---|---|---|
| `SALDO_INICIAL_NO_CONFIGURADO` | No existe saldo inicial | F0-20 |
| `SALDO_INICIAL_YA_CONFIGURADO` | Se intenta crear dos veces | F0-T09 |

**Persistencia**

| Código | Situación |
|---|---|
| `BASE_OCUPADA` | `SQLITE_BUSY` / `SQLITE_LOCKED` tras agotar la espera |
| `BASE_CORRUPTA` | `SQLITE_CORRUPT` / `SQLITE_NOTADB` |
| `BASE_NO_DISPONIBLE` | `SQLITE_CANTOPEN` / `SQLITE_READONLY` / `SQLITE_FULL` |
| `ERROR_INTERNO` | Cualquier otro fallo técnico |

### 3.4 Idempotencia por `OperationId` (F0-04, F0-27)

Regla única para **altas** de pago, gasto, ingreso, suscripción y plantilla de ingreso:

```text
Al recibir una solicitud con OperationId:
  1. Buscar un registro existente con ese OperationId.
  2. No existe            → ejecutar la operación normalmente.
  3. Existe y está vigente → devolver éxito con el registro existente
                             (reintento legítimo, no se duplica).
  4. Existe y fue anulado  → devolver OPERACION_YA_PROCESADA.
                             No se reactiva ni se crea otro registro.
```

La garantía física es el índice `UNIQUE(operacion_id)`. Si la búsqueda previa no lo ve por
concurrencia, el `INSERT` falla y Infrastructure repite el paso 3 dentro de la misma
transacción. Un `OperationId` identifica **un intento del usuario**, no un movimiento.

Las **ediciones** y **desactivaciones** no requieren `OperationId`: aplicar dos veces la
misma operación produce el mismo estado final. En el caso de las suscripciones, la
restricción `UNIQUE(suscripcion_id, vigente_desde)` impide además duplicar una versión.

---

## 4. Enumeraciones

```csharp
namespace SubTrack.Core.Domain.Enums;

public enum BillingFrequency
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Quarterly = 4,
    SemiAnnual = 5,
    Annual = 6
}                                   // YA EXISTE en feature/business-logic

public enum AmountType
{
    Fixed = 1,
    Variable = 2
}

public enum ScheduledChargeStatus
{
    Pending = 1,
    Paid = 2,
    Cancelled = 3
}

public enum PaymentStatus
{
    Active = 1,
    Cancelled = 2
}

public enum ExpenseStatus
{
    Planned = 1,
    Confirmed = 2,
    Cancelled = 3
}

public enum IncomeStatus
{
    Planned = 1,
    Confirmed = 2,
    Cancelled = 3
}

public enum IncomeType
{
    Monthly = 1,
    Biweekly = 2,
    Extraordinary = 3
}
```

`IncomeType.Extraordinary` nunca se usa en plantillas (F0-T07).
Para ingresos quincenales, `AnchorDay` solo admite `15` o `31`, donde `31` significa
«último día del mes» (F0-10, F0-T01).

---

## 5. Entidades

Convención: entidades con propiedades `required init` (Infrastructure las materializa con
inicializadores de objeto); DTOs posicionales (§6).

```csharp
namespace SubTrack.Core.Domain.Entities;

public sealed record Subscription
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Guid OperationId { get; init; }
    public required long AmountCents { get; init; }
    public required AmountType AmountType { get; init; }
    public required BillingFrequency Frequency { get; init; }
    public required DateOnly StartDate { get; init; }
    public required int AnchorDay { get; init; }          // derivado de la versión vigente
    public DateOnly? NextChargeDate { get; init; }        // null solo si está inactiva y sin pendientes
    public required bool IsActive { get; init; }
    public DateOnly? DeactivatedOn { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public sealed record SubscriptionVersion
{
    public required Guid Id { get; init; }
    public required Guid SubscriptionId { get; init; }
    public required int VersionNumber { get; init; }
    public required DateOnly EffectiveFrom { get; init; }
    public required long AmountCents { get; init; }
    public required AmountType AmountType { get; init; }
    public required BillingFrequency Frequency { get; init; }
    public required int AnchorDay { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed record ScheduledCharge
{
    public required Guid Id { get; init; }
    public required Guid SubscriptionId { get; init; }
    public required DateOnly ScheduledDate { get; init; }
    public required long AmountCents { get; init; }
    public required ScheduledChargeStatus Status { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public sealed record Payment
{
    public required Guid Id { get; init; }
    public required Guid ScheduledChargeId { get; init; }
    public required Guid OperationId { get; init; }
    public required DateOnly PaymentDate { get; init; }
    public required long AmountCents { get; init; }
    public required PaymentStatus Status { get; init; }
    public required string VoucherNumber { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public sealed record Expense
{
    public required Guid Id { get; init; }
    public required string Concept { get; init; }
    public required Guid CategoryId { get; init; }
    public required Guid OperationId { get; init; }
    public required long AmountCents { get; init; }
    public required DateOnly PlannedDate { get; init; }
    public DateOnly? ActualDate { get; init; }
    public required ExpenseStatus Status { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public sealed record Income
{
    public required Guid Id { get; init; }
    public required string Concept { get; init; }
    public Guid? TemplateId { get; init; }
    public required Guid OperationId { get; init; }
    public required long AmountCents { get; init; }
    public required IncomeType Type { get; init; }
    public required DateOnly PlannedDate { get; init; }
    public DateOnly? ActualDate { get; init; }
    public required IncomeStatus Status { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public sealed record RecurringIncomeTemplate
{
    public required Guid Id { get; init; }
    public required string Concept { get; init; }
    public required Guid OperationId { get; init; }
    public required long AmountCents { get; init; }
    public required IncomeType Frequency { get; init; }   // Monthly | Biweekly
    public required int AnchorDay { get; init; }
    public required DateOnly StartDate { get; init; }
    public required bool IsActive { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public sealed record ExpenseCategory
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required bool IsActive { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public sealed record OpeningBalance
{
    public required long AmountCents { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public sealed record ExpenseCorrection
{
    public required Guid Id { get; init; }
    public required Guid ExpenseId { get; init; }
    public string? PreviousConcept { get; init; }
    public string? NewConcept { get; init; }
    public long? PreviousAmountCents { get; init; }
    public long? NewAmountCents { get; init; }
    public Guid? PreviousCategoryId { get; init; }
    public Guid? NewCategoryId { get; init; }
    public DateOnly? PreviousDate { get; init; }
    public DateOnly? NewDate { get; init; }
    public required DateTimeOffset ModifiedAt { get; init; }
}

public sealed record IncomeCorrection
{
    public required Guid Id { get; init; }
    public required Guid IncomeId { get; init; }
    public string? PreviousConcept { get; init; }
    public string? NewConcept { get; init; }
    public long? PreviousAmountCents { get; init; }
    public long? NewAmountCents { get; init; }
    public DateOnly? PreviousDate { get; init; }
    public DateOnly? NewDate { get; init; }
    public required DateTimeOffset ModifiedAt { get; init; }
}

public sealed record OpeningBalanceCorrection
{
    public required Guid Id { get; init; }
    public required long PreviousAmountCents { get; init; }
    public required long NewAmountCents { get; init; }
    public required DateOnly PreviousStartDate { get; init; }
    public required DateOnly NewStartDate { get; init; }
    public required DateTimeOffset ModifiedAt { get; init; }
}
```

---

## 6. DTOs de solicitud

```csharp
namespace SubTrack.Core.Contracts;

// ---------- Suscripciones ----------

public sealed record InitialPendingCharge(DateOnly ScheduledDate, long AmountCents);

public sealed record CreateSubscriptionRequest(
    string Name,
    long AmountCents,
    AmountType AmountType,
    BillingFrequency Frequency,
    DateOnly StartDate,
    Guid OperationId,
    IReadOnlyList<InitialPendingCharge>? InitialPendingCharges = null);

public sealed record UpdateSubscriptionRequest(
    Guid SubscriptionId,
    string Name,
    long AmountCents,
    AmountType AmountType,
    BillingFrequency Frequency,
    DateOnly EffectiveFrom);

public sealed record DeactivateSubscriptionRequest(
    Guid SubscriptionId,
    DateOnly EffectiveFrom);

// ---------- Cobros programados ----------

public sealed record GenerateChargesRequest(
    Guid SubscriptionId,
    DateOnly Until);

public sealed record AdjustChargeAmountRequest(
    Guid ScheduledChargeId,
    long AmountCents);

public sealed record RegisterInitialPendingChargeRequest(
    Guid SubscriptionId,
    DateOnly ScheduledDate,
    long AmountCents);

// ---------- Pagos ----------

public sealed record RegisterPaymentRequest(
    Guid ScheduledChargeId,
    DateOnly PaymentDate,
    Guid OperationId);          // forma exacta de F0-T04

public sealed record CancelPaymentRequest(
    Guid PaymentId,
    string Reason);

// ---------- Gastos ----------

public sealed record CreateExpenseRequest(
    string Concept,
    Guid CategoryId,
    long AmountCents,
    DateOnly? PlannedDate,
    DateOnly? ActualDate,
    Guid OperationId);

public sealed record UpdateExpenseRequest(
    Guid ExpenseId,
    string Concept,
    Guid CategoryId,
    long AmountCents,
    DateOnly PlannedDate);

public sealed record ConfirmExpenseRequest(
    Guid ExpenseId,
    DateOnly ActualDate);

public sealed record CancelExpenseRequest(
    Guid ExpenseId,
    string Reason);

public sealed record ExpenseHistoryRequest(
    PageRequest Page,
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? CategoryId = null,
    ExpenseStatus? Status = null);

// ---------- Ingresos ----------

public sealed record CreateIncomeTemplateRequest(
    string Concept,
    long AmountCents,
    IncomeType Frequency,
    int AnchorDay,
    DateOnly StartDate,
    Guid OperationId);

public sealed record GeneratePlannedIncomesRequest(
    Guid TemplateId,
    DateOnly Until);

public sealed record CreateIncomeRequest(
    string Concept,
    long AmountCents,
    IncomeType Type,
    DateOnly? PlannedDate,
    DateOnly? ActualDate,
    Guid? TemplateId,
    Guid OperationId);

public sealed record UpdateIncomeRequest(
    Guid IncomeId,
    string Concept,
    long AmountCents,
    DateOnly PlannedDate);

public sealed record ConfirmIncomeRequest(
    Guid IncomeId,
    DateOnly ActualDate);

public sealed record CancelIncomeRequest(
    Guid IncomeId,
    string Reason);

public sealed record IncomeHistoryRequest(
    PageRequest Page,
    DateOnly? From = null,
    DateOnly? To = null,
    IncomeStatus? Status = null);

// ---------- Categorías ----------

public sealed record CreateExpenseCategoryRequest(string Name);

public sealed record UpdateExpenseCategoryRequest(Guid CategoryId, string Name);

// ---------- Saldo inicial ----------

public sealed record SetOpeningBalanceRequest(long AmountCents, DateOnly StartDate);

public sealed record CorrectOpeningBalanceRequest(long AmountCents, DateOnly StartDate);

// ---------- Dashboard y paginación ----------

public sealed record GetDashboardRequest(DateOnly Month);

public sealed record PageRequest(int Page = 1, int PageSize = 50)
{
    public const int DefaultPageSize = 50;   // F0-T29
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    public bool HasNextPage => Page < TotalPages;
}
```

### 6.1 Reglas de los DTOs

| DTO | Regla | Decisión |
|---|---|---|
| `CreateExpenseRequest` | `ActualDate == null` ⇒ estado `Planned`, `PlannedDate` obligatoria. `ActualDate != null` ⇒ estado `Confirmed`, `PlannedDate ??= ActualDate` | F0-07, F0-T08 |
| `CreateIncomeRequest` | misma regla que gastos | F0-06, F0-19 |
| `CreateIncomeTemplateRequest` | `Frequency` solo `Monthly` o `Biweekly`; `Biweekly` exige `AnchorDay` 15 o 31 | F0-T07, F0-T01 |
| `UpdateSubscriptionRequest` | `EffectiveFrom` no puede ser anterior a hoy | F0-09, F0-23 |
| `GenerateChargesRequest` | `Until` no puede exceder hoy + 12 meses | F0-28 |
| `RegisterPaymentRequest` | no lleva importe: el pago siempre es por el importe del cobro | F0-04 |
| `PageRequest` | `PageSize` máximo 100; por defecto 50 | F0-T29 |

---

## 7. Interfaces de servicio (Core)

Todas son asíncronas y devuelven `Result`/`Result<T>` (F0-T03).
Las implementa Infrastructure (COSMO).

```csharp
namespace SubTrack.Core.Abstractions;

public interface ISubscriptionService
{
    Task<Result<Subscription>> CreateAsync(
        CreateSubscriptionRequest request, CancellationToken cancellationToken = default);

    Task<Result<Subscription>> UpdateAsync(
        UpdateSubscriptionRequest request, CancellationToken cancellationToken = default);

    Task<Result<Subscription>> DeactivateAsync(
        DeactivateSubscriptionRequest request, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<Subscription>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Result<Subscription>> GetByIdAsync(
        Guid subscriptionId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<SubscriptionVersion>>> GetVersionsAsync(
        Guid subscriptionId, CancellationToken cancellationToken = default);
}

public interface IScheduledChargeService
{
    Task<Result<IReadOnlyList<ScheduledCharge>>> GenerateAsync(
        GenerateChargesRequest request, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ScheduledCharge>>> GetByRangeAsync(
        DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ScheduledCharge>>> GetPendingAsync(
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ScheduledCharge>>> GetOverdueAsync(
        DateOnly asOf, CancellationToken cancellationToken = default);

    Task<Result<ScheduledCharge>> AdjustAmountAsync(
        AdjustChargeAmountRequest request, CancellationToken cancellationToken = default);

    Task<Result<ScheduledCharge>> RegisterInitialPendingAsync(
        RegisterInitialPendingChargeRequest request, CancellationToken cancellationToken = default);

    Task<Result<ScheduledCharge>> CancelAsync(
        Guid scheduledChargeId, CancellationToken cancellationToken = default);
}

public interface IPaymentService
{
    Task<Result<Payment>> RegisterAsync(
        RegisterPaymentRequest request, CancellationToken cancellationToken = default);

    Task<Result<Payment>> CancelAsync(
        CancelPaymentRequest request, CancellationToken cancellationToken = default);

    Task<Result<Payment>> GetByIdAsync(
        Guid paymentId, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<Payment>>> GetHistoryAsync(
        PageRequest page, CancellationToken cancellationToken = default);
}

public interface IExpenseService
{
    Task<Result<Expense>> CreateAsync(
        CreateExpenseRequest request, CancellationToken cancellationToken = default);

    Task<Result<Expense>> UpdateAsync(
        UpdateExpenseRequest request, CancellationToken cancellationToken = default);

    Task<Result<Expense>> ConfirmAsync(
        ConfirmExpenseRequest request, CancellationToken cancellationToken = default);

    Task<Result<Expense>> CancelAsync(
        CancelExpenseRequest request, CancellationToken cancellationToken = default);

    Task<Result<Expense>> GetByIdAsync(
        Guid expenseId, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<Expense>>> GetHistoryAsync(
        ExpenseHistoryRequest request, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ExpenseCorrection>>> GetCorrectionsAsync(
        Guid expenseId, CancellationToken cancellationToken = default);
}

public interface IIncomeService
{
    Task<Result<RecurringIncomeTemplate>> CreateTemplateAsync(
        CreateIncomeTemplateRequest request, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<RecurringIncomeTemplate>>> GetTemplatesAsync(
        bool includeInactive, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<Income>>> GeneratePlannedAsync(
        GeneratePlannedIncomesRequest request, CancellationToken cancellationToken = default);

    Task<Result<Income>> CreateAsync(
        CreateIncomeRequest request, CancellationToken cancellationToken = default);

    Task<Result<Income>> UpdateAsync(
        UpdateIncomeRequest request, CancellationToken cancellationToken = default);

    Task<Result<Income>> ConfirmAsync(
        ConfirmIncomeRequest request, CancellationToken cancellationToken = default);

    Task<Result<Income>> CancelAsync(
        CancelIncomeRequest request, CancellationToken cancellationToken = default);

    Task<Result<Income>> GetByIdAsync(
        Guid incomeId, CancellationToken cancellationToken = default);

    Task<Result<PagedResult<Income>>> GetHistoryAsync(
        IncomeHistoryRequest request, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<IncomeCorrection>>> GetCorrectionsAsync(
        Guid incomeId, CancellationToken cancellationToken = default);
}

public interface IExpenseCategoryService
{
    Task<Result<ExpenseCategory>> CreateAsync(
        CreateExpenseCategoryRequest request, CancellationToken cancellationToken = default);

    Task<Result<ExpenseCategory>> UpdateAsync(
        UpdateExpenseCategoryRequest request, CancellationToken cancellationToken = default);

    Task<Result<ExpenseCategory>> DeactivateAsync(
        Guid categoryId, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        Guid categoryId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ExpenseCategory>>> GetAllAsync(
        bool includeInactive, CancellationToken cancellationToken = default);
}

public interface IOpeningBalanceService
{
    Task<Result<OpeningBalance>> GetAsync(
        CancellationToken cancellationToken = default);

    Task<Result<OpeningBalance>> SetAsync(
        SetOpeningBalanceRequest request, CancellationToken cancellationToken = default);

    Task<Result<OpeningBalance>> CorrectAsync(
        CorrectOpeningBalanceRequest request, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<OpeningBalanceCorrection>>> GetCorrectionsAsync(
        CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<Result<DashboardSnapshot>> GetAsync(
        GetDashboardRequest request, CancellationToken cancellationToken = default);
}

public interface IDateProvider
{
    DateOnly Today { get; }        // fecha local de Windows (F0-T32)
}
```

### 7.1 Respuesta del Dashboard

```csharp
public sealed record DashboardSnapshot(
    DateOnly Month,
    int ActiveSubscriptions,
    IReadOnlyList<ScheduledCharge> UpcomingCharges,     // próximos 7 días
    IReadOnlyList<ScheduledCharge> OverdueCharges,      // vencidos pendientes
    long SubscriptionPaymentsCents,                     // pagos vigentes del mes
    long ExpensesCents,                                 // gastos confirmados del mes
    long TotalExpensesCents,                            // pagos + gastos
    long IncomeCents,                                   // ingresos confirmados del mes
    long OpeningBalanceCents,
    long CurrentBalanceCents,
    long ProjectedSavingsCents);
```

---

## 8. Algoritmos exactos

### 8.1 Próximo vencimiento (F0-T31)

```csharp
public static class NextChargeDateCalculator
{
    public static DateOnly CalculateNextDate(
        DateOnly lastScheduledDate,
        BillingFrequency frequency,
        int anchorDay)
    {
        if (anchorDay is < 1 or > 31)
            throw new ArgumentOutOfRangeException(nameof(anchorDay));

        var months = frequency switch
        {
            BillingFrequency.Monthly => 1,
            BillingFrequency.Quarterly => 3,
            BillingFrequency.SemiAnnual => 6,
            BillingFrequency.Annual => 12,
            _ => 0
        };

        if (months == 0)
        {
            return frequency switch
            {
                BillingFrequency.Daily => lastScheduledDate.AddDays(1),
                BillingFrequency.Weekly => lastScheduledDate.AddDays(7),
                _ => throw new ArgumentOutOfRangeException(nameof(frequency))
            };
        }

        var firstOfTargetMonth = new DateOnly(
            lastScheduledDate.Year, lastScheduledDate.Month, 1).AddMonths(months);

        var day = Math.Min(
            anchorDay,
            DateTime.DaysInMonth(firstOfTargetMonth.Year, firstOfTargetMonth.Month));

        return new DateOnly(firstOfTargetMonth.Year, firstOfTargetMonth.Month, day);
    }
}
```

Secuencia obligatoria de prueba (año no bisiesto):

```text
2027-01-31 → 2027-02-28 → 2027-03-31 → 2027-04-30 → 2027-05-31
```

> Este algoritmo **sustituye** la firma actual de ZYKO `CalculateNextDate(DateOnly, BillingFrequency)`.
> Ver conflicto **C-01**: su implementación actual devuelve `2027-03-28`, no `2027-03-31`.

### 8.2 Generación de cobros por rango (F0-01, F0-05, F0-09, F0-10, F0-25, F0-28)

```text
GenerateAsync(request):
1. Validar Until ≤ hoy + 12 meses  → si no, RANGO_EXCEDIDO.
2. Cargar suscripción              → si no existe, SUSCRIPCION_NO_ENCONTRADA.
3. Si IsActive == false y Until > DeactivatedOn → truncar Until = DeactivatedOn.
4. Tomar de la versión vigente: AmountCents, Frequency, AnchorDay.
5. Fecha candidata = primera fecha de la programación ≥ hoy.
6. Mientras candidata ≤ Until:
     a. Si ya existe cobro (suscripción, fecha) → omitir (idempotencia natural).
     b. Insertar cobro PENDIENTE con el importe de la versión vigente.
     c. candidata = CalculateNextDate(candidata, Frequency, AnchorDay).
7. Recalcular suscripciones.proximo_cobro = menor fecha PENDIENTE (o la próxima calculada).
8. Devolver los cobros creados, ordenados por fecha ascendente.
```

Reglas derivadas:

- Nunca se generan cobros anteriores a `StartDate` (salvo F0-25, que es explícito).
- Nunca se generan cobros posteriores a `DeactivatedOn` (F0-10).
- Un cobro ya existente no se duplica ni se modifica (F0-01).
- La generación **no** ocurre al consultar el Dashboard (F0-01: operación explícita).

Planificador puro que usa la implementación (sin acceso a datos):

```csharp
public static class ChargeSchedulePlanner
{
    /// <summary>Fechas que deben existir en el rango, sin duplicados y en orden ascendente.</summary>
    public static IReadOnlyList<DateOnly> Plan(
        DateOnly from,
        DateOnly until,
        DateOnly startDate,
        DateOnly? deactivatedOn,
        BillingFrequency frequency,
        int anchorDay);
}
```

`Plan` no consulta la base: Infrastructure descarta después las fechas que ya existen.
Así la regla de fechas queda probada por ZYKO sin SQLite.

### 8.3 Registro de pago (F0-03, F0-04, F0-18, F0-29)

```text
RegisterAsync(request):
1. Validar PaymentDate ≤ hoy                → si no, FECHA_FUTURA_NO_PERMITIDA.
2. BEGIN IMMEDIATE
3. Buscar pago por OperationId:
     - existe VIGENTE  → COMMIT y devolver el pago existente (reintento).
     - existe ANULADO  → rollback y OPERACION_YA_PROCESADA.
4. Cargar cobro                              → si no existe, COBRO_NO_ENCONTRADO.
     - estado PAGADO    → COBRO_YA_PAGADO
     - estado CANCELADO → COBRO_CANCELADO
5. Importe del pago = cobro.AmountCents      (F0-04: nunca parcial ni superior)
6. Número de comprobante:
     año = PaymentDate.Year
     secuencia = 'comprobante_pago:' + año
     siguiente = ultimo + 1 dentro de la transacción
     número = $"CMP-{año:D4}-{siguiente:D6}"
7. INSERT pago VIGENTE (operacion_id, comprobante, importe, fecha)
8. UPDATE cobro SET estado = 'PAGADO'
     WHERE id = @cobro AND estado = 'PENDIENTE'
     si changes != 1 → rollback + COBRO_YA_PAGADO
9. UPDATE suscripción SET proximo_cobro = CalculateNextDate(cobro.ScheduledDate, versión)
10. COMMIT
```

Notas:

- El paso 9 actualiza la fecha, **no crea** el cobro siguiente (ver C-05).
- El comprobante se numera por el **año de la fecha de pago**, no por el año del registro (C-07).
- Los comprobantes anulados conservan su número (F0-03).

### 8.4 Anulación de pago (F0-02)

```text
CancelAsync(request):
1. Validar motivo (1..500)                  → si no, MOTIVO_REQUERIDO.
2. BEGIN IMMEDIATE
3. Cargar pago                              → si no existe, PAGO_NO_ENCONTRADO.
4. Si ya está ANULADO → devolver éxito con el pago existente (reintento).
5. UPDATE pago SET estado='ANULADO', anulado_en=ahora, motivo=@motivo
6. UPDATE cobro SET estado='PENDIENTE'       WHERE id = @cobro AND estado='PAGADO'
7. Si cobro.ScheduledDate < suscripción.proximo_cobro
       → proximo_cobro = cobro.ScheduledDate
8. COMMIT
```

El pago anulado **nunca** se borra y deja de contar en los totales (F0-02, CONTRATOS).

### 8.5 Modificación y desactivación de suscripción (F0-09, F0-10, F0-23, F0-T06)

```text
UpdateAsync(request):
1. Validar EffectiveFrom ≥ hoy              → si no, FECHA_EFECTIVA_ANTERIOR.
2. Cargar suscripción                       → SUSCRIPCION_NO_ENCONTRADA / SUSCRIPCION_INACTIVA.
3. BEGIN IMMEDIATE
4. horizonte = MAX(fecha_programada) de sus cobros   (si no hay, EffectiveFrom)
5. INSERT versión (numero = MAX(numero)+1,
                   vigente_desde = EffectiveFrom,
                   importe/tipo/frecuencia nuevos,
                   dia_ancla = EffectiveFrom.Day)
6. UPDATE suscripciones con los valores nuevos.
7. DELETE cobros PENDIENTES con fecha_programada ≥ EffectiveFrom
       AND sin ningún pago asociado (protege el historial)
8. Regenerar cobros desde EffectiveFrom hasta horizonte con la versión nueva.
9. proximo_cobro = menor fecha PENDIENTE; si no queda ninguna, EffectiveFrom.
10. COMMIT
```

```text
DeactivateAsync(request):
1. Validar EffectiveFrom ≥ hoy              → si no, FECHA_EFECTIVA_ANTERIOR.
2. UPDATE suscripciones
       SET activa = 0, desactivada_en = @fecha
     WHERE id = @id AND activa = 1
     si changes != 1 → SUSCRIPCION_YA_INACTIVA
3. No se borra ni se altera ningún cobro ni pago existente (F0-10).
4. proximo_cobro = menor cobro PENDIENTE restante, o NULL si no hay.
```

### 8.6 Gastos previstos vs. confirmados (F0-07, F0-19, F0-T08)

| Acción | Efecto en SQLite | Efecto en Dashboard |
|---|---|---|
| Crear con `ActualDate == null` | `estado = PLANIFICADO`, `fecha_real = NULL` | Solo proyección |
| Crear con `ActualDate != null` | `estado = CONFIRMADO`, `fecha_real = @fecha` | Egreso real del mes |
| Confirmar | `PLANIFICADO → CONFIRMADO`, `fecha_real = @fecha` | Pasa a egreso real |
| Editar | Actualiza el registro y escribe una fila en `gastos_historial` | Usa solo el valor vigente |
| Anular | `estado = ANULADO`, conserva motivo y fecha | Deja de contar |

### 8.7 Ingresos (F0-06, F0-11, F0-T07)

| Tipo | `PlannedDate` | `ActualDate` | Estado inicial |
|---|---|---|---|
| Mensual | Día ancla de la plantilla | `null` | `Planned` |
| Quincenal | Día 15 o último del mes | `null` | `Planned` |
| Extraordinario | `ActualDate` | Obligatoria | `Confirmed` |

Un ingreso `Planned` **nunca** cuenta como dinero recibido (F0-06, F0-19).
Solo al confirmarlo pasa a `Confirmed` y suma al saldo (F0-11).

### 8.8 Dashboard (F0-11, F0-T12)

Cálculo puro en Core; Infrastructure solo reúne los insumos.

```csharp
public sealed record DashboardInputs(
    long OpeningBalanceCents,
    long ConfirmedIncomeCents,
    long ConfirmedExpensesCents,
    long ConfirmedPaymentsCents,
    long PlannedIncomeCents,
    long PendingChargesCents,
    long PlannedExpensesCents);

public static class DashboardCalculator
{
    public static DashboardSnapshot Calculate(
        DateOnly month,
        DashboardInputs inputs,
        int activeSubscriptions,
        IReadOnlyList<ScheduledCharge> upcomingCharges,
        IReadOnlyList<ScheduledCharge> overdueCharges)
    {
        var totalExpenses = checked(
            inputs.ConfirmedPaymentsCents + inputs.ConfirmedExpensesCents);

        var currentBalance = checked(
            inputs.OpeningBalanceCents + inputs.ConfirmedIncomeCents - totalExpenses);

        var projectedSavings = checked(
            currentBalance
            + inputs.PlannedIncomeCents
            - inputs.PendingChargesCents
            - inputs.PlannedExpensesCents);

        return new DashboardSnapshot(
            month,
            activeSubscriptions,
            upcomingCharges,
            overdueCharges,
            inputs.ConfirmedPaymentsCents,
            inputs.ConfirmedExpensesCents,
            totalExpenses,
            inputs.ConfirmedIncomeCents,
            inputs.OpeningBalanceCents,
            currentBalance,
            projectedSavings);
    }
}
```

Verificación con el ejemplo de F0-T12: saldo inicial 50 000 + ingreso 30 000 − gasto 4 000
= saldo actual 76 000 centavos.

Un resultado negativo significa déficit (CONTRATOS «Reglas de cálculo»).

---

## 9. Correspondencia con la base de datos

### 9.1 Entidad → tabla

| Entidad | Tabla |
|---|---|
| `Subscription` | `suscripciones` |
| `SubscriptionVersion` | `suscripcion_versiones` |
| `ScheduledCharge` | `cobros_programados` |
| `Payment` | `pagos` |
| `Expense` | `gastos` |
| `Income` | `ingresos` |
| `RecurringIncomeTemplate` | `ingresos_plantillas` |
| `ExpenseCategory` | `categorias_gasto` |
| `OpeningBalance` | `saldo_inicial` |
| `ExpenseCorrection` | `gastos_historial` |
| `IncomeCorrection` | `ingresos_historial` |
| `OpeningBalanceCorrection` | `saldo_inicial_historial` |

### 9.2 Propiedad → columna

| Entidad.Propiedad | Columna |
|---|---|
| `Subscription.Id` | `suscripciones.id` |
| `Subscription.Name` | `suscripciones.nombre` |
| `Subscription.OperationId` | `suscripciones.operacion_id` |
| `Subscription.AmountCents` | `suscripciones.monto_centavos` |
| `Subscription.AmountType` | `suscripciones.tipo_importe` |
| `Subscription.Frequency` | `suscripciones.frecuencia` |
| `Subscription.StartDate` | `suscripciones.fecha_inicio` |
| `Subscription.NextChargeDate` | `suscripciones.proximo_cobro` |
| `Subscription.IsActive` | `suscripciones.activa` |
| `Subscription.DeactivatedOn` | `suscripciones.desactivada_en` |
| `Subscription.AnchorDay` | *derivado*: `suscripcion_versiones.dia_ancla` de la versión vigente |
| `SubscriptionVersion.VersionNumber` | `suscripcion_versiones.numero_version` |
| `SubscriptionVersion.EffectiveFrom` | `suscripcion_versiones.vigente_desde` |
| `ScheduledCharge.ScheduledDate` | `cobros_programados.fecha_programada` |
| `ScheduledCharge.Status` | `cobros_programados.estado` |
| `Payment.ScheduledChargeId` | `pagos.cobro_id` |
| `Payment.OperationId` | `pagos.operacion_id` |
| `Payment.PaymentDate` | `pagos.fecha_pago` |
| `Payment.VoucherNumber` | `pagos.numero_comprobante` |
| `Payment.CancelledAt` | `pagos.anulado_en` |
| `Payment.CancellationReason` | `pagos.motivo_anulacion` |
| `Expense.CategoryId` | `gastos.categoria_id` |
| `Expense.PlannedDate` | `gastos.fecha_prevista` |
| `Expense.ActualDate` | `gastos.fecha_real` |
| `Income.TemplateId` | `ingresos.plantilla_id` |
| `Income.Type` | `ingresos.tipo` |
| `ExpenseCorrection.ExpenseId` | `gastos_historial.gasto_id` |
| `...Correction.ModifiedAt` | `..._historial.modificado_en` |

El resto de propiedades sigue la misma traducción directa inglés → español `snake_case`.

### 9.3 Errores SQLite → código de dominio

| Código SQLite | Código de dominio |
|---|---|
| `2067` `CONSTRAINT_UNIQUE` | Según la columna: `COBRO_DUPLICADO`, `COMPROBANTE_DUPLICADO`, `CATEGORIA_DUPLICADA` |
| `1555` `CONSTRAINT_PRIMARYKEY` | `ERROR_INTERNO` (id repetido es un fallo de programación) |
| `787` `CONSTRAINT_FOREIGNKEY` | `ERROR_INTERNO` (referencia inexistente) |
| `275` `CONSTRAINT_CHECK` | `ERROR_INTERNO` o validación específica si es predecible |
| `1299` `CONSTRAINT_NOTNULL` | `ERROR_INTERNO` |
| `5` `BUSY` / `6` `LOCKED` | `BASE_OCUPADA` (reintentar con espera acotada) |
| `11` `CORRUPT` / `26` `NOTADB` | `BASE_CORRUPTA` |
| `8` `READONLY` / `10` `IOERR` / `13` `FULL` / `14` `CANTOPEN` | `BASE_NO_DISPONIBLE` |
| Otro | `ERROR_INTERNO` |

Los errores de restricción **no se reintentan**; solo `BUSY` y `LOCKED`.

---

## 10. Pruebas que debe escribir ZYKO

Todas son unitarias puras, sin SQLite.

| # | Prueba | Decisión |
|---|---|---|
| 1 | `2027-01-31 → 2027-02-28 → 2027-03-31 → 2027-04-30` | F0-T31 |
| 2 | Año bisiesto: `2028-01-31 → 2028-02-29 → 2028-03-31` | F0-T31 |
| 3 | Diaria y semanal suman 1 y 7 días | Contratos |
| 4 | Trimestral, semestral y anual usan meses calendario | Contratos |
| 5 | `anchorDay` fuera de 1..31 lanza `ArgumentOutOfRangeException` | F0-T31 |
| 6 | El planificador de rango no repite fechas | F0-01 |
| 7 | El planificador respeta 12 meses | F0-28 |
| 8 | El planificador se detiene en `DeactivatedOn` | F0-10 |
| 9 | Egresos realizados = pagos + gastos | F0-11 |
| 10 | Saldo actual = saldo inicial + ingresos − egresos | F0-T12 |
| 11 | Ahorro proyectado = saldo + previstos − pendientes | F0-11 |
| 12 | Movimientos anulados no suman | F0-02, F0-T10 |
| 13 | Ingreso `Planned` no cuenta como recibido | F0-06 |
| 14 | Gasto `Planned` no reduce el saldo actual | F0-07 |
| 15 | Déficit se representa como valor negativo | Contratos |
| 16 | Un importe `long` que desborda lanza `OverflowException` | F0-T30 |
| 17 | `Result` fallido no expone `Value` | F0-15 |
| 18 | Multierror de validación conserva todos los códigos | F0-T33 |
| 19 | Quincenal acepta 15 y 31, rechaza 10 | F0-T01 |
| 20 | Extraordinario no admite plantilla | F0-T07 |
| 21 | Filtros de historial aplican fecha, categoría y estado | F0-T29 |
| 22 | Paginación: 50 por página, más reciente primero | F0-T29 |

COSMO cubre en SQLite las garantías físicas (ya implementadas en `DomainSchemaTests`):
un solo pago vigente, importe exacto, comprobante único, cobro no duplicado, anulación con
motivo, operación única, estados coherentes y saldo inicial único.

---

## 11. Conflictos y aclaraciones que requieren decisión humana

Cada punto enfrenta dos fuentes aprobadas. **Se propone una resolución; GRIM decide.**

### C-01 — Cálculo del próximo vencimiento en meses cortos

| Fuente | Texto |
|---|---|
| CONTRATOS §85-86 (19/10/2026) | «El cambio de día se conserva para los cobros posteriores. 31/01 → 28/02 → 28/03» |
| F0-T31 (posterior, aprobada) | «Conservará el día original… sin modificar permanentemente la programación. 31/01 → 28/02 → **31/03**» |

**Resolución propuesta:** prevalece F0-T31 por ser posterior y más específica.
**Impacto:** `NextChargeDateCalculator` de ZYKO debe recibir `anchorDay` y corregirse.
**Si se decide lo contrario:** hay que revertir F0-T31 y eliminar `dia_ancla` del esquema.

### C-02 — ¿Los repositorios son contratos de Core?

| Fuente | Texto |
|---|---|
| ARQUITECTURA §19 | «Implementación de repositorios definidos en Core» |
| F0-T03 | Solo enumera servicios (`ISubscriptionService`, `IScheduledChargeService`, `IPaymentService`…) |

**Resolución propuesta:** Core expone **servicios**; los repositorios son internos de
Infrastructure. Motivo: nadie fuera de Infrastructure los consume, WinForms tiene prohibido
usar SQL y un repositorio público obliga a Core a conocer conceptos de persistencia.
**Impacto:** ZYKO entrega 8 interfaces de servicio, no 7 de repositorio más 8 de servicio.

### C-03 — Idioma de los identificadores C#

| Fuente | Texto |
|---|---|
| D1 §4 y código de ZYKO | `ScheduledCharge`, `Payment`, `BillingFrequency` |
| CONTRATOS §17-21 | `MontoCentavos`, `FechaInicio`, `ProximoCobro` |

**Resolución propuesta:** C# en inglés, SQLite en español, interfaz en español.
**Impacto:** si se prefiere español en C#, el cambio es mecánico pero debe hacerse **antes**
de que ZYKO escriba las entidades, no después.

### C-04 — ¿Se puede registrar un pago con fecha futura?

No hay decisión explícita. F0-24 permite pagar **antes del vencimiento**, pero siempre un
pago ya realizado.

**Resolución propuesta:** `PaymentDate ≤ hoy` (fecha local, F0-T32).
**Impacto:** un pago «programado» no existe en el alcance; eso es un gasto planificado.

### C-05 — ¿Registrar un pago crea el cobro siguiente?

| Fuente | Texto |
|---|---|
| CONTRATOS §95 | «Se calcula el siguiente cobro según la frecuencia» |
| F0-01 | «Operación explícita y controlada, no una escritura oculta» |

**Resolución propuesta:** el pago actualiza `proximo_cobro` pero **no inserta** el cobro
siguiente. La generación es siempre explícita (`GenerateAsync`).
**Impacto:** GRIM debe invocar la sincronización al abrir la vista de cobros.

### C-06 — «Ingresos futuros confirmados» en F0-11

F0-11 usa «ingresos futuros confirmados», pero F0-06 y F0-19 establecen que un ingreso
futuro está **previsto** y solo se confirma al recibirlo.

**Resolución propuesta:** interpretar «futuros confirmados» como **previstos pendientes**.
Un ingreso ya confirmado es dinero recibido y nunca se proyecta dos veces.
**Impacto:** `DashboardInputs.PlannedIncomeCents`.

### C-07 — ¿Qué año define el número de comprobante?

F0-03 dice «numeración consecutiva por año» sin precisar el año.

**Resolución propuesta:** el año de `PaymentDate` (fecha del pago), que es la que se imprime
y la que determina el mes contable (F0-08).
**Impacto:** un pago de diciembre registrado en enero conserva la serie del año pagado.

---

## 12. Qué entrega cada uno

### ZYKO (Core)

```text
src/SubTrack.Core/
  Domain/Enums/            BillingFrequency (existe), AmountType,
                           ScheduledChargeStatus, PaymentStatus,
                           ExpenseStatus, IncomeStatus, IncomeType
  Domain/Entities/         12 entidades de §5
  Results/                 Result.cs, ResultOfT.cs, DomainError.cs, ErrorCodes.cs
  Abstractions/            8 interfaces de servicio de §7 + IDateProvider
  Contracts/               DTOs de §6, PageRequest, PagedResult, DashboardSnapshot
  Services/                NextChargeDateCalculator (corregir),
                           RecurrenceScheduleValidator (corregir),
                           FinancialCalculator (existe, ampliar),
                           DashboardCalculator (nuevo),
                           ChargeSchedulePlanner (nuevo)
```

### COSMO (Infrastructure)

```text
src/SubTrack.Infrastructure/Persistence/
  OfficialMigrations.cs                  (hecho: migración 0001)
  Repositories/                          7 repositorios internos
  Services/                              8 implementaciones de §7
  SqliteEnumCodeMapper.cs                (§2.3)
  SqliteErrorTranslator.cs               (§9.3)
  SystemDateProvider.cs                  (IDateProvider)
```

### GRIM (WinForms)

- Sustituir `Form1` por la navegación real.
- Cablear `SqliteDatabaseInitializer` → `SqliteDatabaseMigrator` → servicios (F9).
- Consumir únicamente las interfaces de §7; sin SQL y sin cálculos financieros.

---

## 13. Checklist de aprobación

- [ ] GRIM aprueba C-01 (vencimiento en meses cortos).
- [ ] GRIM aprueba C-02 (repositorios internos).
- [ ] GRIM aprueba C-03 (idioma de identificadores).
- [ ] GRIM aprueba C-04 (fecha de pago no futura).
- [ ] GRIM aprueba C-05 (generación explícita de cobros).
- [ ] GRIM aprueba C-06 (ingresos previstos en la proyección).
- [ ] GRIM aprueba C-07 (año del comprobante).
- [ ] ZYKO confirma que puede entregar §12 sin cambiar firmas.
- [ ] COSMO confirma la correspondencia de §9 contra la migración 0001.
- [ ] Se registra la aprobación en CONTRATOS-INTEGRACION (fecha y versión).
- [ ] Solo entonces se autoriza implementación (D1 §6).

---

*Fin del documento. Versión 1 — 25/10/2026.*
