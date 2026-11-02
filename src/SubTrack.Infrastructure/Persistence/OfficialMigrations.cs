namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Catálogo oficial de migraciones de SubTrack.Infrastructure (INF-02B).
/// La versión 0001 crea el esquema de dominio derivado de las 75 decisiones aprobadas
/// (D1: F0-01..F0-30 y F0-T01..F0-T45).
/// </summary>
/// <remarks>
/// Cada restricción del esquema referencia la decisión que la origina para permitir
/// trazabilidad durante la revisión humana. El contrato C# de Core (entidades e
/// interfaces) sigue pendiente; este esquema solo fija las garantías de SQLite.
/// </remarks>
internal static class OfficialMigrations
{
    /// <summary>Versión del esquema de dominio.</summary>
    public const int InitialSchemaVersion = 1;

    /// <summary>Nombre estable de la migración inicial (aparece en esquema_migraciones).</summary>
    public const string InitialSchemaName = "esquema_inicial_dominio";

    /// <summary>
    /// DDL de la migración 0001.
    /// </summary>
    /// <remarks>
    /// Convenciones aplicadas (F0-T30, F0-T33, P1/P2 de la auditoría):
    /// identificadores Guid en TEXT minúsculas de 36 caracteres, importes en centavos
    /// (INTEGER), fechas de negocio en TEXT ISO yyyy-MM-dd, fechas técnicas en TEXT ISO UTC,
    /// enumeraciones como TEXT con CHECK y booleanos como INTEGER 0/1.
    /// </remarks>
    private const string InitialSchemaSql = """
        -- =============================================================================
        -- SubTrack 1.0 — Migración 0001: esquema de dominio
        -- =============================================================================

        -- -----------------------------------------------------------------------------
        -- F0-13 / F0-T11 — Categorías de gasto predefinidas y personalizadas.
        -- Una categoría con gastos asociados puede desactivarse pero no eliminarse
        -- (la FK de gastos usa ON DELETE RESTRICT).
        -- -----------------------------------------------------------------------------
        CREATE TABLE categorias_gasto (
            id              TEXT    NOT NULL PRIMARY KEY
                                    CHECK (length(id) = 36 AND id = lower(id)),
            nombre          TEXT    NOT NULL
                                    CHECK (length(trim(nombre)) BETWEEN 1 AND 100),
            activa          INTEGER NOT NULL CHECK (activa IN (0, 1)),
            creado_en       TEXT    NOT NULL,
            actualizado_en  TEXT    NOT NULL,
            CONSTRAINT ux_categorias_gasto_nombre UNIQUE (nombre)
        );

        -- F0-13: categorías iniciales aprobadas.
        INSERT INTO categorias_gasto (id, nombre, activa, creado_en, actualizado_en)
        VALUES
            ('11111111-1111-4111-8111-111111111111', 'Alimentación',
             1, strftime('%Y-%m-%dT%H:%M:%fZ', 'now'), strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
            ('22222222-2222-4222-8222-222222222222', 'Transporte',
             1, strftime('%Y-%m-%dT%H:%M:%fZ', 'now'), strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
            ('33333333-3333-4333-8333-333333333333', 'Salud',
             1, strftime('%Y-%m-%dT%H:%M:%fZ', 'now'), strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
            ('44444444-4444-4444-8444-444444444444', 'Educación',
             1, strftime('%Y-%m-%dT%H:%M:%fZ', 'now'), strftime('%Y-%m-%dT%H:%M:%fZ', 'now'));

        -- -----------------------------------------------------------------------------
        -- Suscripciones.
        -- F0-22: importe fijo o variable (en variable, monto_centavos es el estimado).
        -- Frecuencias aprobadas: DIARIA, SEMANAL, MENSUAL, TRIMESTRAL, SEMESTRAL, ANUAL.
        -- F0-10: activa=0 conserva cobros y pagos anteriores.
        -- -----------------------------------------------------------------------------
        CREATE TABLE suscripciones (
            id              TEXT    NOT NULL PRIMARY KEY
                                    CHECK (length(id) = 36 AND id = lower(id)),
            nombre          TEXT    NOT NULL
                                    CHECK (length(trim(nombre)) BETWEEN 1 AND 200),
            operacion_id    TEXT    NOT NULL
                                    CHECK (length(trim(operacion_id)) BETWEEN 1 AND 100),
            monto_centavos  INTEGER NOT NULL CHECK (monto_centavos > 0),
            tipo_importe    TEXT    NOT NULL
                                    CHECK (tipo_importe IN ('FIJO', 'VARIABLE')),
            frecuencia      TEXT    NOT NULL
                                    CHECK (frecuencia IN
                                        ('DIARIA', 'SEMANAL', 'MENSUAL',
                                         'TRIMESTRAL', 'SEMESTRAL', 'ANUAL')),
            fecha_inicio    TEXT    NOT NULL
                                    CHECK (fecha_inicio GLOB
                                        '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'
                                        AND date(fecha_inicio) = fecha_inicio),
            proximo_cobro   TEXT    NULL
                                    CHECK (proximo_cobro IS NULL
                                        OR (proximo_cobro GLOB
                                            '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'
                                            AND date(proximo_cobro) = proximo_cobro)),
            activa          INTEGER NOT NULL CHECK (activa IN (0, 1)),
            desactivada_en  TEXT    NULL
                                    CHECK (desactivada_en IS NULL
                                        OR (desactivada_en GLOB
                                            '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'
                                            AND date(desactivada_en) = desactivada_en)),
            creado_en       TEXT    NOT NULL,
            actualizado_en  TEXT    NOT NULL,
            -- F0-10: la fecha efectiva de cancelación es obligatoria al desactivar y
            -- determina desde cuándo no se generan cobros nuevos.
            CONSTRAINT ck_suscripcion_desactivacion CHECK (
                (activa = 1 AND desactivada_en IS NULL)
                OR (activa = 0 AND desactivada_en IS NOT NULL)
            ),
            -- F0-27: un doble clic o reintento no crea dos suscripciones.
            CONSTRAINT ux_suscripcion_operacion UNIQUE (operacion_id),
            -- Una suscripción activa siempre debe tener próximo cobro; una
            -- desactivada puede no tener ninguno pendiente.
            CONSTRAINT ck_suscripcion_proximo_cobro CHECK (
                activa = 0 OR proximo_cobro IS NOT NULL
            )
        );

        -- -----------------------------------------------------------------------------
        -- F0-T06 / F0-09 / F0-23 — Historial de versiones de suscripciones.
        -- Cada cambio de importe, frecuencia o fecha de cobro crea una versión con su
        -- fecha de entrada en vigor; los cobros históricos no se alteran.
        -- dia_ancla (F0-T31): día original de vencimiento. Un mes sin ese día usa el
        -- último disponible sin modificar permanentemente la programación.
        -- -----------------------------------------------------------------------------
        CREATE TABLE suscripcion_versiones (
            id                  TEXT    NOT NULL PRIMARY KEY
                                        CHECK (length(id) = 36 AND id = lower(id)),
            suscripcion_id      TEXT    NOT NULL,
            numero_version      INTEGER NOT NULL CHECK (numero_version >= 1),
            vigente_desde       TEXT    NOT NULL
                                        CHECK (vigente_desde GLOB
                                            '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'
                                            AND date(vigente_desde) = vigente_desde),
            monto_centavos      INTEGER NOT NULL CHECK (monto_centavos > 0),
            tipo_importe        TEXT    NOT NULL
                                        CHECK (tipo_importe IN ('FIJO', 'VARIABLE')),
            frecuencia          TEXT    NOT NULL
                                        CHECK (frecuencia IN
                                            ('DIARIA', 'SEMANAL', 'MENSUAL',
                                             'TRIMESTRAL', 'SEMESTRAL', 'ANUAL')),
            dia_ancla           INTEGER NOT NULL CHECK (dia_ancla BETWEEN 1 AND 31),
            creado_en           TEXT    NOT NULL,
            CONSTRAINT fk_version_suscripcion
                FOREIGN KEY (suscripcion_id) REFERENCES suscripciones(id)
                ON DELETE RESTRICT ON UPDATE RESTRICT,
            CONSTRAINT ux_version_numero UNIQUE (suscripcion_id, numero_version),
            CONSTRAINT ux_version_vigencia UNIQUE (suscripcion_id, vigente_desde)
        );

        -- -----------------------------------------------------------------------------
        -- Cobros programados.
        -- F0-01 / F0-28: generación explícita por rango, sin duplicados (UNIQUE).
        -- F0-05: los cobros atrasados se conservan por separado con su fecha original.
        -- F0-T01: estado CANCELADO además de PENDIENTE y PAGADO.
        -- -----------------------------------------------------------------------------
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
                                      CHECK (estado IN ('PENDIENTE', 'PAGADO', 'CANCELADO')),
            creado_en         TEXT    NOT NULL,
            actualizado_en    TEXT    NOT NULL,
            CONSTRAINT fk_cobro_suscripcion
                FOREIGN KEY (suscripcion_id) REFERENCES suscripciones(id)
                ON DELETE RESTRICT ON UPDATE RESTRICT,
            CONSTRAINT ux_cobro_suscripcion_fecha
                UNIQUE (suscripcion_id, fecha_programada)
        );

        -- -----------------------------------------------------------------------------
        -- Pagos de suscripción.
        -- F0-T04 / F0-27: operacion_id hace idempotente cada intento de registro.
        -- F0-03: comprobante interno con numeración consecutiva por año (CMP-AAAA-NNNNNN).
        -- F0-02: los pagos anulados se conservan; solo uno VIGENTE por cobro.
        -- F0-08: fecha_pago es la fecha real y determina el mes contable.
        -- Nota: pagos no lleva suscripcion_id; se deriva por cobro_id (sin redundancia).
        -- -----------------------------------------------------------------------------
        CREATE TABLE pagos (
            id                  TEXT    NOT NULL PRIMARY KEY
                                        CHECK (length(id) = 36 AND id = lower(id)),
            cobro_id            TEXT    NOT NULL,
            operacion_id        TEXT    NOT NULL
                                        CHECK (length(trim(operacion_id)) BETWEEN 1 AND 100),
            fecha_pago          TEXT    NOT NULL
                                        CHECK (fecha_pago GLOB
                                            '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'
                                            AND date(fecha_pago) = fecha_pago),
            monto_centavos      INTEGER NOT NULL CHECK (monto_centavos > 0),
            estado              TEXT    NOT NULL
                                        CHECK (estado IN ('VIGENTE', 'ANULADO')),
            numero_comprobante  TEXT    NOT NULL
                                        CHECK (numero_comprobante GLOB
                                            'CMP-[0-9][0-9][0-9][0-9]-[0-9][0-9][0-9][0-9][0-9][0-9]'),
            anulado_en          TEXT    NULL,
            motivo_anulacion    TEXT    NULL,
            creado_en           TEXT    NOT NULL,
            actualizado_en      TEXT    NOT NULL,
            CONSTRAINT fk_pago_cobro
                FOREIGN KEY (cobro_id) REFERENCES cobros_programados(id)
                ON DELETE RESTRICT ON UPDATE RESTRICT,
            CONSTRAINT ux_pago_comprobante UNIQUE (numero_comprobante),
            CONSTRAINT ux_pago_operacion UNIQUE (operacion_id),
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

        -- F0-02 / F0-18: como máximo un pago VIGENTE por cobro, con cualquier número
        -- de pagos ANULADOS. La protección aplica incluso ante intentos simultáneos.
        CREATE UNIQUE INDEX ux_pagos_cobro_vigente
            ON pagos (cobro_id)
            WHERE estado = 'VIGENTE';

        -- F0-04: el importe pagado debe coincidir exactamente con el cobro programado.
        -- Los pagos parciales y los superiores al importe quedan prohibidos.
        CREATE TRIGGER trg_pagos_importe_completo
        BEFORE INSERT ON pagos
        WHEN NEW.estado = 'VIGENTE'
        BEGIN
            SELECT RAISE(ABORT, 'El importe del pago debe coincidir exactamente con el importe del cobro programado.')
            WHERE (SELECT monto_centavos FROM cobros_programados WHERE id = NEW.cobro_id)
                  <> NEW.monto_centavos;
        END;

        CREATE TRIGGER trg_pagos_importe_completo_update
        BEFORE UPDATE OF monto_centavos, cobro_id ON pagos
        WHEN NEW.estado = 'VIGENTE'
        BEGIN
            SELECT RAISE(ABORT, 'El importe del pago debe coincidir exactamente con el importe del cobro programado.')
            WHERE (SELECT monto_centavos FROM cobros_programados WHERE id = NEW.cobro_id)
                  <> NEW.monto_centavos;
        END;

        -- -----------------------------------------------------------------------------
        -- Gastos independientes.
        -- F0-07 / F0-19 / F0-T08: PLANIFICADO -> CONFIRMADO sin duplicar importes;
        -- la fecha prevista y la fecha real se conservan por separado.
        -- F0-12: la fecha real la elige el usuario.
        -- F0-T02: anulación con historial y motivo.
        -- F0-27: operacion_id evita duplicados por doble clic o reintento.
        -- -----------------------------------------------------------------------------
        CREATE TABLE gastos (
            id               TEXT    NOT NULL PRIMARY KEY
                                     CHECK (length(id) = 36 AND id = lower(id)),
            concepto         TEXT    NOT NULL
                                     CHECK (length(trim(concepto)) BETWEEN 1 AND 200),
            categoria_id     TEXT    NOT NULL,
            operacion_id     TEXT    NOT NULL
                                     CHECK (length(trim(operacion_id)) BETWEEN 1 AND 100),
            monto_centavos   INTEGER NOT NULL CHECK (monto_centavos > 0),
            fecha_prevista   TEXT    NOT NULL
                                     CHECK (fecha_prevista GLOB
                                         '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'
                                         AND date(fecha_prevista) = fecha_prevista),
            fecha_real       TEXT    NULL
                                     CHECK (fecha_real IS NULL
                                         OR (fecha_real GLOB
                                             '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'
                                             AND date(fecha_real) = fecha_real)),
            estado           TEXT    NOT NULL
                                     CHECK (estado IN ('PLANIFICADO', 'CONFIRMADO', 'ANULADO')),
            anulado_en       TEXT    NULL,
            motivo_anulacion TEXT    NULL,
            creado_en        TEXT    NOT NULL,
            actualizado_en   TEXT    NOT NULL,
            CONSTRAINT fk_gasto_categoria
                FOREIGN KEY (categoria_id) REFERENCES categorias_gasto(id)
                ON DELETE RESTRICT ON UPDATE RESTRICT,
            CONSTRAINT ux_gasto_operacion UNIQUE (operacion_id),
            CONSTRAINT ck_gasto_estado CHECK (
                (estado = 'PLANIFICADO'
                 AND fecha_real IS NULL
                 AND anulado_en IS NULL
                 AND motivo_anulacion IS NULL)
                OR
                (estado = 'CONFIRMADO'
                 AND fecha_real IS NOT NULL
                 AND anulado_en IS NULL
                 AND motivo_anulacion IS NULL)
                OR
                (estado = 'ANULADO'
                 AND anulado_en IS NOT NULL
                 AND motivo_anulacion IS NOT NULL
                 AND length(trim(motivo_anulacion)) BETWEEN 1 AND 500)
            )
        );

        -- -----------------------------------------------------------------------------
        -- F0-14 / F0-T10 — Historial independiente de correcciones de gastos.
        -- Se conserva el valor anterior, el nuevo y la fecha del cambio.
        -- -----------------------------------------------------------------------------
        CREATE TABLE gastos_historial (
            id                     TEXT    NOT NULL PRIMARY KEY
                                           CHECK (length(id) = 36 AND id = lower(id)),
            gasto_id               TEXT    NOT NULL,
            concepto_anterior      TEXT    NULL,
            concepto_nuevo         TEXT    NULL,
            monto_anterior         INTEGER NULL CHECK (monto_anterior IS NULL OR monto_anterior > 0),
            monto_nuevo            INTEGER NULL CHECK (monto_nuevo IS NULL OR monto_nuevo > 0),
            categoria_anterior_id  TEXT    NULL,
            categoria_nueva_id     TEXT    NULL,
            fecha_anterior         TEXT    NULL,
            fecha_nueva            TEXT    NULL,
            modificado_en          TEXT    NOT NULL,
            CONSTRAINT fk_historial_gasto
                FOREIGN KEY (gasto_id) REFERENCES gastos(id)
                ON DELETE RESTRICT ON UPDATE RESTRICT,
            CONSTRAINT ck_historial_gasto_cambio CHECK (
                concepto_anterior IS NOT NULL
                OR concepto_nuevo IS NOT NULL
                OR monto_anterior IS NOT NULL
                OR monto_nuevo IS NOT NULL
                OR categoria_anterior_id IS NOT NULL
                OR categoria_nueva_id IS NOT NULL
                OR fecha_anterior IS NOT NULL
                OR fecha_nueva IS NOT NULL
            )
        );

        -- -----------------------------------------------------------------------------
        -- F0-06 / F0-T07 — Plantillas de ingresos recurrentes.
        -- Quincenal significa días 15 y último día del mes (aclaración de F0-10).
        -- dia_ancla = 31 representa "último día" conforme a F0-T31.
        -- -----------------------------------------------------------------------------
        CREATE TABLE ingresos_plantillas (
            id              TEXT    NOT NULL PRIMARY KEY
                                    CHECK (length(id) = 36 AND id = lower(id)),
            concepto        TEXT    NOT NULL
                                    CHECK (length(trim(concepto)) BETWEEN 1 AND 200),
            operacion_id    TEXT    NOT NULL
                                    CHECK (length(trim(operacion_id)) BETWEEN 1 AND 100),
            monto_centavos  INTEGER NOT NULL CHECK (monto_centavos > 0),
            frecuencia      TEXT    NOT NULL
                                    CHECK (frecuencia IN ('MENSUAL', 'QUINCENAL')),
            dia_ancla       INTEGER NOT NULL CHECK (dia_ancla BETWEEN 1 AND 31),
            fecha_inicio    TEXT    NOT NULL
                                    CHECK (fecha_inicio GLOB
                                        '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'
                                        AND date(fecha_inicio) = fecha_inicio),
            activa          INTEGER NOT NULL CHECK (activa IN (0, 1)),
            creado_en       TEXT    NOT NULL,
            actualizado_en  TEXT    NOT NULL,
            CONSTRAINT ck_plantilla_dia CHECK (
                frecuencia <> 'QUINCENAL' OR dia_ancla IN (15, 31)
            ),
            -- F0-27: evita plantillas duplicadas por doble envío.
            CONSTRAINT ux_plantilla_operacion UNIQUE (operacion_id)
        );

        -- -----------------------------------------------------------------------------
        -- Ingresos: mensuales, quincenales y extraordinarios.
        -- F0-06 / F0-19: PREVISTO -> CONFIRMADO cuando el dinero se recibe realmente.
        -- F0-T07: cada ingreso previsto se guarda por separado, vinculado a su plantilla.
        -- F0-27: operacion_id evita duplicados.
        -- -----------------------------------------------------------------------------
        CREATE TABLE ingresos (
            id               TEXT    NOT NULL PRIMARY KEY
                                     CHECK (length(id) = 36 AND id = lower(id)),
            concepto         TEXT    NOT NULL
                                     CHECK (length(trim(concepto)) BETWEEN 1 AND 200),
            plantilla_id     TEXT    NULL,
            operacion_id     TEXT    NOT NULL
                                     CHECK (length(trim(operacion_id)) BETWEEN 1 AND 100),
            monto_centavos   INTEGER NOT NULL CHECK (monto_centavos > 0),
            tipo             TEXT    NOT NULL
                                     CHECK (tipo IN ('MENSUAL', 'QUINCENAL', 'EXTRAORDINARIO')),
            fecha_prevista   TEXT    NOT NULL
                                     CHECK (fecha_prevista GLOB
                                         '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'
                                         AND date(fecha_prevista) = fecha_prevista),
            fecha_real       TEXT    NULL
                                     CHECK (fecha_real IS NULL
                                         OR (fecha_real GLOB
                                             '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'
                                             AND date(fecha_real) = fecha_real)),
            estado           TEXT    NOT NULL
                                     CHECK (estado IN ('PREVISTO', 'CONFIRMADO', 'ANULADO')),
            anulado_en       TEXT    NULL,
            motivo_anulacion TEXT    NULL,
            creado_en        TEXT    NOT NULL,
            actualizado_en   TEXT    NOT NULL,
            CONSTRAINT fk_ingreso_plantilla
                FOREIGN KEY (plantilla_id) REFERENCES ingresos_plantillas(id)
                ON DELETE RESTRICT ON UPDATE RESTRICT,
            CONSTRAINT ux_ingreso_operacion UNIQUE (operacion_id),
            CONSTRAINT ck_ingreso_estado CHECK (
                (estado = 'PREVISTO'
                 AND fecha_real IS NULL
                 AND anulado_en IS NULL
                 AND motivo_anulacion IS NULL)
                OR
                (estado = 'CONFIRMADO'
                 AND fecha_real IS NOT NULL
                 AND anulado_en IS NULL
                 AND motivo_anulacion IS NULL)
                OR
                (estado = 'ANULADO'
                 AND anulado_en IS NOT NULL
                 AND motivo_anulacion IS NOT NULL
                 AND length(trim(motivo_anulacion)) BETWEEN 1 AND 500)
            ),
            CONSTRAINT ck_ingreso_extraordinario CHECK (
                tipo <> 'EXTRAORDINARIO' OR plantilla_id IS NULL
            )
        );

        -- F0-14 / F0-T10 — Historial de correcciones de ingresos.
        CREATE TABLE ingresos_historial (
            id               TEXT    NOT NULL PRIMARY KEY
                                     CHECK (length(id) = 36 AND id = lower(id)),
            ingreso_id       TEXT    NOT NULL,
            concepto_anterior TEXT   NULL,
            concepto_nuevo   TEXT    NULL,
            monto_anterior   INTEGER NULL CHECK (monto_anterior IS NULL OR monto_anterior > 0),
            monto_nuevo      INTEGER NULL CHECK (monto_nuevo IS NULL OR monto_nuevo > 0),
            fecha_anterior   TEXT    NULL,
            fecha_nueva      TEXT    NULL,
            modificado_en    TEXT    NOT NULL,
            CONSTRAINT fk_historial_ingreso
                FOREIGN KEY (ingreso_id) REFERENCES ingresos(id)
                ON DELETE RESTRICT ON UPDATE RESTRICT,
            CONSTRAINT ck_historial_ingreso_cambio CHECK (
                concepto_anterior IS NOT NULL
                OR concepto_nuevo IS NOT NULL
                OR monto_anterior IS NOT NULL
                OR monto_nuevo IS NOT NULL
                OR fecha_anterior IS NOT NULL
                OR fecha_nueva IS NOT NULL
            )
        );

        -- -----------------------------------------------------------------------------
        -- F0-20 / F0-21 / F0-T09 — Saldo inicial único con historial de correcciones.
        -- No se registra como ingreso para no duplicarlo en los cálculos.
        -- El id fijo garantiza una sola fila vigente.
        -- -----------------------------------------------------------------------------
        CREATE TABLE saldo_inicial (
            id              TEXT    NOT NULL PRIMARY KEY
                                    CHECK (id = 'saldo_inicial'),
            monto_centavos  INTEGER NOT NULL CHECK (monto_centavos >= 0),
            fecha_inicio    TEXT    NOT NULL
                                    CHECK (fecha_inicio GLOB
                                        '[0-9][0-9][0-9][0-9]-[0-9][0-9]-[0-9][0-9]'
                                        AND date(fecha_inicio) = fecha_inicio),
            creado_en       TEXT    NOT NULL,
            actualizado_en  TEXT    NOT NULL
        );

        CREATE TABLE saldo_inicial_historial (
            id                     TEXT    NOT NULL PRIMARY KEY
                                           CHECK (length(id) = 36 AND id = lower(id)),
            saldo_inicial_id       TEXT    NOT NULL,
            monto_anterior         INTEGER NOT NULL CHECK (monto_anterior >= 0),
            monto_nuevo            INTEGER NOT NULL CHECK (monto_nuevo >= 0),
            fecha_inicio_anterior  TEXT    NOT NULL,
            fecha_inicio_nueva     TEXT    NOT NULL,
            modificado_en          TEXT    NOT NULL,
            CONSTRAINT fk_historial_saldo
                FOREIGN KEY (saldo_inicial_id) REFERENCES saldo_inicial(id)
                ON DELETE RESTRICT ON UPDATE RESTRICT
        );

        -- -----------------------------------------------------------------------------
        -- F0-03 — Secuencias transaccionales (numeración de comprobantes por año).
        -- Nombre esperado: 'comprobante_pago:AAAA'.
        -- -----------------------------------------------------------------------------
        CREATE TABLE secuencias (
            nombre  TEXT    NOT NULL PRIMARY KEY
                            CHECK (length(trim(nombre)) BETWEEN 1 AND 100),
            ultimo  INTEGER NOT NULL CHECK (ultimo >= 0)
        );

        -- -----------------------------------------------------------------------------
        -- Índices de consulta (F0-08, F0-17, F0-19, F0-26, F0-T29).
        -- -----------------------------------------------------------------------------
        CREATE INDEX ix_cobros_estado_fecha
            ON cobros_programados (estado, fecha_programada);

        CREATE INDEX ix_cobros_suscripcion
            ON cobros_programados (suscripcion_id);

        CREATE INDEX ix_pagos_fecha_estado
            ON pagos (fecha_pago, estado);

        CREATE INDEX ix_gastos_estado_fecha
            ON gastos (estado, fecha_real, fecha_prevista);

        CREATE INDEX ix_gastos_categoria
            ON gastos (categoria_id);

        CREATE INDEX ix_ingresos_estado_fecha
            ON ingresos (estado, fecha_real, fecha_prevista);

        CREATE INDEX ix_suscripciones_activa
            ON suscripciones (activa, proximo_cobro);

        CREATE INDEX ix_versiones_suscripcion
            ON suscripcion_versiones (suscripcion_id, vigente_desde);
        """;

    /// <summary>Migraciones oficiales ordenadas por versión.</summary>
    public static readonly IReadOnlyList<SqliteMigration> All =
    [
        new SqliteMigration(InitialSchemaVersion, InitialSchemaName, InitialSchemaSql)
    ];
}
