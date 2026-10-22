# SUBTRACK 1.0 — FASE 1
## Auditoría de infraestructura y propuesta de persistencia SQLite

| Dato | Valor |
|---|---|
| Responsable | COSMO — Desarrollador 2 |
| Rama asignada | `feature/database` (no verificada con Git por prohibición expresa) |
| Fecha del informe | 20/10/2026 |
| Alcance | Análisis y propuesta. Sin implementación. |
| Estado | **Pendiente de revisión y autorización humana** |

---

## 0. Declaración de cumplimiento y método

- **No se ejecutó ningún comando Git**, ni de lectura.
- **No se ejecutó ningún comando de shell**, compilación ni prueba.
- **No se creó, modificó ni eliminó ningún archivo** durante la auditoría.
- **No se instalaron paquetes NuGet.**
- **No se creó ninguna base de datos ni se ejecutaron migraciones.**
- **No se modificaron Core, WinForms ni Tests.**
- El presente documento es el único archivo creado, a pedido explícito del usuario, para conservar el informe.

**Método:** inspección estática con herramientas de solo lectura sobre:

- `README.md`
- `docs/ARQUITECTURA.md`
- `docs/CONTRATOS-INTEGRACION.md`
- `src/SubTrack.Infrastructure` (`.csproj` y `Class1.cs`)
- `src/SubTrack.Core` (`.csproj` y `Class1.cs`)
- `src/SubTrack.WinForms` (`.csproj`, `Program.cs`, `Form1.cs`, `Form1.Designer.cs`)
- `tests/SubTrack.Tests` (`.csproj`, `UnitTest1.cs`)
- `SubTrack.slnx` y `.gitignore`

No se ejecutó `dotnet build` ni `dotnet test` porque generan artefactos `bin/obj` y pueden restaurar paquetes, lo que chocaría con la regla de no crear archivos. La compilación queda como validación pendiente y autorizable.

**Convención de lectura del informe:**

- **[APROBADO]**: decisión ya registrada en la documentación. No se reabre sin acuerdo del equipo.
- **[PROPUESTA COSMO]**: diseño técnico propuesto, sujeto a aprobación.
- **[BLOQUEANTE]**: requiere decisión antes de escribir código.
- **[RIESGO]**: punto que puede afectar la integración o la integridad.

---

## 1. Estado actual de la infraestructura

### 1.1 Proyectos inspeccionados

| Proyecto | Target | Referencias | Estado real |
|---|---|---|---|
| `SubTrack.Core` | `net10.0` | Ninguna | Solo `Class1.cs` vacío. Sin entidades, enums ni interfaces. |
| `SubTrack.Infrastructure` | `net10.0` | Core | Solo `Class1.cs` vacío. **Sin paquete SQLite**, sin conexión, sin repositorios, sin migraciones, sin respaldos. |
| `SubTrack.WinForms` | `net10.0-windows` | Core e Infrastructure | `Form1` por defecto. `Program.cs` ejecuta `new Form1()` sin composición de dependencias ni inicialización de base de datos. No contiene SQL. |
| `SubTrack.Tests` | `net10.0` | Core e Infrastructure | xUnit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1, coverlet 6.0.4. Solo una prueba vacía. |

### 1.2 Hallazgos principales

| ID | Hallazgo | Impacto |
|---|---|---|
| F-01 | `SubTrack.Infrastructure` es un esqueleto: no hay ninguna capacidad de persistencia implementada. | La fase parte de cero; no hay código previo que auditar ni conservar. |
| F-02 | No existe referencia a `Microsoft.Data.Sqlite`, EF Core ni ningún proveedor SQLite. | Agregar el paquete es una decisión pendiente y un cambio de dependencias que debe aprobarse. |
| F-03 | `SubTrack.Core` no tiene entidades, enums ni interfaces de repositorio. | **Bloqueante:** Infrastructure no puede implementar contratos que aún no existen. |
| F-04 | Los contratos funcionales están aprobados, pero las secciones iniciales de entidades en `CONTRATOS-INTEGRACION.md` están desactualizadas respecto de las decisiones posteriores. | **Bloqueante documental:** hay que reconciliar entidades antes de implementar. |
| F-05 | No existe resolución de ruta de base de datos, fábrica de conexiones ni inicialización en `Program.cs`. | La ubicación y el arranque de SQLite están sin definir. |
| F-06 | No existe estrategia de migraciones ni de respaldos. | Debe diseñarse antes de la primera tabla. |
| F-07 | El proyecto de Tests está vacío y es compartido por Core e Infrastructure. | Hay que acordar quién escribe las pruebas de persistencia y cómo se aíslan. |
| F-08 | `.gitignore` ya excluye `*.db`, `*.db-shm`, `*.db-wal`, `*.sqlite`, `*.sqlite3`, `backups/` y `TestResults/`. | Buena base; faltan patrones de journals y archivos temporales. |
| F-09 | No hay `global.json`, administración central de paquetes ni configuración de CI. | No es bloqueante, pero conviene fijar la versión del SDK y del paquete SQLite. |
| F-10 | WinForms no contiene SQL ni acceso directo a datos. | Correcto respecto de la arquitectura; debe mantenerse. |

### 1.3 Contratos: lo aprobado y lo que falta

#### [APROBADO] Decisiones funcionales registradas

| Tema | Decisión registrada |
|---|---|
| Identificadores | `Guid`; almacenados en SQLite como `TEXT` en formato estándar; relaciones con `Guid`. |
| Frecuencias | Diaria, semanal, mensual, trimestral, semestral y anual. |
| Próximo cobro | Se calcula desde la última fecha programada; usa meses calendario y conserva el ajuste de fin de mes. Lo implementa ZYKO. |
| Registro de pagos | Manual; cada pago genera comprobante interno; el cobro queda pagado; el historial se conserva; registro y actualización del cobro en una misma transacción SQLite. |
| Pagos anticipados/atrasados | Se conserva la fecha programada y la fecha real; la fecha real no mueve el calendario. |
| Duplicados | Un cobro programado solo puede pagarse una vez; se exige restricción `UNIQUE` en SQLite y protección ante concurrencia. |
| Anulación de pagos | No se elimina el historial; se registra fecha y motivo; el pago pasa a Anulado; el cobro vuelve a Pendiente; la anulación es transaccional. |
| Gastos | Guid, concepto, categoría, monto en centavos, fecha, edición y anulación con historial; excluidos de totales si están anulados. |
| Ingresos | Guid, concepto, monto en centavos, fecha, tipo mensual o extraordinario, corrección/anulación con historial. |
| Dashboard | Nueve indicadores y fórmulas definidas; meses calendario; anulados excluidos; importes en centavos. |
| Arquitectura | Core no depende de WinForms ni SQLite; Infrastructure depende de Core; WinForms depende de ambos y no ejecuta SQL; claves foráneas activas; `DateOnly` para fechas de negocio. |

#### [BLOQUEANTE] Contratos técnicos aún no definidos

1. Nombres y firmas exactas de las interfaces C#.
2. Enums definitivos: frecuencia, estado de cobro, estado de pago, tipo de ingreso, estado de gasto/ingreso, categorías.
3. Contrato de **cobros programados**: la sección inicial de contratos no lo define como entidad.
4. Contrato de **pago**: la sección inicial solo tiene `Id`, `SuscripcionId`, `MontoCentavos` y `FechaPago`; las decisiones posteriores exigen `CobroId`, `Estado`, comprobante, fecha y motivo de anulación.
5. Contrato de gasto: falta `Categoria`, `Estado` y campos de anulación en la sección inicial.
6. Contrato de ingreso: falta `Estado` y campos de anulación en la sección inicial.
7. Semántica de ingresos futuros, gastos futuros e ingresos "confirmados" para el ahorro proyectado.
8. Política de errores y validaciones: ¿excepciones, `Result`, códigos?
9. ¿Las APIs serán sincrónicas o asincrónicas?
10. ¿Quién genera el número de comprobante y con qué formato?
11. ¿Quién genera los cobros programados y con qué horizonte?
12. ¿Infrastructure devuelve datos crudos para el Dashboard o ejecuta las fórmulas financieras?

> **Observación importante:** `CONTRATOS-INTEGRACION.md` declara al inicio "PROPUESTA PENDIENTE DE APROBACION", pero al final registra "acuerdos funcionales aprobados". La interpretación razonable es: **lo funcional está aprobado; lo técnico sigue pendiente**. Esa distinción debe quedar escrita por el equipo para evitar que alguien implemente sobre una sección desactualizada.

---

## 2. Esquema SQLite propuesto

> **Nombre y columnas son [PROPUESTA COSMO].** No son decisiones aprobadas. No se ejecutó este DDL.

### 2.1 Convenciones propuestas

| Dato | Representación | Motivo |
|---|---|---|
| `Guid` | `TEXT` de 36 caracteres, formato `D`, minúsculas | Legible, estable, coincide con la decisión aprobada. |
| Dinero | `INTEGER` en centavos (`long` en C#) | Evita punto flotante; SQLite `INTEGER` es 64 bits. |
| Fecha de negocio | `TEXT` ISO `yyyy-MM-dd` (`DateOnly`) | Ordenable, sin zona horaria. |
| Fecha/hora técnica | `TEXT` ISO UTC `yyyy-MM-ddTHH:mm:ss.fffZ` | Auditoría inequívoca. |
| Enums | `TEXT` con códigos estables y `CHECK` | Legible y resistente a cambios de orden. |
| Booleanos | `INTEGER` 0/1 con `CHECK` | SQLite no tiene `BOOLEAN`. |
| Nombres | `snake_case` en español | Consistencia con el dominio. |
| Moneda | Una sola moneda implícita (USD en presentación) | Multimoneda no está aprobada; si se requiere, se agrega `moneda`. |

### 2.2 Diagrama relacional

```text
suscripciones 1 ─── N cobros_programados 1 ─── N pagos
gastos          (independiente)
ingresos        (independiente)
esquema_migraciones (infraestructura)
secuencias       (opcional, para comprobantes)
```

- `pagos` **no** lleva `suscripcion_id` como columna: se deriva mediante `cobro_id → suscripcion_id`. Esto evita inconsistencias entre el pago y la suscripción.
- No se propone `ON DELETE CASCADE` en ninguna relación financiera. El historial no se borra.

### 2.3 DDL propuesto — tablas de dominio

```sql
-- PROPUESTA COSMO — NO EJECUTAR
-- 0001_esquema_inicial.sql

PRAGMA application_id = 1398096468;  -- 'SUBT'
PRAGMA user_version = 1;

CREATE TABLE suscripciones (
    id              TEXT    NOT NULL PRIMARY KEY
                            CHECK (length(id) = 36 AND id = lower(id)),
    nombre          TEXT    NOT NULL
                            CHECK (length(trim(nombre)) BETWEEN 1 AND 200),
    monto_centavos  INTEGER NOT NULL CHECK (monto_centavos > 0),
    frecuencia      TEXT    NOT NULL
                            CHECK (frecuencia IN
                                ('DIARIA','SEMANAL','MENSUAL',
                                 'TRIMESTRAL','SEMESTRAL','ANUAL')),
    fecha_inicio    TEXT    NOT NULL
                            CHECK (fecha_inicio GLOB
                                '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'
                                AND date(fecha_inicio) = fecha_inicio),
    proximo_cobro   TEXT    NOT NULL
                            CHECK (proximo_cobro GLOB
                                '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'
                                AND date(proximo_cobro) = proximo_cobro),
    activa          INTEGER NOT NULL CHECK (activa IN (0, 1)),
    creado_en       TEXT    NOT NULL,
    actualizado_en  TEXT    NOT NULL
);

CREATE TABLE cobros_programados (
    id                TEXT    NOT NULL PRIMARY KEY
                              CHECK (length(id) = 36 AND id = lower(id)),
    suscripcion_id    TEXT    NOT NULL,
    fecha_programada  TEXT    NOT NULL
                              CHECK (fecha_programada GLOB
                                  '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'
                                  AND date(fecha_programada) = fecha_programada),
    monto_centavos    INTEGER NOT NULL CHECK (monto_centavos > 0),
    estado            TEXT    NOT NULL
                              CHECK (estado IN ('PENDIENTE','PAGADO')),
    creado_en         TEXT    NOT NULL,
    actualizado_en    TEXT    NOT NULL,
    CONSTRAINT fk_cobro_suscripcion
        FOREIGN KEY (suscripcion_id) REFERENCES suscripciones(id)
        ON DELETE RESTRICT ON UPDATE RESTRICT,
    CONSTRAINT ux_cobro_suscripcion_fecha
        UNIQUE (suscripcion_id, fecha_programada)
);

CREATE TABLE pagos (
    id                  TEXT    NOT NULL PRIMARY KEY
                                CHECK (length(id) = 36 AND id = lower(id)),
    cobro_id            TEXT    NOT NULL,
    fecha_pago          TEXT    NOT NULL
                                CHECK (fecha_pago GLOB
                                    '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'
                                    AND date(fecha_pago) = fecha_pago),
    monto_centavos      INTEGER NOT NULL CHECK (monto_centavos > 0),
    estado              TEXT    NOT NULL
                                CHECK (estado IN ('VIGENTE','ANULADO')),
    numero_comprobante  TEXT    NOT NULL
                                CHECK (length(trim(numero_comprobante)) BETWEEN 1 AND 50),
    anulado_en          TEXT    NULL,
    motivo_anulacion    TEXT    NULL,
    creado_en           TEXT    NOT NULL,
    actualizado_en      TEXT    NOT NULL,
    CONSTRAINT fk_pago_cobro
        FOREIGN KEY (cobro_id) REFERENCES cobros_programados(id)
        ON DELETE RESTRICT ON UPDATE RESTRICT,
    CONSTRAINT ux_pago_comprobante UNIQUE (numero_comprobante),
    CONSTRAINT ck_pago_anulacion CHECK (
        (estado = 'VIGENTE'
         AND anulado_en IS NULL
         AND motivo_anulacion IS NULL)
        OR
        (estado = 'ANULADO'
         AND anulado_en IS NOT NULL
         AND motivo_anulacion IS NOT NULL
         AND length(trim(motivo_anulacion)) BETWEEN 1 AND 500)
    )
);

-- Clave para permitir un nuevo pago tras anular el anterior:
-- como máximo un pago VIGENTE por cobro, pero varios ANULADOS.
CREATE UNIQUE INDEX ux_pagos_cobro_vigente
    ON pagos(cobro_id)
    WHERE estado = 'VIGENTE';

CREATE TABLE gastos (
    id               TEXT    NOT NULL PRIMARY KEY
                             CHECK (length(id) = 36 AND id = lower(id)),
    concepto         TEXT    NOT NULL
                             CHECK (length(trim(concepto)) BETWEEN 1 AND 200),
    categoria        TEXT    NOT NULL
                             CHECK (length(trim(categoria)) BETWEEN 1 AND 100),
    monto_centavos   INTEGER NOT NULL CHECK (monto_centavos > 0),
    fecha            TEXT    NOT NULL
                             CHECK (fecha GLOB
                                 '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'
                                 AND date(fecha) = fecha),
    estado           TEXT    NOT NULL
                             CHECK (estado IN ('VIGENTE','ANULADO')),
    anulado_en       TEXT    NULL,
    motivo_anulacion TEXT    NULL,
    creado_en        TEXT    NOT NULL,
    actualizado_en   TEXT    NOT NULL,
    CONSTRAINT ck_gasto_anulacion CHECK (
        (estado = 'VIGENTE'
         AND anulado_en IS NULL
         AND motivo_anulacion IS NULL)
        OR
        (estado = 'ANULADO'
         AND anulado_en IS NOT NULL
         AND motivo_anulacion IS NOT NULL
         AND length(trim(motivo_anulacion)) BETWEEN 1 AND 500)
    )
);

CREATE TABLE ingresos (
    id               TEXT    NOT NULL PRIMARY KEY
                             CHECK (length(id) = 36 AND id = lower(id)),
    concepto         TEXT    NOT NULL
                             CHECK (length(trim(concepto)) BETWEEN 1 AND 200),
    monto_centavos   INTEGER NOT NULL CHECK (monto_centavos > 0),
    fecha            TEXT    NOT NULL
                             CHECK (fecha GLOB
                                 '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'
                                 AND date(fecha) = fecha),
    tipo             TEXT    NOT NULL
                             CHECK (tipo IN ('MENSUAL','EXTRAORDINARIO')),
    estado           TEXT    NOT NULL
                             CHECK (estado IN ('VIGENTE','ANULADO')),
    anulado_en       TEXT    NULL,
    motivo_anulacion TEXT    NULL,
    creado_en        TEXT    NOT NULL,
    actualizado_en   TEXT    NOT NULL,
    CONSTRAINT ck_ingreso_anulacion CHECK (
        (estado = 'VIGENTE'
         AND anulado_en IS NULL
         AND motivo_anulacion IS NULL)
        OR
        (estado = 'ANULADO'
         AND anulado_en IS NOT NULL
         AND motivo_anulacion IS NOT NULL
         AND length(trim(motivo_anulacion)) BETWEEN 1 AND 500)
    )
);
```

### 2.4 Tablas de infraestructura

```sql
CREATE TABLE esquema_migraciones (
    version     INTEGER NOT NULL PRIMARY KEY CHECK (version > 0),
    nombre      TEXT    NOT NULL,
    checksum    TEXT    NOT NULL CHECK (length(checksum) = 64),
    aplicada_en TEXT    NOT NULL
);

-- Opcional, solo si se aprueba numeración secuencial de comprobantes.
CREATE TABLE secuencias (
    nombre TEXT    NOT NULL PRIMARY KEY,
    ultimo INTEGER NOT NULL CHECK (ultimo >= 0)
);

INSERT INTO secuencias(nombre, ultimo) VALUES ('comprobante_pago', 0);
```

### 2.5 Índices propuestos

```sql
CREATE INDEX ix_cobros_estado_fecha
    ON cobros_programados(estado, fecha_programada);

CREATE INDEX ix_cobros_suscripcion
    ON cobros_programados(suscripcion_id);

CREATE INDEX ix_pagos_fecha_estado
    ON pagos(fecha_pago, estado);

CREATE INDEX ix_pagos_cobro
    ON pagos(cobro_id, estado);

CREATE INDEX ix_gastos_fecha_estado
    ON gastos(fecha, estado);

CREATE INDEX ix_gastos_categoria
    ON gastos(categoria, estado);

CREATE INDEX ix_ingresos_fecha_estado
    ON ingresos(fecha, estado);

CREATE INDEX ix_ingresos_tipo
    ON ingresos(tipo, estado);

-- Índice parcial para el Dashboard de suscripciones activas.
CREATE INDEX ix_suscripciones_activas
    ON suscripciones(proximo_cobro)
    WHERE activa = 1;
```

---

## 3. Relaciones, índices y restricciones

### 3.1 Relaciones

| Relación | Cardinalidad | Regla |
|---|---|---|
| `suscripciones` → `cobros_programados` | 1:N | Un cobro pertenece a una suscripción. `ON DELETE RESTRICT`. |
| `cobros_programados` → `pagos` | 1:N | Varios pagos históricos; como máximo uno vigente. `ON DELETE RESTRICT`. |
| `suscripciones` → `pagos` | 1:N derivada | Se obtiene por join; no se duplica la FK. |
| `gastos` | Independiente | Sin FK a suscripciones ni pagos. |
| `ingresos` | Independiente | Sin FK. |

### 3.2 Restricciones de integridad

| Restricción | Dónde | Propósito |
|---|---|---|
| PK `TEXT` con longitud 36 | Todas las tablas | Evitar Ids mal formados. |
| FK con `RESTRICT` | `cobros`, `pagos` | Impedir borrar historial financiero. |
| `UNIQUE(suscripcion_id, fecha_programada)` | `cobros_programados` | Evitar dos cobros iguales para la misma suscripción y fecha. |
| `UNIQUE(numero_comprobante)` | `pagos` | Evitar comprobantes duplicados. |
| Índice único parcial `estado='VIGENTE'` | `pagos` | Un solo pago vigente por cobro; permite varios anulados. |
| `CHECK(monto_centavos > 0)` | Todas las tablas financieras | Rechazar importes inválidos. |
| `CHECK` de fecha ISO | Todas las fechas de negocio | Rechazar formatos o fechas imposibles. |
| `CHECK` de estados | Todas las tablas | Evitar estados fuera del dominio. |
| `CHECK` de coherencia de anulación | `pagos`, `gastos`, `ingresos` | Un anulado debe tener fecha y motivo; un vigente no puede tenerlos. |
| `NOT NULL` | Campos obligatorios | Evitar filas incompletas. |

> **Detalle SQLite importante:** un `CHECK` que evalúa a `NULL` **no falla**. Por eso la rama de anulación exige explícitamente `motivo_anulacion IS NOT NULL`; no alcanza con `length(trim(...)) BETWEEN ...`.

### 3.3 PRAGMAs propuestos

```sql
PRAGMA foreign_keys = ON;       -- por conexión; SQLite lo trae OFF por defecto
PRAGMA journal_mode = WAL;      -- persistente en el archivo
PRAGMA synchronous = FULL;      -- por conexión; durabilidad financiera
PRAGMA busy_timeout = 5000;     -- por conexión; espera ante escritor ocupado
PRAGMA encoding = 'UTF-8';      -- solo al crear la base
PRAGMA application_id = 1398096468; -- identifica la base como SubTrack
```

Notas:

- `foreign_keys` debe activarse **en cada conexión**, antes de cualquier transacción. No se puede cambiar dentro de una transacción.
- `journal_mode=WAL` se configura una vez, pero conviene verificarlo al abrir.
- `synchronous=FULL` prioriza durabilidad ante corte de energía. En WAL, `NORMAL` puede perder las últimas transacciones; para datos financieros se recomienda `FULL`, aceptando el costo.
- `application_id` permite rechazar la restauración de un archivo SQLite ajeno.
- Evaluar `STRICT` tables si se aprueba: agrega validación de tipos, pero requiere una versión de SQLite que soporte esa característica. No se da por aprobado.

### 3.4 Integridad ante cierres inesperados

- WAL + `synchronous=FULL` + transacciones cortas.
- Toda operación que afecte pago y cobro se hace en una sola transacción.
- Al iniciar: `PRAGMA quick_check`; si hubo cierre anormal o se detecta un problema, `PRAGMA integrity_check`.
- Después de migraciones y restauraciones: `PRAGMA foreign_key_check` e `integrity_check`.
- No usar `journal_mode=OFF` ni `MEMORY`.
- No copiar el archivo `.db` mientras la aplicación lo tiene abierto y hay WAL activo.

---

## 4. Estrategia de prevención de pagos duplicados

### 4.1 Regla

**Un cobro programado puede tener como máximo un pago `VIGENTE`; puede tener cualquier cantidad de pagos `ANULADO`.**

La regla se implementa con:

```sql
CREATE UNIQUE INDEX ux_pagos_cobro_vigente
    ON pagos(cobro_id)
    WHERE estado = 'VIGENTE';
```

Este índice parcial es la garantía definitiva, incluso si dos intentos ocurren al mismo tiempo. No alcanza con consultar antes de insertar: entre la consulta y el insert existe una ventana de carrera.

### 4.2 Ejemplo de historial permitido

| Pago | Cobro | Estado | Comprobante |
|---|---|---|---|
| P1 | C1 | `ANULADO` | `CMP-2026-000001` |
| P2 | C1 | `VIGENTE` | `CMP-2026-000002` |
| P3 | C1 | — | Rechazado por el índice único |

- P1 se conserva para siempre.
- P2 es el pago válido actual.
- P3 no puede insertarse mientras P2 esté vigente.
- Si P2 se anula, P3 puede registrarse; P1 y P2 siguen en el historial.

### 4.3 Secuencia bajo concurrencia

```text
T1: BEGIN IMMEDIATE
    SELECT cobro C1 → PENDIENTE
    INSERT pago P1 (VIGENTE)
    UPDATE cobro C1 → PAGADO
    COMMIT

T2: BEGIN IMMEDIATE  (espera el lock de escritura)
    SELECT cobro C1 → PAGADO
    → rechaza con "Este cobro ya fue registrado como pagado"
```

Si T2 hubiera leído antes del commit de T1, el `INSERT` falla igual por el índice único. El error se traduce a un conflicto de dominio, no se propaga el error crudo de SQLite.

### 4.4 Doble protección recomendada

1. **Índice único parcial** como garantía física.
2. **Actualización condicional** del cobro:

```sql
UPDATE cobros_programados
SET estado = 'PAGADO', actualizado_en = @ahora
WHERE id = @cobroId AND estado = 'PENDIENTE';
```

Si `changes != 1`, se hace rollback y se informa duplicado. Esto evita que un pago quede insertado sin que el cobro haya cambiado de estado.

3. **Validación en Core** para dar un mensaje temprano y amigable. Core no reemplaza la restricción de SQLite.

### 4.5 Otros registros financieros duplicados

| Caso | Protección propuesta |
|---|---|
| Dos cobros para la misma suscripción y fecha | `UNIQUE(suscripcion_id, fecha_programada)`. |
| Dos comprobantes iguales | `UNIQUE(numero_comprobante)`. |
| Doble clic al registrar un gasto o ingreso | Clave de idempotencia generada por el formulario y `UNIQUE` parcial, o botón deshabilitado + validación. **Pendiente de acuerdo con GRIM/ZYKO.** |
| Gasto que replica un pago de suscripción | Regla de negocio en Core/GRIM; no conviene imponerla con un `UNIQUE` de contenido porque dos gastos legítimos pueden coincidir en concepto, fecha y monto. |

> **No se propone** un `UNIQUE` por "huella" de concepto+monto+fecha para gastos e ingresos: bloquearía duplicados legítimos. La protección correcta para doble envío es una clave de idempotencia por operación.

### 4.6 Errores SQLite a mapear

| Código | Situación | Acción |
|---|---|---|
| `2067` (`SQLITE_CONSTRAINT_UNIQUE`) | Pago duplicado, comprobante duplicado, cobro repetido | Conflicto de dominio; no reintentar. |
| `1555` (`SQLITE_CONSTRAINT_PRIMARYKEY`) | Id repetido | Error de programación o reintento; revisar. |
| `787` (`SQLITE_CONSTRAINT_FOREIGNKEY`) | Referencia inexistente | Error de datos; no reintentar. |
| `275` (`SQLITE_CONSTRAINT_CHECK`) | Importe, fecha o estado inválido | Error de validación. |
| `1299` (`SQLITE_CONSTRAINT_NOTNULL`) | Campo obligatorio ausente | Error de validación. |
| `5` (`SQLITE_BUSY`) / `6` (`SQLITE_LOCKED`) | Otro escritor o bloqueo | Reintentar con espera acotada; luego mensaje "base ocupada". |
| `8` (`SQLITE_READONLY`) | Permisos o medio de solo lectura | Error de configuración; no reintentar. |
| `10` (`SQLITE_IOERR`) | Error de E/S | Registrar; evaluar recuperación. |
| `11` (`SQLITE_CORRUPT`) | Base corrupta | Detener escrituras; ofrecer restauración; preservar archivo. |
| `13` (`SQLITE_FULL`) | Disco lleno | Detener; no borrar respaldos. |
| `14` (`SQLITE_CANTOPEN`) | Ruta o permisos | Error de configuración. |
| `26` (`SQLITE_NOTADB`) | Archivo no es SQLite | Rechazar; no sobrescribir. |

Solo se reintentan `BUSY` y `LOCKED`. Los errores de restricción no se reintentan.

---

## 5. Diseño de transacciones y anulaciones

### 5.1 Principio

- **Core** valida reglas de negocio y calcula fechas/importes.
- **Infrastructure** abre la transacción, ejecuta las operaciones atómicas, aplica restricciones y traduce errores.
- **WinForms** llama a una operación de Core/Infrastructure; nunca abre conexiones ni ejecuta SQL.

### 5.2 Registro de pago

Secuencia propuesta dentro de una transacción `BEGIN IMMEDIATE`:

```text
1. BEGIN IMMEDIATE
2. Leer el cobro por id.
   - No existe → COBRO_NO_ENCONTRADO
   - estado != PENDIENTE → COBRO_YA_PAGADO
3. Validar que el importe coincida con el cobro (si se aprueba pago total).
4. Insertar el pago en estado VIGENTE con número de comprobante único.
5. UPDATE cobro SET estado='PAGADO'
   WHERE id=@cobroId AND estado='PENDIENTE'.
   - changes != 1 → rollback + COBRO_YA_PAGADO
6. Persistir el siguiente cobro programado, si Core lo indica.
7. Actualizar suscripciones.proximo_cobro en la misma transacción.
8. COMMIT
```

Cualquier fallo en 4–7 hace rollback completo: no queda un pago sin cobro actualizado ni un cobro pagado sin pago.

### 5.3 Anulación de pago

```text
1. BEGIN IMMEDIATE
2. Leer el pago por id.
   - No existe → PAGO_NO_ENCONTRADO
   - estado != VIGENTE → PAGO_YA_ANULADO
3. UPDATE pagos
   SET estado='ANULADO', anulado_en=@ahora,
       motivo_anulacion=@motivo, actualizado_en=@ahora
   WHERE id=@pagoId AND estado='VIGENTE'.
   - changes != 1 → rollback + CONFLICTO
4. UPDATE cobros SET estado='PENDIENTE'
   WHERE id=@cobroId AND estado='PAGADO'.
   - changes != 1 → rollback + INCONSISTENCIA
5. COMMIT
```

Después del commit, el cobro vuelve a estar pendiente y se puede registrar un nuevo pago. El pago anulado permanece con su motivo y fecha.

### 5.4 Qué pasa con el siguiente cobro al anular

Hay una decisión pendiente:

- Si al pagar se creó el cobro siguiente, al anular el pago anterior **el cobro siguiente ya existe**.
- Se propone conservarlo: el cobro anulado vuelve a pendiente y el siguiente sigue pendiente. No se borra historial ni se pierde la programación.
- La alternativa —eliminar el cobro siguiente al anular— requiere una regla de negocio explícita y puede perder información. **No se recomienda sin aprobación de ZYKO.**

### 5.5 Generación del siguiente cobro

**Decisión pendiente y de alto impacto.** Hay tres opciones:

| Opción | Descripción | Ventaja | Riesgo |
|---|---|---|---|
| A. Un solo cobro próximo | Se crea el siguiente al registrar la suscripción y se crea el próximo al pagar. | Simple; cumple el contrato de "cobro correspondiente". | Para frecuencias semanales/diarias, el Dashboard del mes no ve todas las obligaciones futuras. |
| B. Horizonte rodante | Se generan cobros por N meses/días hacia adelante. | Dashboard completo. | Editar/desactivar la suscripción obliga a cancelar/recalcular cobros futuros. |
| C. Generación idempotente por rango | Antes de consultar el Dashboard se aseguran los cobros hasta una fecha, con `UNIQUE` y `ON CONFLICT DO NOTHING`. | Equilibrio; no duplica. | Introduce escrituras durante una lectura de Dashboard y requiere transacción. |

**Recomendación COSMO:** definir primero la semántica del Dashboard. Si "obligaciones pendientes del mes" debe incluir todos los cobros semanales/diarios del mes, la opción A no alcanza. La opción C es la más compatible con el esquema propuesto, pero debe aprobarla ZYKO porque implica una operación de sincronización de cobros.

### 5.6 Consistencia entre `cobros.estado` y `pagos`

El estado del cobro se mantiene transaccionalmente, pero es un dato derivable. Para detectar divergencias se propone una consulta de invariante en pruebas y en diagnóstico:

```sql
SELECT c.id
FROM cobros_programados c
LEFT JOIN pagos p
       ON p.cobro_id = c.id AND p.estado = 'VIGENTE'
WHERE (c.estado = 'PAGADO'  AND p.id IS NULL)
   OR (c.estado = 'PENDIENTE' AND p.id IS NOT NULL);
```

Debe devolver cero filas. También:

```sql
SELECT cobro_id, COUNT(*) AS vigentes
FROM pagos
WHERE estado = 'VIGENTE'
GROUP BY cobro_id
HAVING COUNT(*) > 1;
```

El índice único parcial ya impide el segundo caso; la consulta sirve como verificación.

### 5.7 Inmutabilidad del historial

Propuesta adicional, pendiente de aprobación:

- No permitir `DELETE` físico sobre `pagos`, `gastos` ni `ingresos`.
- Una corrección de pago se hace anulando y registrando uno nuevo.
- Para gastos e ingresos, el contrato permite edición; si se edita, se pierde el valor anterior salvo que se agregue una tabla de auditoría.
- Si se requiere historial completo de ediciones, agregar `eventos_auditoria` (entidad, entidad_id, evento, ocurrido_en, motivo, datos_json). **No se incluye por defecto** porque el contrato solo exige historial de anulación.

---

## 6. Estrategia de migraciones

### 6.1 Mecanismo propuesto

- Scripts SQL incrementales, embebidos como recursos del proyecto `SubTrack.Infrastructure`.
- Nombres ordenables: `0001_esquema_inicial.sql`, `0002_...sql`, etc.
- Tabla `esquema_migraciones` como fuente de verdad.
- `PRAGMA user_version` como espejo rápido de la versión.
- Checksum SHA-256 de cada script para detectar modificaciones posteriores.
- Sin `EnsureCreated` ni creación implícita de esquema desde el ORM.

### 6.2 Algoritmo de arranque

1. Resolver la ruta de la base y crear el directorio si no existe.
2. Abrir conexión y aplicar PRAGMAs de conexión.
3. Verificar `application_id`; si la base es nueva, fijarlo.
4. Crear `esquema_migraciones` si no existe.
5. Leer versión aplicada y checksums.
6. Si `user_version` es mayor que la máxima soportada por la aplicación → abortar con mensaje de versión incompatible.
7. Si hay migraciones pendientes:
   - Crear respaldo previo.
   - Para cada migración: `BEGIN IMMEDIATE`, ejecutar script, insertar fila en `esquema_migraciones`, actualizar `user_version`, `COMMIT`.
8. Si falla una migración: rollback, conservar respaldo, abortar y mostrar error.
9. Ejecutar `PRAGMA foreign_key_check` y `PRAGMA quick_check`.
10. Cerrar y continuar con el arranque de la aplicación.

### 6.3 Base nueva: inicialización atómica

Propuesta:

1. Crear la base en un archivo temporal dentro del mismo directorio.
2. Aplicar todas las migraciones.
3. Verificar `integrity_check`, `foreign_key_check`, `application_id` y `user_version`.
4. Cerrar la conexión.
5. Mover el archivo temporal a la ruta definitiva con operación atómica en el mismo volumen.
6. Si el archivo definitivo ya existe, no sobrescribir: abrir y migrar el existente.

Así no queda una base a medio inicializar si el proceso falla.

### 6.4 Reglas de migración

- Cada migración debe ser idempotente desde la versión anterior, no desde cero.
- Los scripts aplicados son inmutables; si se corrige uno, se crea una migración nueva.
- Un checksum distinto para una migración ya aplicada debe abortar el arranque.
- No se soportan downgrades automáticos.
- Las migraciones que reconstruyen tablas deben seguir el procedimiento de SQLite: desactivar `foreign_keys` fuera de la transacción, crear tabla nueva, copiar, borrar, renombrar, verificar FK y reactivar.
- `PRAGMA journal_mode` y `PRAGMA foreign_keys` no se cambian dentro de una transacción.
- No se ejecutan migraciones desde WinForms.

### 6.5 Concurrencia de migraciones

- `BEGIN IMMEDIATE` para tomar el lock de escritura.
- Releer la versión dentro de la transacción.
- Si otra instancia ya migró, no hacer nada.
- Además, se recomienda una instancia única de la aplicación (mutex con nombre) para evitar dos procesos migrando o restaurando a la vez. **Pendiente de acuerdo con GRIM.**

---

## 7. Estrategia de respaldos y restauración

### 7.1 Ubicación propuesta

```text
%LOCALAPPDATA%\SubTrack\data\subtrack.db
%LOCALAPPDATA%\SubTrack\backups\subtrack-YYYYMMDD-HHmmssZ.db
```

Motivos:

- `LocalApplicationData` es escribible por el usuario y no requiere permisos de administrador.
- No está dentro del repositorio.
- No está en `Program Files`.
- No está en una carpeta sincronizada por OneDrive por defecto, lo que reduce el riesgo de corrupción por sincronización.

Se propone permitir una ruta alternativa por configuración o variable de entorno para pruebas y modo portable. **Pendiente de aprobación.**

### 7.2 Respaldo consistente con la base en uso

**No copiar el archivo `.db` con el explorador mientras la aplicación está abierta**, especialmente en WAL: la copia puede no incluir el contenido del `-wal`.

Opciones:

1. **API de backup en línea de SQLite** (`SqliteConnection.BackupDatabase`): copia consistente mientras la base está en uso. **Recomendada.**
2. **`VACUUM INTO 'archivo'`**: genera una copia compacta y consistente; requiere que el destino no exista y no puede ejecutarse dentro de una transacción. Útil como alternativa.

### 7.3 Validaciones del respaldo

Después de crear el respaldo:

```sql
PRAGMA integrity_check;
PRAGMA foreign_key_check;
PRAGMA application_id;
PRAGMA user_version;
```

Debe verificarse:

- `integrity_check` devuelve `ok`.
- `foreign_key_check` no devuelve filas.
- `application_id` corresponde a SubTrack.
- `user_version` es conocido por la aplicación.
- El archivo tiene tamaño razonable y no está vacío.

### 7.4 Nombres y retención

Propuesta inicial, pendiente de aprobación:

- Nombre con timestamp UTC: `subtrack-20261020-183000Z.db`.
- Conservar, por ejemplo, 30 respaldos diarios y 12 mensuales.
- Respaldo obligatorio antes de cada migración.
- No borrar el último respaldo válido aunque falle la retención.

### 7.5 Restauración

1. Requerir que la aplicación no tenga conexiones abiertas; idealmente, restaurar desde un flujo de arranque o reiniciar la aplicación.
2. Abrir el candidato en modo solo lectura.
3. Validar `integrity_check`, `foreign_key_check`, `application_id` y `user_version`.
4. Rechazar si la versión es más nueva que la soportada.
5. Crear un respaldo de seguridad de la base actual antes de reemplazar.
6. Cerrar todas las conexiones; ejecutar `PRAGMA wal_checkpoint(TRUNCATE)` si es posible.
7. Eliminar `-wal` y `-shm` antiguos después de cerrar; de lo contrario, SQLite podría interpretar frames viejos.
8. Reemplazar el archivo de forma atómica en el mismo volumen.
9. Reabrir, verificar y, si la versión es anterior, aplicar migraciones con respaldo previo.
10. Si algo falla, restaurar el respaldo de seguridad.

### 7.6 Archivos temporales y errores

- Escribir respaldos en `.tmp` dentro del mismo directorio y renombrar al finalizar.
- Borrar temporales huérfanos al iniciar.
- Manejar disco lleno, permisos, archivo bloqueado por antivirus y rutas inexistentes con mensajes claros.
- No dejar un respaldo a medio escribir con nombre definitivo.
- Un respaldo fallido no debe impedir el arranque normal, salvo que sea un respaldo previo a migración: en ese caso, no migrar sin respaldo.

### 7.7 Exclusión de Git

Estado actual de `.gitignore`:

```text
bin/
obj/
.vs/
*.user
*.suo
*.db
*.db-shm
*.db-wal
*.sqlite
*.sqlite3
backups/
TestResults/
```

Cobertura correcta para lo principal. **Propuesta pendiente** de agregar, si el equipo lo aprueba:

```text
*.db-journal
*.sqlite-journal
*.db.bak
*.backup
*.tmp
```

No se modificó el archivo en esta fase. Además, la base se ubicará fuera del repositorio, por lo que la exclusión es una defensa adicional.

---

## 8. Dependencias pendientes de ZYKO

> No se propone modificar Core. Solo se enumera lo que Infrastructure necesita recibir. Las firmas son ilustrativas y no vinculantes.

### 8.1 Entidades y tipos

| Entidad | Datos mínimos que Infrastructure necesita |
|---|---|
| `Suscripcion` | `Id`, `Nombre`, `MontoCentavos`, `Frecuencia`, `FechaInicio`, `ProximoCobro`, `Activa`, timestamps. |
| `CobroProgramado` | `Id`, `SuscripcionId`, `FechaProgramada`, `MontoCentavos`, `Estado`, timestamps. |
| `Pago` | `Id`, `CobroId`, `FechaPago`, `MontoCentavos`, `Estado`, `NumeroComprobante`, `AnuladoEn`, `MotivoAnulacion`, timestamps. |
| `Gasto` | `Id`, `Concepto`, `Categoria`, `MontoCentavos`, `Fecha`, `Estado`, `AnuladoEn`, `MotivoAnulacion`, timestamps. |
| `Ingreso` | `Id`, `Concepto`, `MontoCentavos`, `Fecha`, `Tipo`, `Estado`, `AnuladoEn`, `MotivoAnulacion`, timestamps. |

### 8.2 Enums y estados

| Enum | Valores propuestos | Estado |
|---|---|---|
| `FrecuenciaCobro` | Diaria, Semanal, Mensual, Trimestral, Semestral, Anual | Contenido aprobado; nombres C# pendientes. |
| `EstadoCobro` | Pendiente, Pagado | Pendiente confirmar si existe `Vencido` persistido o derivado. |
| `EstadoPago` | Vigente, Anulado | Contenido aprobado; nombres C# pendientes. |
| `EstadoGasto` / `EstadoIngreso` | Vigente, Anulado | Pendiente confirmar nombres. |
| `TipoIngreso` | Mensual, Extraordinario | Contenido aprobado; nombres C# pendientes. |
| `CategoriaGasto` | Pendiente: texto libre, enum o catálogo | **Bloqueante menor.** |

Recomendación: `Vencido` no se persiste; se deriva de `estado='PENDIENTE' AND fecha_programada < hoy`. Así no queda un estado obsoleto si la fecha cambia o la app no se abre.

### 8.3 Interfaces de repositorio

ZYKO debe definir, como mínimo:

- `ISuscripcionRepository`
- `ICobroProgramadoRepository`
- `IPagoRepository`
- `IGastoRepository`
- `IIngresoRepository`
- `IConsultasDashboard` o `IConsultasFinancieras`
- Contrato de operación atómica de pago/anulación

Ejemplo ilustrativo, **no vinculante**:

```csharp
public interface IPagoRepository
{
    Task<ResultadoRegistroPago> RegistrarPagoAsync(
        SolicitudRegistroPago solicitud,
        CancellationToken cancellationToken = default);

    Task AnularPagoAsync(
        SolicitudAnulacionPago solicitud,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Pago>> ObtenerHistorialPorCobroAsync(
        Guid cobroId,
        CancellationToken cancellationToken = default);
}
```

### 8.4 Contrato de registro y anulación

Debe definir:

- ¿El usuario envía solo `CobroId`, `FechaPago` y `MontoCentavos`?
- ¿Core calcula la siguiente fecha y se la pasa a Infrastructure?
- ¿Infrastructure crea el siguiente cobro o solo actualiza `proximo_cobro`?
- ¿El pago debe ser por el importe total del cobro? ¿Se permiten pagos parciales? **No está aprobado.**
- ¿Quién genera `NumeroComprobante`? ¿Formato secuencial, por año, UUID?
- ¿`AnuladoEn` es timestamp UTC o `DateOnly`?
- ¿Quién valida el motivo? ¿Longitud máxima?
- ¿Una anulación puede repetirse? ¿Es idempotente?
- ¿Qué mensaje/código se devuelve si el cobro ya está pagado?

### 8.5 Consultas del Dashboard

ZYKO debe definir el contrato de las nueve consultas:

1. Suscripciones activas.
2. Próximos cobros en los siguientes 7 días.
3. Pagos de suscripciones del mes.
4. Gastos independientes del mes.
5. Egresos totales del mes.
6. Cobros vencidos y pendientes.
7. Ingresos registrados del mes.
8. Saldo mensual actual estimado.
9. Ahorro proyectado al finalizar el mes.

Preguntas bloqueantes:

- ¿"Pagos del mes" se filtra por `fecha_pago` o por `fecha_programada`? **Recomendación:** `fecha_pago` para flujo de caja; `fecha_programada` para obligaciones.
- ¿"Próximos 7 días" es inclusivo en ambos extremos?
- ¿"Mes" se calcula con la fecha local del equipo? ¿Qué pasa si el usuario cambia de zona horaria?
- ¿Infrastructure devuelve sumas crudas y Core aplica las fórmulas, o Infrastructure ejecuta las fórmulas?
- ¿"Ingresos previstos" son filas futuras confirmadas o una plantilla recurrente? **No está definido.**
- ¿"Obligaciones pendientes" incluye cobros aún no generados de suscripciones semanales/diarias?

**Recomendación COSMO:** Infrastructure devuelve conteos, filas y sumas mecánicas (`SUM`, `COUNT`, filtros por estado y rango). Core aplica las fórmulas financieras y la semántica de "egreso", "ahorro" e "ingreso previsto". Esto respeta la regla de que Infrastructure no implementa reglas financieras.

### 8.6 Errores y reloj

- Modelo de errores: excepciones de dominio, `Result<T>` o códigos. Debe ser consistente para GRIM.
- `IReloj` o `TimeProvider` en Core para "hoy", "ahora UTC" y meses calendario. Evitar `date('now')` de SQLite en consultas de negocio: usa UTC y puede no coincidir con la fecha local.
- Contrato de mensajes: "Este cobro ya fue registrado como pagado" ya está aprobado; los demás deben definirse.

### 8.7 Pruebas

- ¿COSMO escribe pruebas de persistencia en `SubTrack.Tests` o se crea un proyecto aparte?
- ¿Se permite modificar el proyecto de Tests compartido?
- ¿ZYKO aporta builders/fixtures de entidades?

**Pendiente de acuerdo.** En esta fase no se tocó Tests.

---

## 9. Riesgos de integración con GRIM

| Riesgo | Impacto | Mitigación propuesta |
|---|---|---|
| GRIM ejecuta SQL o abre conexiones directamente | Rompe la arquitectura y duplica reglas | Composición solo en `Program.cs`; conexión interna a Infrastructure; revisión de código. |
| API asincrónica mal usada en WinForms (`.Result`, `.Wait()`) | Bloqueos o deadlocks de UI | Definir si las APIs serán async; si lo son, GRIM debe usar `async/await` hasta el evento. |
| Operaciones de base en el hilo de UI | Congelamiento en migraciones, respaldos o consultas | Arranque con splash; operaciones largas en background; reportar progreso. |
| Estado visual "Pendiente" obsoleto | Usuario intenta pagar dos veces | Infrastructure rechaza el duplicado; GRIM refresca y muestra mensaje claro. |
| Errores SQLite crudos en pantalla | Mala experiencia y filtración técnica | Mapear a errores de dominio con mensajes en español. |
| Formatos de fecha/moneda inconsistentes | Errores de un día o de centavos | `DateOnly` en toda la cadena; centavos `long`; formateo solo en presentación. |
| Dashboard con lecturas mezcladas | Totales incoherentes si se registra un pago mientras se consulta | Una transacción de lectura o una consulta única para el resumen. |
| Restauración con formularios abiertos | Archivos bloqueados o datos inconsistentes | Restaurar solo al arrancar o reiniciar la aplicación; coordinar con GRIM. |
| Dos instancias de la aplicación | Contención de escritura, migraciones o restauraciones simultáneas | Mutex de instancia única o política explícita de multinstancia con `busy_timeout`. |
| Base en carpeta sincronizada (OneDrive/Desktop) | Corrupción por sincronización de WAL | Usar `%LOCALAPPDATA%`; no permitir rutas relativas. |
| Falta de librería nativa SQLite en publicación | La app falla al iniciar en otra máquina | Incluir el bundle nativo de `Microsoft.Data.Sqlite`; probar publicación `win-x64`. |
| Pruebas apuntando a la base real | Pérdida de datos o pruebas no reproducibles | Inyectar ruta temporal; prohibir `LocalAppData` en tests. |
| Cambios de contrato durante la implementación | Retrabajo y migraciones innecesarias | Cerrar F0 antes de codificar; congelar entidades y firmas. |
| Migraciones automáticas sin respaldo | Pérdida de datos ante un fallo | Respaldo obligatorio antes de migrar; checksum; transacción. |

### 9.1 Inicialización desde `Program.cs`

Propuesta de flujo, sin SQL en WinForms:

```text
Program.Main
  ApplicationConfiguration.Initialize()
  try
      resolver ruta de base
      crear fábrica de conexiones
      ejecutar migraciones pendientes
      verificar integridad
      componer repositorios/servicios
      abrir formulario principal con los servicios
  catch (ErrorDeInicializacion)
      mostrar MessageBox con diagnóstico y salir
```

No se propone un contenedor de dependencias obligatorio. Para una aplicación pequeña, la composición manual es suficiente y evita una dependencia adicional. Si el equipo prefiere DI, se decide en F0.

---

## 10. Plan de implementación por fases pequeñas

> Estimación preliminar, no compromiso. Cada fase requiere revisión humana antes de pasar a la siguiente. Ninguna operación Git será ejecutada por el agente.

| Fase | Alcance | Criterio de salida |
|---|---|---|
| **F0. Congelar contratos** | Entidades, enums, estados, firmas, DTOs, errores, semántica de Dashboard y de cobros. | Documento aprobado por ZYKO, COSMO y GRIM. Sin código. |
| **F1. Bootstrap de persistencia** | Paquete SQLite aprobado, resolución de ruta, fábrica de conexiones, PRAGMAs, health check. | Se verifica `foreign_keys=ON`, WAL activo, ruta correcta y conexión de solo prueba. |
| **F2. Migraciones** | Runner versionado, `0001`, `esquema_migraciones`, `application_id`, `user_version`, inicialización atómica. | Base vacía → versión 1; reejecución no-op; checksum inválido aborta; versión futura aborta. |
| **F3. Suscripciones y cobros** | Repositorios, mapeo `Guid`/`long`/`DateOnly`, restricciones y consultas básicas. | CRUD, unicidad de cobro por fecha, FK y roundtrip de fechas/importes. |
| **F4. Pagos** | Registro atómico, comprobante único, índice parcial, actualización de cobro y siguiente cobro. | Un pago vigente por cobro; prueba de concurrencia con exactamente un éxito; rollback correcto. |
| **F5. Anulaciones** | Anulación de pagos, retorno del cobro a pendiente, nuevo pago posterior; anulación de gastos e ingresos. | Historial conservado; nuevo pago permitido; totales excluyen anulados. |
| **F6. Gastos e ingresos** | Repositorios, edición/anulación, filtros por fecha/categoría/tipo. | CRUD y sumas mensuales correctas; anulados fuera de totales. |
| **F7. Dashboard** | Consultas y DTOs para los nueve indicadores; lectura consistente. | Datos sembrados producen los nueve valores esperados; límites de mes correctos. |
| **F8. Respaldos y restauración** | Backup en línea, validación, restauración, retención, respaldo previo a migración. | Respaldo íntegro con base en uso; restauración valida y rechaza archivos corruptos/ajenos/versión futura. |
| **F9. Endurecimiento e integración** | Mapeo de errores, reintentos `BUSY`, logging, chequeos de integridad, apoyo a GRIM, documentación. | Arranque fail-fast, mensajes claros, pruebas de integridad y smoke test con WinForms. |

**Duración indicativa:** 8–10 días hábiles si F0 se cierra rápidamente. La fecha límite del 2 de noviembre es alcanzable solo si los contratos técnicos se aprueban en los primeros días.

---

## 11. Separación explícita: aprobado vs. propuesto

### 11.1 [APROBADO] — no requiere nueva decisión

| Tema | Fuente |
|---|---|
| Guid como Id y `TEXT` en SQLite | `CONTRATOS-INTEGRACION.md` — Identificadores |
| Seis frecuencias de cobro | `CONTRATOS-INTEGRACION.md` — Frecuencias |
| Cálculo del próximo cobro desde la última fecha programada | `CONTRATOS-INTEGRACION.md` — Cálculo del próximo cobro |
| Registro manual de pagos y comprobante interno | `CONTRATOS-INTEGRACION.md` — Registro de pagos |
| Fecha programada vs. fecha real; pagos anticipados/atrasados | `CONTRATOS-INTEGRACION.md` — Pagos atrasados y anticipados |
| Un pago por cobro y restricción `UNIQUE` | `CONTRATOS-INTEGRACION.md` — Prevención de duplicados |
| Anulación con historial, motivo y retorno a Pendiente | `CONTRATOS-INTEGRACION.md` — Anulación de pagos |
| Gastos con categoría, edición y anulación | `CONTRATOS-INTEGRACION.md` — Gestión de gastos |
| Ingresos mensuales y extraordinarios | `CONTRATOS-INTEGRACION.md` — Gestión de ingresos |
| Dashboard de nueve indicadores y fórmulas | `CONTRATOS-INTEGRACION.md` — Dashboard y reglas |
| Core sin SQLite/WinForms; WinForms sin SQL; Infrastructure depende de Core | `docs/ARQUITECTURA.md` |
| Importes en centavos; `DateOnly`; FK activas | `docs/ARQUITECTURA.md` |

### 11.2 [PROPUESTA COSMO] — pendiente de aprobación

| # | Propuesta | Recomendación |
|---|---|---|
| P1 | Nombres de tablas y columnas en `snake_case` español | Aprobar o cambiar antes de F2. |
| P2 | Enums como `TEXT` con códigos estables | Aprobar; evita reordenamientos. |
| P3 | `Vencido` derivado, no persistido | Aprobar; evita estados obsoletos. |
| P4 | Índice único parcial `ux_pagos_cobro_vigente` | **Aprobar; es la pieza clave de la regla de duplicados.** |
| P5 | Anulación por estado + fecha + motivo, sin borrar | Aprobar; cumple el contrato. |
| P6 | Un cobro siguiente generado al pagar, con opción de sincronización por rango | **Decisión bloqueante** por el Dashboard de frecuencias cortas. |
| P7 | `numero_comprobante` generado por Infrastructure con secuencia transaccional | Aprobar formato y propiedad. |
| P8 | Pago total obligatorio (importe igual al cobro) | Aprobar; los pagos parciales no están contemplados. |
| P9 | Base en `%LOCALAPPDATA%\SubTrack` | Aprobar; no usar el repositorio ni Program Files. |
| P10 | WAL + `synchronous=FULL` + `foreign_keys=ON` + `busy_timeout` | Aprobar configuración inicial. |
| P11 | Migraciones SQL embebidas con checksum y `user_version` | Aprobar; alternativa EF Core solo si el equipo lo prefiere. |
| P12 | Respaldo con API en línea, retención 30 diarios/12 mensuales | Aprobar política; definir cifrado. |
| P13 | Restauración solo con la aplicación cerrada o al arrancar | Coordinar con GRIM. |
| P14 | Instancia única de la aplicación | Recomendado; evita migraciones/restauraciones concurrentes. |
| P15 | Sin auditoría de ediciones; solo historial de anulación | Aprobar; si se requiere auditoría completa, agregar tabla de eventos. |
| P16 | Infrastructure devuelve agregados; Core aplica fórmulas | Aprobar; respeta la separación de responsabilidades. |
| P17 | Clave de idempotencia para gastos/ingresos | Evaluar con GRIM; evita doble clic. |
| P18 | Ampliar `.gitignore` con journals, `.bak`, `.backup`, `.tmp` | Aprobar; no se modificó en esta fase. |
| P19 | `STRICT` tables | Evaluar; mejora validación de tipos, pero requiere versión compatible. |
| P20 | Moneda única | Confirmar que no habrá multimoneda en 1.0. |

### 11.3 [BLOQUEANTE] — decisiones que deben tomarse antes de F1

1. ¿`Pago` tiene `CobroId` en Core? La sección inicial dice `SuscripcionId`; las decisiones posteriores exigen el cobro.
2. ¿Quién genera el siguiente cobro y con qué horizonte?
3. ¿Cómo se manejan cobros atrasados de varias semanas/meses?
4. ¿El pago es siempre por el total del cobro?
5. ¿Quién genera el número de comprobante y con qué formato?
6. ¿"Pagos del mes" se mide por fecha de pago o por fecha programada?
7. ¿Infrastructure calcula fórmulas del Dashboard o solo devuelve datos?
8. ¿Los ingresos mensuales son transacciones o plantillas recurrentes?
9. ¿Qué significa "ingreso futuro confirmado" y "gasto futuro registrado"?
10. ¿APIs sincrónicas o asincrónicas? ¿Excepciones o `Result`?
11. ¿Instancia única o múltiples instancias?
12. ¿Categoría de gasto es texto libre, enum o catálogo?
13. ¿Se permite editar un gasto/ingreso sin dejar historial del valor anterior?
14. ¿Quién escribe las pruebas de persistencia y en qué proyecto?

---

## 12. Conclusión

La infraestructura actual es un esqueleto sin persistencia. La propuesta técnica es viable con SQLite, pero **no puede implementarse todavía** porque faltan contratos técnicos de Core y decisiones funcionales sobre cobros, Dashboard y comprobantes.

El diseño propuesto cubre los puntos críticos:

- Integridad referencial y restricciones de importes/fechas/estados.
- Prevención de pagos duplicados incluso ante concurrencia, mediante índice único parcial y transacción `BEGIN IMMEDIATE`.
- Anulación sin pérdida de historial y posibilidad de un nuevo pago válido.
- Migraciones versionadas con checksum y respaldo previo.
- Respaldos consistentes con la base en uso y restauración validada.
- Separación entre reglas financieras de Core y persistencia de Infrastructure.

**No se ha implementado ni probado ninguna base de datos. No se afirma que el esquema funcione todavía.** Es una propuesta sujeta a revisión.

**Quedo a la espera de la revisión y autorización humana para continuar.**
