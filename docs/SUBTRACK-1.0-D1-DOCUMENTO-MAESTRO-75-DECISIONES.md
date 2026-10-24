# SUBTRACK 1.0 — D1: DOCUMENTO MAESTRO DE 75 DECISIONES

> **BORRADOR PARA REVISIÓN DE GRIM.** No es aprobación de implementación, contratos ni Git.

- **Estado:** BORRADOR PARA REVISIÓN DE GRIM — no aprobado
- **Fuente:** 75 desiciones(2).txt; 4007 líneas; evidencia por número de línea
- **Fecha:** 22 de octubre de 2026
- **Control:** D1 — consolidación documental; sin cambios de código, Git, contratos o migraciones

## 1. Alcance aprobado

El alcance de SubTrack 1.0 queda limitado a las decisiones F0-01 a F0-30 y F0-T01 a F0-T45, conforme a las aprobaciones explícitas de GRIM. Las propuestas no ratificadas y los ejemplos no crean requisitos adicionales.

## 2. Responsabilidades

GRIM: dirección, interfaz WinForms, revisión de contratos e integración humana. ZYKO: Core y pruebas de lógica de negocio. COSMO: Infrastructure/SQLite, migraciones y respaldos. La implementación específica de contratos compartidos requiere autorización posterior de GRIM.

## 3. Dependencias

WinForms consume servicios y resultados tipados de Core; Infrastructure implementa persistencia y mecanismos SQLite respetando los contratos aprobados; las pruebas integradas comprueban interacciones. No se considera aprobada una firma o DTO concreto solo por la aprobación funcional.

## 4. Contratos pendientes

ScheduledCharge, Payment, interfaces de repositorio, otros modelos compartidos y contratos transaccionales/de auditoría requieren revisión independiente. Durante D1 no se modifica ninguno.

## 5. Estado de integración declarado

GRIM: feature/ui. ZYKO: feature/business-logic, commits 6e748fe, 74240a4 y b711948 reportados; el último no está verificado aquí mediante Git. COSMO: feature/database, INF-01 integrada; INF-02A e INF-05 reportadas localmente, publicación no verificada. integration/subtrack-1.0: integración progresiva suspendida durante D1.

## 6. Restricciones

Ningún agente ejecutará comandos Git de lectura o escritura, implementará funcionalidades, modificará código o contratos, ejecutará migraciones o integrará ramas. Solo GRIM podrá incorporar y confirmar el documento cuando lo apruebe.

## 7. Método de trazabilidad

Cada registro identifica pregunta/problema original, evidencia de aprobación y formulación resultante con número de línea de la fuente. Las citas breves son extractos; para contexto íntegro prevalece la conversación original. Los campos que no puedan inferirse de una aprobación explícita se remiten a revisión humana.

## 8. Inventario y trazabilidad de las 75 decisiones

| ID | Título o tema | Planteamiento (línea) | Aprobación (línea) |
|---|---|---:|---:|
| F0-01 | Generación de cobros programados por rango | 50 | 90 |
| F0-02 | Anulación y nuevo pago | 96 | 116 |
| F0-03 | Número de comprobante interno | 122 | 154 |
| F0-04 | ¿Se permiten pagos parciales? | 162 | 184 |
| F0-05 | ¿Cómo manejar los cobros atrasados? | 201 | 258 |
| F0-06 | ¿Cómo manejar los ingresos mensuales? | 266 | 288 |
| F0-07 | ¿Cómo manejar los gastos futuros? | 298 | 322 |
| F0-08 | ¿Cómo calcular los pagos del mes? | 330 | 354 |
| F0-09 | ¿Qué ocurre al modificar una suscripción? | 362 | 386 |
| F0-10 | ¿Qué sucede al desactivar una suscripción? | 394 | 416 |
| F0-11 | Cálculo del ahorro proyectado | 453 | 493 |
| F0-12 | ¿Qué fecha utilizaremos para registrar los movimientos? | 505 | 529 |
| F0-13 | ¿Cómo manejar las categorías de gastos? | 539 | 567 |
| F0-14 | ¿Cómo manejar las correcciones de ingresos y gastos? | 575 | 600 |
| F0-15 | ¿Cómo manejar los errores del sistema? | 608 | 632 |
| F0-16 | ¿Cómo protegeremos los datos de SubTrack? | 646 | 670 |
| F0-17 | ¿Cómo mostrar las próximas obligaciones? | 681 | 711 |
| F0-18 | ¿Cómo evitar registrar movimientos duplicados? | 719 | 746 |
| F0-19 | ¿Qué hacer con los ingresos y gastos futuros cuando llega su fecha? | 758 | 784 |
| F0-20 | ¿Cómo manejar el saldo inicial? | 795 | 823 |
| F0-21 | ¿Cómo manejar los cambios del saldo inicial? | 833 | 859 |
| F0-22 | ¿Cómo manejar las suscripciones con importes variables? | 867 | 895 |
| F0-23 | ¿Qué hacer cuando una suscripción cambia su fecha de cobro? | 908 | 934 |
| F0-24 | ¿Qué ocurre si una suscripción se paga antes de su vencimiento? | 942 | 968 |
| F0-25 | ¿Cómo manejar los cobros de suscripciones que ya existían antes de instalar SubTrack? | 981 | 1008 |
| F0-26 | ¿Cómo manejar la restauración de un respaldo? | 1018 | 1062 |
| F0-27 | ¿Cómo evitar ingresos y gastos duplicados? | 1074 | 1100 |
| F0-28 | ¿Hasta qué fecha generar los cobros de suscripciones? | 1114 | 1140 |
| F0-29 | ¿Qué debe pasar si ocurre un fallo mientras se guarda una operación? | 1155 | 1183 |
| F0-30 | ¿Qué debe hacer SubTrack al detectar una base de datos dañada? | 1195 | 1321 |
| F0-T01 | Modelo ampliado de entidades y estados | 1570 | 1610 |
| F0-T02 | ¿Cómo deben relacionarse los registros financieros? | 1616 | 1667 |
| F0-T03 | ¿Cómo se comunicarán los tres módulos? | 1673 | 1726 |
| F0-T04 | Contratos de solicitudes y respuestas (DTO) | 1738 | 1868 |
| F0-T05 | ¿Qué pasa si falla el guardado de un pago? | 1880 | 1913 |
| F0-T06 | ¿Cómo controlaremos los cambios en las suscripciones? | 1919 | 1951 |
| F0-T07 | ¿Cómo controlaremos los ingresos recurrentes? | 1964 | 2004 |
| F0-T08 | ¿Cómo administraremos los gastos futuros? | 2012 | 2050 |
| F0-T09 | ¿Cómo controlaremos el saldo inicial y sus correcciones? | 2058 | 2098 |
| F0-T10 | ¿Cómo guardaremos las correcciones de ingresos y gastos? | 2106 | 2147 |
| F0-T11 | ¿Cómo gestionaremos las categorías de gastos? | 2155 | 2189 |
| F0-T12 | ¿Cómo calcularemos el Dashboard financiero? | 2195 | 2251 |
| F0-T13 | ¿Cómo actualizaremos el Dashboard? | 2263 | 2295 |
| F0-T14 | ¿Cómo evitaremos perder información si se cierra la aplicación? | 2303 | 2338 |
| F0-T15 | ¿Dónde guardaremos los datos de SubTrack? | 2346 | 2374 |
| F0-T16 | ¿Cómo protegeremos los respaldos de SubTrack? | 2382 | 2425 |
| F0-T17 | ¿Qué sucede cuando restauramos un respaldo? | 2433 | 2467 |
| F0-T18 | ¿Qué haremos si SQLite detecta datos dañados? | 2475 | 2513 |
| F0-T19 | ¿Cómo controlaremos las actualizaciones de la base de datos? | 2525 | 2567 |
| F0-T20 | ¿Qué pasa si intentamos abrir SubTrack dos veces? | 2575 | 2611 |
| F0-T21 | ¿Qué sucede si una operación tarda demasiado? | 2619 | 2651 |
| F0-T22 | ¿Cómo mostraremos los errores al usuario? | 2661 | 2695 |
| F0-T23 | ¿Cómo manejaremos los registros de errores? | 2703 | 2738 |
| F0-T24 | ¿Cómo evitaremos que se mezclen operaciones al cambiar de pantalla? | 2749 | 2787 |
| F0-T25 | ¿Cómo evitaremos mostrar saldos mezclados o desactualizados? | 2795 | 2834 |
| F0-T26 | ¿Cómo funcionarán las alertas de pagos próximos a vencer? | 2842 | 2887 |
| F0-T27 | ¿Qué ocurrirá si cierras SubTrack mientras guarda un pago? | 2895 | 2931 |
| F0-T28 | ¿Qué haremos si una consulta tarda demasiado? | 2942 | 2979 |
| F0-T29 | ¿Cómo mostraremos los historiales cuando tengan muchos registros? | 2987 | 3038 |
| F0-T30 | ¿Cómo evitaremos errores al manejar dinero? | 3046 | 3092 |
| F0-T31 | ¿Cómo controlaremos las fechas de pagos y vencimientos? | 3104 | 3150 |
| F0-T32 | ¿Qué pasará cuando cambie la fecha de Windows? | 3160 | 3201 |
| F0-T33 | ¿Qué sucederá si intentas guardar información incorrecta? | 3209 | 3249 |
| F0-T34 | ¿Cómo organizaremos las pruebas de SubTrack? | 3261 | 3299 |
| F0-T35 | ¿Cómo comprobaremos que SubTrack está listo para entregarse? | 3312 | 3356 |
| F0-T36 | ¿Cómo organizaremos los errores encontrados durante las pruebas? | 3364 | 3406 |
| F0-T37 | ¿Cómo sabremos si una corrección dañó algo que ya funcionaba? | 3414 | 3454 |
| F0-T38 | ¿Cómo sabremos quién debe corregir cada error? | 3462 | 3514 |
| F0-T39 | ¿Cómo organizaremos la integración de las tres partes? | 3522 | 3563 |
| F0-T40 | ¿Cómo controlaremos los cambios de los contratos entre integrantes? | 3571 | 3638 |
| F0-T41 | ¿Cómo instalaremos SubTrack 1.0 en Windows? | 3644 | 3681 |
| F0-T42 | ¿Cómo actualizaremos SubTrack sin perder información? | 3693 | 3738 |
| F0-T43 | ¿Cómo comprobaremos que SubTrack funciona en otra computadora? | 3748 | 3788 |
| F0-T44 | ¿Cómo organizaremos la entrega final del proyecto? | 3800 | 3842 |
| F0-T45 | ¿Cómo evitaremos agregar funciones nuevas antes de la entrega? | 3854 | 3894 |

## 9. Decisiones funcionales — F0-01 a F0-30

### F0-01 — Generación de cobros programados por rango

**Planteamiento y opciones (fuente desde línea 50):** Texto pegado.txt También hay dos detalles que conviene corregir: ambos informes describen la antigua interfaz `Form1`, aunque tu Fase 1 visual ya está publicada; y la propuesta de COSMO usa fecha/hora UTC para la anulación, mientras ZYKO propone inicialmente `DateOnly`. Debemos unificarlo. No enviaría todavía a los agentes a implementar. Primero cerremos la Fase 0 de contratos, pero sin convertirla en un proceso largo. Podemos tomar las decisiones una por una y preparar después dos autorizacione

**Evidencia de aprobación de GRIM (línea 86):** Para SubTrack 1.0, elijo la opción C para la generación de cobros programados. Continuemos con la siguiente decisión de la Fase 0.

**Regla/formulación registrada (línea 90):** F0-01 aprobada Generación de cobros por rango (opción C) Esto significa que SubTrack generará los cobros necesarios hasta una fecha determinada, sin duplicarlos. La sincronización será una operación explícita y controlada, no una escritura oculta al consultar el Dashboard.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-02 — Anulación y nuevo pago

**Planteamiento y opciones (fuente desde línea 96):** Imagina que registras el pago de Netflix por $12, pero te equivocas en la fecha. Lo anulas para corregirlo. ¿Qué debe hacer SubTrack cuando quieras registrar nuevamente ese pago? A. No permitir otro pago. El cobro queda bloqueado después de anular su pago. B. Permitir un nuevo pago y conservar el anterior anulado. El cobro vuelve a Pendiente y mantiene todo el historial. C. Reactivar el pago anulado. Se reutiliza el registro anterior en lugar de crear uno nuevo. Recomiendo B. Es la alternativa p

**Evidencia de aprobación de GRIM (línea 112):** Todavía no autorizamos modificaciones de código ni operaciones Git a los agentes. Primero terminaremos las decisiones de la Fase 0.Para SubTrack 1.0, apruebo la opción B para F0-02: anulación y nuevo pago. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 116):** F0-02 aprobada Opción B — Nuevo pago conservando el historial Cuando anules un pago, el registro anterior conservará su fecha y motivo de anulación, el cobro volverá a Pendiente y podrás registrar un pago nuevo. SQLite permitirá como máximo un pago vigente por cobro.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-03 — Número de comprobante interno

**Planteamiento y opciones (fuente desde línea 122):** Imagina que registras el pago de Netflix. SubTrack debe generar un comprobante para identificarlo en el historial. ¿Cómo quieres que se identifiquen los comprobantes? A. Numeración consecutiva por año (recomendada) `CMP-2026-000001`, `CMP-2026-000002` Fácil de leer y buscar. SQLite asigna el número dentro de la transacción, reiniciando la secuencia cada año. B. Numeración consecutiva global `CMP-000001`, `CMP-000002`

**Evidencia de aprobación de GRIM (línea 150):** Seguimos cerrando las decisiones antes de autorizar a ZYKO y COSMO a implementar código.Para SubTrack 1.0, apruebo la opción A para F0-03: numeración de comprobantes internos. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 154):** F0-03 aprobada Los comprobantes internos tendrán numeración consecutiva por año: `CMP-2026-000001` → `CMP-2026-000002` COSMO implementará la generación automática en SQLite, dentro de una transacción, sin duplicados. Cada año comenzará una nueva secuencia. Los comprobantes anulados conservarán su número original.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-04 — ¿Se permiten pagos parciales?

**Planteamiento y opciones (fuente desde línea 162):** Imagina que tienes una suscripción de Netflix de $15 mensuales, pero registras solamente $10 como pagados. ¿Cómo debería comportarse SubTrack? A. Solo pagos completos (recomendada) Si el cobro es de $15, debes registrar exactamente $15. No se permiten pagos parciales ni cantidades superiores. B. Permitir pagos parciales Puedes registrar $10 y dejar $5 pendientes. Requiere manejar saldos pendientes y múltiples pagos por cobro. C. Permitir cualquier importe

**Evidencia de aprobación de GRIM (línea 184):** Estado de la Fase 0: tres decisiones aprobadas; seguimos definiendo los contratos antes de autorizar a ZYKO y COSMO a programar. Para SubTrack 1.0, apruebo la opción A para F0-04: política de importes de pago. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 184):** Estado de la Fase 0: tres decisiones aprobadas; seguimos definiendo los contratos antes de autorizar a ZYKO y COSMO a programar. Para SubTrack 1.0, apruebo la opción A para F0-04: política de importes de pago. Continuemos con la siguiente decisión. ¡Excelente! Queda **aprobada F0-04** para SubTrack 1.0. ✅ SubTrack aplicará estas reglas: - El importe pagado debe coincidir exactamente con el cobro programado. - No se permiten pagos parciales ni superiores al importe. - Cada cobro admite un único pago vigente. - Si existe un error, se anula el pago y se registra uno nuevo, conservando el historial.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-05 — ¿Cómo manejar los cobros atrasados?

**Planteamiento y opciones (fuente desde línea 201):** Imagina que tienes una suscripción de **Netflix por $15 mensuales**. No registraste los pagos de enero, febrero y marzo. Ahora estamos en abril. ¿Cómo debería comportarse SubTrack? SubTrack mostrará: | Mes | Importe | Estado | |---|---:|---| | Enero | $15 | Vencido | | Febrero | $15 | Vencido |

**Evidencia de aprobación de GRIM (línea 256):** opcion a

**Regla/formulación registrada (línea 258):** F0-05 aprobada Queda aprobada la opción A: conservar todos los cobros pendientes por separado. SubTrack 1.0 mantendrá los cobros atrasados con su fecha e importe originales. Cada uno podrá pagarse individualmente y los vencidos aparecerán en el Dashboard. Ya tenemos 5 decisiones aprobadas.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-06 — ¿Cómo manejar los ingresos mensuales?

**Planteamiento y opciones (fuente desde línea 266):** Imagina que recibes un salario de $600 cada mes y quieres que SubTrack lo considere al calcular tus finanzas. A. Registrar cada ingreso manualmente Cada mes introduces los $600. No hay ingresos futuros automáticos. B. Configurar ingresos recurrentes y confirmar cada recepción (recomendada) Registras una vez tu salario de $600 mensuales. SubTrack lo muestra como ingreso previsto y, cuando realmente lo recibes, lo marcas como recibido. Evita contarlo dos veces. C. Registrar automáticamente el ingr

**Evidencia de aprobación de GRIM (línea 286):** Seguimos únicamente con las decisiones funcionales; ZYKO y COSMO todavía no tienen autorización para implementar.Para SubTrack 1.0, apruebo la opción B para F0-06: gestión de ingresos mensuales. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 288):** F0-06 aprobada Queda aprobada la opción B: ingresos mensuales recurrentes con confirmación manual. SubTrack 1.0 permitirá configurar un ingreso mensual, mostrarlo como previsto y marcarlo como recibido cuando el dinero realmente ingrese. Así, los ingresos previstos servirán para las proyecciones, pero no se contarán como dinero recibido hasta su confirmación, evitando duplicaciones. Llevamos 6 decisiones aprobadas.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-07 — ¿Cómo manejar los gastos futuros?

**Planteamiento y opciones (fuente desde línea 298):** Imagina que sabes que el 28 de octubre debes pagar $40 de electricidad, pero todavía no has realizado ese pago. ¿Cómo debería manejarlo SubTrack? A. Registrar únicamente gastos realizados Los $40 aparecerán en SubTrack solo cuando efectivamente los pagues. No podrás anticiparlos en la proyección. B. Registrar gastos previstos y confirmarlos al pagarlos (recomendada) Puedes registrar hoy los $40 con fecha prevista del 28 de octubre. Aparecerán como gasto futuro y afectarán el ahorro proyectado. C

**Evidencia de aprobación de GRIM (línea 320):** Todavía no autorizamos implementación ni operaciones Git a los agentes. Primero terminaremos de cerrar los contratos de la Fase 0.Para SubTrack 1.0, apruebo la opción B para F0-07: gestión de gastos futuros. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 322):** F0-07 aprobada Queda aprobada la opción B: registrar gastos futuros y confirmarlos cuando se paguen. SubTrack 1.0 distinguirá entre gastos previstos y realizados. Los previstos afectarán el ahorro proyectado, pero no el saldo actual. Al confirmarlos, se actualizará su estado sin duplicar importes. Ya llevamos 7 decisiones aprobadas.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-08 — ¿Cómo calcular los pagos del mes?

**Planteamiento y opciones (fuente desde línea 330):** Imagina que tienes una suscripción de $20 cuyo cobro estaba programado para el 30 de septiembre, pero realmente la pagaste el 2 de octubre. ¿En qué mes debería aparecer ese dinero como pagado en el Dashboard? A. En octubre, cuando realmente pagaste (recomendada) El Dashboard registra los $20 como egreso de octubre. El historial conserva que correspondían a septiembre. B. En septiembre, cuando estaba programado Los $20 se contabilizan en septiembre aunque el dinero haya salido en octubre. C. En a

**Evidencia de aprobación de GRIM (línea 352):** Seguimos cerrando las reglas financieras antes de entregar los contratos definitivos a ZYKO y COSMO. Para SubTrack 1.0, apruebo la opción A para F0-08: contabilización mensual de pagos. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 354):** F0-08 aprobada Queda aprobada la opción A: contabilizar los pagos en el mes en que realmente se realizaron. Por ejemplo, un cobro programado para septiembre y pagado el 2 de octubre se contabilizará como egreso de octubre. La obligación seguirá vinculada a septiembre en el historial. Llevamos 8 decisiones aprobadas.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-09 — ¿Qué ocurre al modificar una suscripción?

**Planteamiento y opciones (fuente desde línea 362):** Imagina que tienes Netflix por $15 mensuales y, a partir de noviembre, su precio aumenta a $18. ¿Qué debe hacer SubTrack con los cobros anteriores y los futuros? A. Conservar el historial y aplicar cambios hacia adelante (recomendada) Los cobros de $15 anteriores a noviembre permanecen intactos, incluso si están pendientes. Los cobros desde la fecha efectiva del cambio se calculan a $18. Los cobros futuros todavía no pagados se actualizan de forma controlada. B. Cambiar todos los cobros pendient

**Evidencia de aprobación de GRIM (línea 384):** Todavía no autorizamos a ZYKO ni a COSMO a modificar código. Estamos cerrando las reglas compartidas de la Fase 0. Para SubTrack 1.0, apruebo la opción A para F0-09: modificación de suscripciones. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 386):** F0-09 aprobada Queda aprobada la opción A: conservar el historial y aplicar los cambios de una suscripción hacia adelante. Por ejemplo, si Netflix aumenta de $15 a $18 en noviembre, los cobros anteriores conservarán sus importes y los cobros futuros no pagados se ajustarán desde la fecha efectiva del cambio. Llevamos 9 decisiones aprobadas.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-10 — ¿Qué sucede al desactivar una suscripción?

**Planteamiento y opciones (fuente desde línea 394):** Imagina que cancelas Netflix el 20 de octubre, pero tienes un cobro de septiembre que todavía no has pagado. ¿Qué debería hacer SubTrack? A. Conservar las deudas anteriores y detener los cobros futuros (recomendada) Los cobros pendientes anteriores al 20 de octubre permanecen registrados. No se generan cobros nuevos desde la fecha efectiva de cancelación. El historial de pagos permanece intacto. B. Eliminar todos los cobros pendientes La suscripción queda inactiva y desaparecen sus cobros sin pa

**Evidencia de aprobación de GRIM (línea 416):** Seguimos cerrando los contratos de la Fase 0. ZYKO y COSMO todavía no están autorizados para implementar código ni ejecutar comandos Git. Para SubTrack 1.0, apruebo la opción A para F0-10: desactivación de suscripciones. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 416):** Seguimos cerrando los contratos de la Fase 0. ZYKO y COSMO todavía no están autorizados para implementar código ni ejecutar comandos Git. Para SubTrack 1.0, apruebo la opción A para F0-10: desactivación de suscripciones. Continuemos con la siguiente decisión. recuerda que definiremos el tipo de ingreso ya sea 15nal o mensual ¡Correcto! Lo incorporamos a los contratos de SubTrack 1.0. Al registrar un ingreso recurrente, el usuario podrá elegir: - Quincenal: ingreso recibido dos veces al mes o cada 15 días, según la regla que definamos. - Mensual: ingreso recibido una vez al mes. - Extraordinario: ingreso ocasional, sin recurrencia.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-11 — Cálculo del ahorro proyectado

**Planteamiento y opciones (fuente desde línea 453):** Imagina que estamos a mitad de octubre y tenemos estos movimientos: | Ingresos ya recibidos | $600 | | -------------------------------- | ---- | | Ingresos futuros confirmados | $200 | | Pagos y gastos realizados | $250 | | Obligaciones y gastos pendientes | $150 | Saldo actual del mes ¿Cómo calcularemos el ahorro proyectado al finalizar el mes?

**Evidencia de aprobación de GRIM (línea 491):** Con esta decisión seguiremos cerrando las reglas del Dashboard antes de entregar los contratos definitivos a ZYKO y COSMO. Para SubTrack 1.0, apruebo la opción A para F0-11: cálculo del ahorro proyectado. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 493):** F0-11 aprobada Queda aprobada la opción A: proyección completa del ahorro para SubTrack 1.0. La fórmula será: \\[ \text{Ahorro proyectado} = \text{Saldo actual} +\text{Ingresos futuros confirmados} -\text{Obligaciones pendientes} \\] Los ingresos quincenales (días 15 y último día del mes) y mensuales se considerarán según sus fechas previstas. Los movimientos ya realizados no se contarán dos veces. Llevamos 11 decisiones principales aprobadas, más la definición de frecuencia quincenal.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-12 — ¿Qué fecha utilizaremos para registrar los movimientos?

**Planteamiento y opciones (fuente desde línea 505):** Imagina que registras un gasto hoy, pero realmente lo pagaste hace tres días. ¿Cómo quieres que SubTrack maneje las fechas? A. Fecha real elegida por el usuario (recomendada) SubTrack propone automáticamente la fecha de hoy, pero puedes cambiarla por la fecha en que realmente recibiste o pagaste el dinero. Los indicadores mensuales utilizan esa fecha. B. Siempre la fecha de registro SubTrack utiliza obligatoriamente la fecha actual del equipo, aunque el movimiento haya ocurrido antes. C. Guardar

**Evidencia de aprobación de GRIM (línea 527):** Seguimos en la Fase 0, sin autorizar todavía cambios de código a ZYKO o COSMO ni operaciones Git a los agentes.  Para SubTrack 1.0, apruebo la opción A para F0-12: gestión de fechas de movimientos. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 529):** F0-12 aprobada Queda aprobada la opción A: fecha real del movimiento seleccionada por el usuario. SubTrack 1.0 propondrá automáticamente la fecha actual, pero permitirá modificarla para registrar pagos, gastos o ingresos de días anteriores. Los indicadores financieros utilizarán la fecha real seleccionada. Esto no impide guardar internamente una fecha técnica de creación para controlar los registros. Llevamos 12 decisiones principales aprobadas.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-13 — ¿Cómo manejar las categorías de gastos?

**Planteamiento y opciones (fuente desde línea 539):** Imagina que registras estos gastos: - $35 de alimentación. - $20 de transporte. - $15 de entretenimiento. ¿Cómo quieres organizar las categorías en SubTrack? A. Categorías fijas SubTrack incluye categorías predefinidas como Alimentación, Transporte, Salud, Educación, Hogar y Entretenimiento. No puedes crear nuevas. B. Categorías predefinidas y personalizadas (recomendada)

**Evidencia de aprobación de GRIM (línea 565):** Seguimos cerrando las decisiones funcionales de la Fase 0. ZYKO y COSMO todavía no tienen autorización para implementar código. Para SubTrack 1.0, apruebo la opción B para F0-13: categorías de gastos. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 567):** F0-13 aprobada Queda aprobada la opción B: categorías de gastos predefinidas y personalizadas. SubTrack 1.0 incluirá categorías iniciales como Alimentación, Transporte, Salud y Educación, pero también permitirá crear categorías propias. Llevamos 13 decisiones principales aprobadas.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-14 — ¿Cómo manejar las correcciones de ingresos y gastos?

**Planteamiento y opciones (fuente desde línea 575):** Imagina que registraste un gasto de $50, pero después descubres que realmente fueron $35. ¿Cómo quieres que SubTrack conserve esa corrección? A. Editar directamente Cambias $50 por $35. Solo se conserva el valor nuevo, sin registro de la corrección. B. Editar y conservar historial (recomendada) Cambias $50 por $35, pero SubTrack guarda el valor anterior, el nuevo y la fecha de modificación. El Dashboard utiliza únicamente los $35 actuales. C. Anular y crear un registro nuevo

**Evidencia de aprobación de GRIM (línea 597):** Seguimos cerrando las decisiones de la Fase 0. Todavía no autorizamos implementación a ZYKO ni COSMO, y los agentes tienen prohibido ejecutar comandos Git. Para SubTrack 1.0, apruebo la opción B para F0-14: correcciones de ingresos y gastos. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 600):** F0-14 aprobada Queda aprobada la opción B: permitir correcciones de ingresos y gastos conservando el historial de modificaciones. SubTrack 1.0 guardará el valor anterior, el nuevo y la fecha de modificación. El Dashboard utilizará únicamente el importe vigente, sin duplicar movimientos. Llevamos 14 decisiones principales aprobadas.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-15 — ¿Cómo manejar los errores del sistema?

**Planteamiento y opciones (fuente desde línea 608):** Imagina que intentas registrar un pago, pero ese cobro ya fue pagado o la base de datos está ocupada. ¿Cómo debería responder SubTrack? A. Errores controlados con códigos y mensajes claros (recomendada) Core devuelve un resultado de éxito o error mediante `Result<T>`, con códigos como `COBRO_YA_PAGADO`. WinForms muestra un mensaje comprensible y SQLite mantiene sus errores técnicos dentro de Infrastructure. B. Utilizar excepciones para todos los errores Los servicios lanzan excepciones que WinFo

**Evidencia de aprobación de GRIM (línea 630):** Seguimos en Fase 0. ZYKO y COSMO todavía no están autorizados para implementar código, y los agentes no deben ejecutar ningún comando Git. Para SubTrack 1.0, apruebo la opción A para F0-15: manejo de errores del sistema. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 632):** F0-15 aprobada Queda aprobada la opción A: manejo de errores controlados mediante `Result<T>` y códigos identificables. Esto significa que: - ZYKO (Core): definirá los resultados y errores de negocio. - COSMO (Infrastructure): manejará los errores técnicos de SQLite sin exponer detalles sensibles. - GRIM (WinForms): mostrará mensajes comprensibles para el usuario. Los errores esperados se comunicarán mediante `Result<T>`; los fallos inesperados se tratarán como excepciones controladas. Llevamos 15 decisiones principales aprobadas.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-16 — ¿Cómo protegeremos los datos de SubTrack?

**Planteamiento y opciones (fuente desde línea 646):** Como SubTrack 1.0 almacenará información financiera personal en SQLite, debemos definir cómo realizar las copias de seguridad. Imagina que cambias de computadora o Windows presenta un problema. ¿Cómo recuperarías tus suscripciones, ingresos, gastos y pagos? A. Copia de seguridad manual El usuario selecciona «Crear respaldo» y SubTrack genera un archivo de respaldo de SQLite que puede guardar en otra ubicación. B. Copia automática y manual (recomendada) SubTrack crea un respaldo automático una ve

**Evidencia de aprobación de GRIM (línea 668):** Seguimos cerrando los contratos de Fase 0 antes de autorizar implementación a ZYKO y COSMO. Ningún agente tiene autorización para ejecutar comandos Git, ni siquiera de consulta. Para SubTrack 1.0, apruebo la opción B para F0-16: política de respaldos y restauración. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 670):** F0-16 aprobada — Respaldos automáticos y manuales SubTrack 1.0 tendrá: - Respaldo automático: una vez al día, al iniciar la aplicación, si todavía no existe uno de ese día. - Respaldo manual: el usuario podrá crearlo cuando lo necesite. - Restauración: con confirmación previa y validación del archivo. - Seguridad: copias consistentes mediante SQLite, con conservación limitada de respaldos automáticos. Llevamos 16 decisiones principales aprobadas.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-17 — ¿Cómo mostrar las próximas obligaciones?

**Planteamiento y opciones (fuente desde línea 681):** Imagina que tienes tres suscripciones: | Suscripción | Próximo cobro | Importe | | ----------- | -------------- | ------- | | Netflix | 22 de octubre | $12 | | Internet | 25 de octubre | $30 | | Spotify | 5 de noviembre | $7 | Queremos que el Dashboard te ayude a anticipar tus pagos sin confundirlos con gastos ya realizados. A. Mostrar únicamente los próximos 7 días Una lista breve de obligaciones cercanas, sin consultar períodos más amplios.

**Evidencia de aprobación de GRIM (línea 709):** Seguimos en Fase 0, definiendo los contratos antes de autorizar implementación a ZYKO y COSMO. Los agentes continúan sin autorización para ejecutar ningún comando Git. Para SubTrack 1.0, apruebo la opción B para F0-17: visualización de próximas obligaciones. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 711):** F0-17 aprobada — Próximas obligaciones SubTrack 1.0 mostrará los próximos 7 días por defecto, con filtros para consultar 15 días, 30 días o el mes actual. Los cobros vencidos aparecerán por separado, y los cobros futuros no se considerarán pagos realizados. Llevamos 17 decisiones principales aprobadas.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-18 — ¿Cómo evitar registrar movimientos duplicados?

**Planteamiento y opciones (fuente desde línea 719):** Imagina que presionas dos veces el botón «Registrar pago» porque la aplicación tarda en responder. Queremos evitar que SubTrack registre dos pagos para una misma obligación. A. Protección únicamente en la interfaz El botón se desactiva mientras se guarda el pago. Es sencillo, pero no protege completamente contra operaciones duplicadas. B. Protección en interfaz, Core y SQLite (recomendada) WinForms bloquea temporalmente el botón, Core comprueba que el cobro no tenga un pago vigente y SQLite impi

**Evidencia de aprobación de GRIM (línea 743):** Seguimos sin autorizar implementación ni comandos Git a los agentes. Para SubTrack 1.0, apruebo la opción B para F0-18: protección contra registros duplicados. Continuemos con la siguiente decisión. Para SubTrack 1.0, apruebo la opción B para F0-18: protección contra registros duplicados. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 746):** F0-18 aprobada — Protección contra registros duplicados SubTrack 1.0 aplicará tres niveles de protección: - GRIM (WinForms): desactivar temporalmente el botón mientras se registra un pago. - ZYKO (Core): validar que el cobro no tenga otro pago vigente. - COSMO (SQLite): garantizar que no existan dos pagos vigentes para el mismo cobro, incluso ante operaciones simultáneas. Esta decisión complementa F0-02, que permite anular un pago y registrar otro sin perder el historial. Llevamos 18 decisiones principales aprobadas.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-19 — ¿Qué hacer con los ingresos y gastos futuros cuando llega su fecha?

**Planteamiento y opciones (fuente desde línea 758):** Imagina que programas un ingreso quincenal de $300 para el 30 de octubre y un gasto de $40 para el 28 de octubre. Llega el día correspondiente, pero todavía no has confirmado que recibiste o pagaste ese dinero. ¿Cómo debe comportarse SubTrack? A. Confirmación automática Cuando llega la fecha, SubTrack convierte el ingreso o gasto previsto en un movimiento realizado, sin intervención del usuario. B. Confirmación manual con pendientes (recomendada) Cuando llega la fecha, el movimiento sigue pendie

**Evidencia de aprobación de GRIM (línea 782):** Continuamos cerrando la Fase 0 antes de autorizar a ZYKO y COSMO a implementar. Los agentes siguen sin permiso para ejecutar ningún comando Git, ni siquiera de consulta. Para SubTrack 1.0, apruebo la opción B para F0-19: confirmación de ingresos y gastos previstos. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 784):** F0-19 aprobada — Confirmación manual de movimientos previstos SubTrack 1.0 tendrá el siguiente comportamiento: - Los ingresos y gastos previstos no se registrarán automáticamente como realizados. - El usuario deberá confirmar cuándo recibió o pagó el dinero. - Si llega la fecha y no se confirma, el movimiento aparecerá como pendiente atrasado. - El Dashboard distinguirá los importes previstos de los movimientos reales, evitando duplicaciones. Llevamos 19 decisiones principales aprobadas.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-20 — ¿Cómo manejar el saldo inicial?

**Planteamiento y opciones (fuente desde línea 795):** Imagina que instalas SubTrack por primera vez y ya tienes $500 ahorrados. Ese dinero existía antes de comenzar a registrar ingresos y gastos en la aplicación. ¿Cómo debería incorporarse al sistema? A. Comenzar siempre desde $0 El saldo se calcula únicamente con los ingresos y gastos registrados desde que empezaste a utilizar SubTrack. B. Configurar un saldo inicial (recomendada) Al comenzar, puedes indicar cuánto dinero tienes disponible y la fecha desde la que llevarás el control. Ese importe s

**Evidencia de aprobación de GRIM (línea 821):** Seguimos en Fase 0. ZYKO y COSMO todavía no tienen autorización para implementar código, y ningún agente puede ejecutar comandos Git. Para SubTrack 1.0, apruebo la opción B para F0-20: configuración del saldo inicial. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 823):** F0-20 aprobada — Configuración del saldo inicial SubTrack 1.0 permitirá establecer un saldo inicial y una fecha de inicio. Por ejemplo, si comienzas con $500, ese dinero formará parte de tu saldo disponible, pero no se contabilizará como un ingreso nuevo. El saldo inicial se incorporará una sola vez para evitar duplicaciones. Ya tenemos 20 decisiones principales aprobadas.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-21 — ¿Cómo manejar los cambios del saldo inicial?

**Planteamiento y opciones (fuente desde línea 833):** Imagina que configuraste un saldo inicial de $500, pero después descubres que realmente tenías $450. ¿Cómo debería permitirte corregirlo SubTrack? A. Permitir modificarlo libremente Puedes cambiar el saldo inicial en cualquier momento. El sistema recalcula el saldo acumulado sin conservar un historial específico del cambio. B. Permitir corrección con historial (recomendada) Puedes corregir el saldo inicial, pero SubTrack conserva el importe anterior, el nuevo y la fecha de corrección. El saldo a

**Evidencia de aprobación de GRIM (línea 857):** Por ahora, no se autoriza implementación ni ejecución de comandos Git por agentes. Para SubTrack 1.0, apruebo la opción B para F0-21: corrección del saldo inicial. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 859):** F0-21 aprobada — Corrección del saldo inicial con historial SubTrack 1.0 permitirá corregir el saldo inicial sin perder el registro del cambio. Por ejemplo, si corriges $500 a $450, el sistema conservará ambos valores y la fecha de modificación, pero utilizará únicamente los $450 vigentes para calcular el saldo. Llevamos 21 decisiones principales aprobadas.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-22 — ¿Cómo manejar las suscripciones con importes variables?

**Planteamiento y opciones (fuente desde línea 867):** Imagina que tienes una suscripción de electricidad o un servicio cuyo importe cambia cada mes. En septiembre pagaste $35, pero en octubre te cobran $42. ¿Cómo debería funcionar SubTrack? A. Solo importes fijos Todas las suscripciones tienen un importe fijo. Para cambiarlo, debes editar la suscripción, y el nuevo importe se aplica a los cobros futuros. B. Importe fijo o variable (recomendada) Al crear una suscripción eliges si su importe es fijo o variable. En las variables, SubTrack utiliza un i

**Evidencia de aprobación de GRIM (línea 893):** Seguimos en Fase 0: ZYKO y COSMO aún no tienen autorización para implementar, y ningún agente puede ejecutar comandos Git. Para SubTrack 1.0, apruebo la opción B para F0-22: suscripciones de importe fijo o variable. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 895):** F0-22 aprobada — Suscripciones de importe fijo o variable SubTrack 1.0 permitirá dos tipos de suscripciones: - Importe fijo: el cobro mantiene su valor establecido. - Importe variable: se utiliza un importe estimado para las proyecciones y se establece el valor definitivo antes de registrar el pago. Por ejemplo, si estimamos $35 de electricidad, pero la factura llega por $42, el cobro se ajustará a $42 antes de pagarlo. El historial anterior permanecerá intacto. Esto respeta F0-04: los pagos siempre serán completos, sin pagos parciales. Llevamos 22 decisiones principales aprobadas.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-23 — ¿Qué hacer cuando una suscripción cambia su fecha de cobro?

**Planteamiento y opciones (fuente desde línea 908):** Imagina que Netflix normalmente te cobra el día 10, pero la empresa cambia la fecha al día 15. ¿Cómo debería manejarlo SubTrack? A. Cambiar toda la programación Modificar la fecha de cobro y recalcular también los cobros anteriores que todavía estén pendientes. B. Aplicar el cambio solo hacia el futuro (recomendada) Conserva los cobros anteriores con sus fechas originales. A partir de una fecha efectiva, los nuevos cobros se generan con la programación actualizada. C. Crear una suscripción nueva

**Evidencia de aprobación de GRIM (línea 932):** Seguimos en Fase 0. No se ha autorizado implementación a ZYKO ni COSMO, y los agentes tienen prohibido ejecutar cualquier comando Git. Para SubTrack 1.0, apruebo la opción B para F0-23: cambios de fecha de cobro de suscripciones. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 934):** F0-23 aprobada — Cambios de fecha de cobro SubTrack 1.0 aplicará los cambios de fecha de las suscripciones únicamente hacia el futuro. Esto significa que los cobros anteriores conservarán sus fechas originales, los pagos realizados permanecerán intactos y los cobros futuros pendientes se ajustarán de manera controlada, evitando duplicados. Llevamos 23 decisiones principales aprobadas.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-24 — ¿Qué ocurre si una suscripción se paga antes de su vencimiento?

**Planteamiento y opciones (fuente desde línea 942):** Imagina que tu suscripción de Internet vence el 25 de octubre, pero decides pagarla el 20 de octubre. ¿Cómo debe registrar SubTrack ese pago? A. Permitir pagos anticipados (recomendada) Puedes pagar cualquier cobro ya generado, aunque todavía no haya vencido. SubTrack conserva la fecha original de vencimiento y registra la fecha real del pago. B. Permitir pagos solo desde el vencimiento Debes esperar hasta el día programado para registrar el pago. C. Adelantar la fecha de vencimiento

**Evidencia de aprobación de GRIM (línea 966):** Continuamos en Fase 0: no hay autorización para implementar código ni para que los agentes ejecuten comandos Git. Para SubTrack 1.0, apruebo la opción A para F0-24: pagos anticipados de suscripciones. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 968):** F0-24 aprobada — Pagos anticipados SubTrack 1.0 permitirá pagar una suscripción antes de su vencimiento. Por ejemplo, si Internet vence el 25 de octubre, pero lo pagas el 20 de octubre: - El cobro conservará su vencimiento del 25. - El pago se registrará con fecha real del 20. - El Dashboard contabilizará el dinero como gastado en octubre. - La fecha del siguiente cobro no cambiará automáticamente. Llevamos 24 decisiones principales aprobadas.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-25 — ¿Cómo manejar los cobros de suscripciones que ya existían antes de instalar SubTrack?

**Planteamiento y opciones (fuente desde línea 981):** Imagina que comienzas a utilizar SubTrack el 20 de octubre, pero tienes una suscripción que venció el 10 de octubre y todavía no has pagado. ¿Cómo debería registrar SubTrack esa obligación anterior? A. Comenzar únicamente con cobros futuros SubTrack genera obligaciones desde la fecha de inicio y no permite incorporar cobros anteriores. B. Permitir registrar obligaciones iniciales pendientes (recomendada) Al configurar una suscripción, puedes indicar si tienes cobros anteriores sin pagar, con sus

**Evidencia de aprobación de GRIM (línea 1005):** Seguimos en Fase 0. ZYKO y COSMO aún no tienen autorización para implementar código, y ningún agente puede ejecutar comandos Git. Para SubTrack 1.0, apruebo la opción B para F0-25: registro de obligaciones iniciales pendientes. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 1008):** F0-25 aprobada — Obligaciones iniciales pendientes SubTrack 1.0 permitirá registrar cobros anteriores a la instalación que todavía estén pendientes de pago. Por ejemplo, si comienzas a utilizar el sistema el 20 de octubre, pero debes $30 de Internet desde el día 10, podrás incorporar esa obligación con su fecha e importe originales. El sistema no inventará cobros históricos ni duplicará obligaciones al sincronizar las suscripciones. Llevamos 25 decisiones principales aprobadas.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-26 — ¿Cómo manejar la restauración de un respaldo?

**Planteamiento y opciones (fuente desde línea 1018):** Ya aprobamos en F0-16 que SubTrack tendrá respaldos automáticos y manuales. Ahora debemos definir qué ocurrirá cuando restaures uno. Imagina que tienes información registrada hasta el 20 de octubre, pero restauras un respaldo creado el 18 de octubre. A. Reemplazar directamente la información SubTrack restaura el respaldo seleccionado después de una confirmación, sustituyendo la base de datos actual. Los movimientos posteriores al respaldo dejan de aparecer. B. Crear una copia de seguridad antes 

**Evidencia de aprobación de GRIM (línea 1060):** En resumen: nos faltan aproximadamente 5 decisiones y el cierre documental de Fase 0. Después pasamos al desarrollo, sin necesidad de seguir ampliando requisitos. Para SubTrack 1.0, apruebo la opción B para F0-26: restauración segura de respaldos. Continuemos con la siguiente decisión.

**Regla/formulación registrada (línea 1062):** F0-26 aprobada — Restauración segura de respaldos SubTrack 1.0 restaurará los respaldos siguiendo estas reglas: - Verificar la integridad del respaldo antes de utilizarlo. - Mostrar una advertencia sobre la información que será reemplazada. - Crear una copia de seguridad de la base actual antes de restaurar. - Cerrar las conexiones a SQLite durante la operación. - Conservar la base anterior si la restauración falla. Avance: 26 de 30 decisiones previstas aprobadas. Nos quedan cuatro decisiones para cerrar esta etapa funcional.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-27 — ¿Cómo evitar ingresos y gastos duplicados?

**Planteamiento y opciones (fuente desde línea 1074):** En F0-18 ya protegimos los pagos de suscripciones. Ahora debemos definir qué sucede con los ingresos y gastos independientes. Ejemplo: registras un gasto de $25 en alimentación. La aplicación tarda en responder y presionas «Guardar» dos veces. A. Protección solamente en WinForms Desactivar el botón mientras se guarda. Es sencillo, pero un reintento técnico podría generar otro registro. B. Identificador único por operación (recomendada) Cada solicitud de registro tendrá un identificador único. Si

**Evidencia de aprobación de GRIM (línea 1098):** Seguimos sin autorizar implementación ni comandos Git a los agentes. Para SubTrack 1.0, apruebo la opción B para F0-27: prevención de ingresos y gastos duplicados. Continuemos con F0-28.

**Regla/formulación registrada (línea 1100):** F0-27 aprobada — Prevención de movimientos duplicados SubTrack 1.0 utilizará un identificador único por operación para registrar ingresos y gastos independientes. Esto permitirá que: - Un doble clic o reintento no registre dos veces el mismo movimiento. - Dos gastos legítimos del mismo importe y fecha puedan registrarse normalmente. - SQLite garantice la unicidad y Core reconozca los reintentos. Avance de Fase 0 27 de 30 decisiones

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-28 — ¿Hasta qué fecha generar los cobros de suscripciones?

**Planteamiento y opciones (fuente desde línea 1114):** En F0-01 aprobamos generar cobros por rangos de fechas, evitando duplicados. Ahora debemos definir cuánto tiempo hacia el futuro puede preparar SubTrack. Por ejemplo, tienes Netflix por $12 mensuales. Al abrir el Dashboard, ¿cuántos cobros futuros debería tener preparados el sistema? A. Únicamente el mes actual Se generan cobros hasta el último día del mes. Para consultar meses posteriores, habría que sincronizarlos cuando llegue el momento. B. Generación por rango solicitado, con límite de 12 m

**Evidencia de aprobación de GRIM (línea 1138):** Seguimos en Fase 0: todavía no autorizamos implementación, y los agentes tienen prohibido ejecutar cualquier comando Git. Para SubTrack 1.0, apruebo la opción B para F0-28: horizonte de generación de cobros. Continuemos con F0-29.

**Regla/formulación registrada (línea 1140):** F0-28 aprobada — Horizonte de generación de cobros SubTrack 1.0 generará cobros únicamente para el rango de fechas solicitado, con un límite de 12 meses hacia el futuro por solicitud. Esto significa que: - El Dashboard podrá consultar sus períodos sin generar cobros innecesarios. - Los cobros vencidos conservarán su historial. - La sincronización evitará duplicados. - Los cambios y desactivaciones de suscripciones se respetarán. Avance de Fase 0

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-29 — ¿Qué debe pasar si ocurre un fallo mientras se guarda una operación?

**Planteamiento y opciones (fuente desde línea 1155):** Imagina que registras un pago de $30. SubTrack debe guardar el pago, asignar su comprobante y actualizar el estado del cobro. Pero ocurre un error de SQLite a mitad del proceso. ¿Cómo debe comportarse el sistema? A. Guardar lo que se haya completado Si se guardó el pago pero falló el comprobante, se conserva el pago y se intenta completar después. B. Operaciones atómicas con transacciones (recomendada) El pago, su comprobante y los cambios relacionados se confirman juntos. Si algo falla antes de

**Evidencia de aprobación de GRIM (línea 1181):** Todavía no autorizamos implementación a ZYKO ni COSMO, y ningún agente puede ejecutar comandos Git, incluidos los de consulta. Para SubTrack 1.0, apruebo la opción B para F0-29: operaciones atómicas y recuperación ante fallos. Continuemos con F0-30.

**Regla/formulación registrada (línea 1183):** F0-29 aprobada — Operaciones atómicas y recuperación ante fallos SubTrack 1.0 utilizará transacciones SQLite para garantizar que una operación financiera se complete correctamente o no se aplique. Por ejemplo, al registrar un pago, el comprobante y los cambios asociados se guardarán juntos. Si algo falla antes de confirmar la transacción, se revertirán los cambios. Esto complementa F0-18 y F0-27, que protegen contra operaciones duplicadas. Avance de Fase 0 29 de 30 decisiones

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.

### F0-30 — ¿Qué debe hacer SubTrack al detectar una base de datos dañada?

**Planteamiento y opciones (fuente desde línea 1195):** Esta será la última decisión funcional prevista. Imagina que Windows se apaga inesperadamente y, al abrir SubTrack, SQLite detecta que el archivo de datos está dañado. ¿Cómo debería responder la aplicación? A. Crear automáticamente una base nueva SubTrack reemplaza la base dañada por una vacía para permitir que la aplicación continúe funcionando. B. Proteger los datos y ofrecer recuperación (recomendada) SubTrack informa del problema, bloquea las operaciones de escritura y permite intentar resta

**Evidencia de aprobación de GRIM (línea por revisar):** Aprobación literal no aislada; revisar contexto

**Regla/formulación registrada (línea 1321):** F0-30 aprobada — Protección ante corrupción de SQLite SubTrack 1.0 deberá detectar problemas de integridad, bloquear las escrituras y ofrecer la restauración de un respaldo válido, sin sobrescribir ni eliminar automáticamente la base de datos dañada. Decisiones funcionales 30 de 30 aprobadas Completamos la definición funcional prevista. Ahora corresponde consolidar y revisar los contratos técnicos antes de iniciar la implementación. El cierre documental debe producir una referencia común para ZYKO (Core), COSMO (Infrastructure/SQLite) y GRIM (WinForms), evitando que cada integrante interprete de forma distinta las 30 decisiones.

**Dependencia:** Core define la regla; Infrastructure la persiste cuando corresponda; WinForms la presenta cuando corresponda.


## 10. Decisiones técnicas — F0-T01 a F0-T45

### F0-T01 — Modelo ampliado de entidades y estados

**Planteamiento y opciones (fuente desde línea 1570):** Ingresos quincenales COSMO solo contempló `MENSUAL` y `EXTRAORDINARIO`. Debemos incorporar las plantillas quincenales de los días 15 y último día del mes. Cobros cancelados e importes variables ZYKO y COSMO propusieron inicialmente cobros `Pending/Paid`. Necesitamos también `Cancelled` y distinguir importe estimado de definitivo. Gastos e ingresos previstos Sus modelos iniciales representan principalmente movimientos realizados. F0-06, F0-07 y F0-19 requieren diferenciar previsión y confirmación

**Evidencia de aprobación de GRIM (línea 1608):** Importante: ZYKO, COSMO y GRIM siguen sin autorización para implementar. Codex, Antigravity y OpenCode con OmniRoute tampoco pueden ejecutar ningún comando Git, ni de lectura ni de escritura. Para SubTrack 1.0, elijo la opción A para F0-T01: revisión del modelo de entidades y estados. Continuemos con los contratos técnicos, sin autorizar implementa

**Regla/formulación registrada (línea 1610):** F0-T01 aprobada — Modelo ampliado de entidades y estados SubTrack 1.0 conservará el modelo ampliado para cubrir las 30 decisiones funcionales. No reduciremos los requisitos ni eliminaremos funciones aprobadas. Ahora continuamos con F0-T02: relaciones entre entidades y restricciones de SQLite.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T02 — ¿Cómo deben relacionarse los registros financieros?

**Planteamiento y opciones (fuente desde línea 1616):** La regla principal es que ninguna operación debe perder su historial ni generar registros duplicados. Propongo las siguientes relaciones: Relaciones principales propuestas. Income y Expense son movimientos independientes; OpeningBalance y los historiales se gestionan aparte. | Relación | Restricción | | ------------------------------- | ---------------------------------------------------------------------------------------------- | | Suscripción → cobros | Una suscripción puede tener muchos cobr

**Evidencia de aprobación de GRIM (línea 1664):** Seguimos en revisión documental. ZYKO, COSMO y GRIM no tienen autorización para implementar, y Codex, Antigravity y OpenCode con OmniRoute mantienen la prohibición absoluta de ejecutar Git. Para SubTrack 1.0, apruebo la opción A para F0-T02: relaciones entre entidades y restricciones SQLite. Continuemos con los contratos técnicos sin autorizar impl

**Regla/formulación registrada (línea 1667):** F0-T02 aprobada — Relaciones estrictas y protección en tres capas Queda establecido que SubTrack 1.0 protegerá sus operaciones mediante WinForms + Core + SQLite, conservando el historial financiero y evitando registros duplicados. Ahora avanzamos a F0-T03: contratos de servicios e interfaces, donde definiremos cómo se comunicarán los módulos de ZYKO, COSMO y GRIM.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T03 — ¿Cómo se comunicarán los tres módulos?

**Planteamiento y opciones (fuente desde línea 1673):** La arquitectura aprobada establece estas responsabilidades: Flujo lógico simplificado. Infrastructure implementa interfaces definidas en Core; WinForms configura las dependencias. | Interfaz en Core | Responsabilidad | | ------------------------- | ---------------------------------------------- | | `ISubscriptionService` | Crear, editar y desactivar suscripciones | | `IScheduledChargeService` | Generar y consultar obligaciones | | `IPaymentService` | Registrar y anular pagos; obtener comprobante

**Evidencia de aprobación de GRIM (línea 1724):** Estado: seguimos exclusivamente en diseño y revisión. La aprobación de esta decisión tampoco autoriza a los agentes a modificar código o ejecutar comandos Git. Para SubTrack 1.0, apruebo la opción B para F0-T03: organización de servicios e interfaces. Continuemos con la definición de firmas y DTO sin autorizar implementación ni ningún comando Git a

**Regla/formulación registrada (línea 1726):** F0-T03 aprobada — Servicios asíncronos con resultados tipados Queda aprobado para SubTrack 1.0: - Servicios públicos asíncronos con `Task<Result<T>>` o `Task<Result>`. - Uso de `CancellationToken` cuando corresponda. - Cálculos financieros puros síncronos dentro de Core. - Operaciones SQLite y respaldos sin bloquear innecesariamente WinForms. - Errores de negocio identificados mediante códigos estables. Aclaración técnica: que un método devuelva `Task` no garantiza por sí solo que no bloquee la interfaz. ZYKO y COSMO deberán utilizar operaciones asíncronas reales cuando estén disponibles y gestionar adecuadamente las operaciones bloqueantes.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T04 — Contratos de solicitudes y respuestas (DTO)

**Planteamiento y opciones (fuente desde línea 1738):** Ahora definimos qué información intercambiarán GRIM, ZYKO y COSMO. Solicitud enviada desde WinForms ``` public sealed record RegisterPaymentRequest( Guid ScheduledChargeId, DateOnly PaymentDate, Guid OperationId ); ```

**Evidencia de aprobación de GRIM (línea 1866):** Seguimos únicamente documentando. ZYKO, COSMO, GRIM, Codex, Antigravity y OpenCode con OmniRoute no tienen autorización para implementar ni ejecutar comandos Git.  Para SubTrack 1.0, apruebo la opción A para F0-T04 sobre protección contra registros duplicados. Continuemos con la siguiente decisión técnica usando explicaciones sencillas y sin mostra

**Regla/formulación registrada (línea 1868):** F0-T04 aprobada — Protección completa contra duplicados SubTrack 1.0 evitará que un mismo intento de registrar pagos, ingresos o gastos genere operaciones duplicadas, incluso cuando ocurra un doble clic o un error de comunicación. AVANCE DE CONTRATOS TÉCNICOS # 4 aprobados F0-T01 a F0-T04 Las cuatro decisiones revisadas están aprobadas. Aún quedan otros contratos técnicos por definir.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T05 — ¿Qué pasa si falla el guardado de un pago?

**Planteamiento y opciones (fuente desde línea 1880):** Imagina que pagas una suscripción de $15.00. SubTrack necesita realizar tres acciones: 1. Guardar el pago. 2. Marcar la obligación como pagada. 3. Generar su comprobante interno. Ahora imagina que ocurre un error justo después de guardar el pago, pero antes de completar las otras acciones. No queremos que el sistema registre un pago sin comprobante o que marque una obligación como pagada sin tener su pago correspondiente. La solución recomendada es tratar las tres acciones como una sola operació

**Evidencia de aprobación de GRIM (línea 1910):** Seguimos únicamente planificando. No necesitas tu PC y ningún agente tiene autorización para modificar código ni ejecutar comandos Git. Para SubTrack 1.0, apruebo la opción A para F0-T05 sobre transacciones atómicas de pagos y comprobantes. Continuemos con la siguiente decisión técnica, usando explicaciones sencillas y sin mostrar código. No autori

**Regla/formulación registrada (línea 1913):** F0-T05 aprobada — Transacciones atómicas Queda establecido que SubTrack 1.0 guardará el pago, la actualización de la obligación y el comprobante como una sola operación segura. Si algo falla antes de finalizar, no se guardarán cambios parciales. Si ocurre un error después de confirmar el guardado, SubTrack comprobará qué se registró antes de volver a intentarlo.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T06 — ¿Cómo controlaremos los cambios en las suscripciones?

**Planteamiento y opciones (fuente desde línea 1919):** Imagina que tienes Netflix por $12.99 mensuales y, a partir de noviembre, aumenta a $15.99. Ejemplo de historial de cobros | Septiembre | $12.99 | Histórico | | ---------- | ------ | ----------- | | Octubre | $12.99 | Histórico | | Noviembre | $15.99 | Nuevo monto | | Diciembre | $15.99 | Nuevo monto | Ya aprobamos funcionalmente que los cambios de monto, frecuencia o fecha de cobro no deben alterar las obligaciones históricas. Ahora necesitamos definir cómo guardaremos esos cambios para que ZYK

**Evidencia de aprobación de GRIM (línea 1949):** Seguimos únicamente en revisión documental. No necesitas tu PC y ningún agente —Codex, Antigravity u OpenCode con OmniRoute— tiene autorización para implementar ni ejecutar comandos Git, incluidos los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T06 sobre historial y versiones de suscripciones. Continuemos con la siguiente decisión té

**Regla/formulación registrada (línea 1951):** F0-T06 aprobada — Historial de versiones de suscripciones Queda establecido que cada modificación de una suscripción tendrá una versión con su fecha de entrada en vigor. Esto significa que: - Los cobros históricos conservarán sus importes y fechas originales. - Los cambios futuros se aplicarán desde la fecha correspondiente. - Los cobros futuros pendientes podrán ajustarse sin duplicarse. - Podremos consultar qué configuración tenía una suscripción en una fecha determinada. Llevamos 6 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T07 — ¿Cómo controlaremos los ingresos recurrentes?

**Planteamiento y opciones (fuente desde línea 1964):** Imagina que recibes un salario de $600 mensuales, dividido en dos pagos: Ejemplo: salario quincenal 15 de noviembre # $300.00 Previsto 30 de noviembre # $300.00

**Evidencia de aprobación de GRIM (línea 2002):** Seguimos únicamente planificando. No necesitas tu PC y ningún agente tiene autorización para implementar ni ejecutar comandos Git, incluidos los comandos de consulta. Para SubTrack 1.0, apruebo la opción A para F0-T07 sobre ingresos recurrentes y su registro individual. Continuemos con la siguiente decisión técnica usando explicaciones sencillas y 

**Regla/formulación registrada (línea 2004):** F0-T07 aprobada — Ingresos recurrentes con registros individuales SubTrack 1.0 guardará una plantilla para los ingresos mensuales o quincenales y creará cada ingreso previsto por separado. Esto permitirá confirmar cada ingreso cuando realmente recibas el dinero, sin duplicarlo ni modificar los demás. Llevamos 7 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T08 — ¿Cómo administraremos los gastos futuros?

**Planteamiento y opciones (fuente desde línea 2012):** Imagina que sabes que el 28 de noviembre debes pagar $45 de electricidad, pero todavía no has realizado ese pago. 28 de noviembre Factura de electricidad Planificado Afecta el ahorro proyectado, pero todavía no reduce el saldo real. Pagado

**Evidencia de aprobación de GRIM (línea 2048):** Seguimos únicamente en planificación documental. ZYKO, COSMO, GRIM, Codex, Antigravity y OpenCode con OmniRoute no tienen autorización para implementar ni ejecutar comandos Git, incluidos los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T08 sobre confirmación de gastos futuros. Continuemos con la siguiente decisión técnica con explica

**Regla/formulación registrada (línea 2050):** F0-T08 aprobada — Confirmación de gastos futuros Queda establecido que cada gasto conservará un único registro, que cambiará de Planificado a Confirmado cuando realmente lo pagues. También conservará su fecha prevista y la fecha real de pago, sin descontarse dos veces. Ya llevamos 8 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T09 — ¿Cómo controlaremos el saldo inicial y sus correcciones?

**Planteamiento y opciones (fuente desde línea 2058):** Imagina que comienzas a utilizar SubTrack con $500 en tu cuenta. Ese dinero ya lo tenías antes de utilizar la aplicación, por lo que no debe registrarse como un ingreso nuevo. Después descubres que tu saldo inicial correcto era $480 porque olvidaste una operación anterior. Ejemplo de corrección Saldo inicial anterior # $500.00 Saldo inicial corregido

**Evidencia de aprobación de GRIM (línea 2096):** Seguimos exclusivamente en planificación. No necesitas tu PC y ningún agente tiene autorización para implementar ni ejecutar comandos Git, incluidos los comandos de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T09 sobre saldo inicial e historial de correcciones. Continuemos con la siguiente decisión técnica con explicaciones sencillas, s

**Regla/formulación registrada (línea 2098):** F0-T09 aprobada — Saldo inicial único con historial SubTrack 1.0 tendrá un único saldo inicial vigente, con su fecha de inicio. Si corriges ese saldo, conservará el valor anterior, el nuevo y la fecha de modificación. El Dashboard utilizará solamente el saldo vigente, sin registrar las correcciones como ingresos o gastos. Ya llevamos 9 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T10 — ¿Cómo guardaremos las correcciones de ingresos y gastos?

**Planteamiento y opciones (fuente desde línea 2106):** Imagina que registras un gasto de supermercado por $35.00, pero después revisas el recibo y descubres que realmente fueron $38.50. Ejemplo de corrección de gasto Registro original # $35.00 Valor corregido # $38.50 Historial de modificación

**Evidencia de aprobación de GRIM (línea 2144):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. ZYKO, COSMO y GRIM no tienen autorización para implementar, y Codex, Antigravity y OpenCode con OmniRoute no pueden ejecutar ningún comando Git, ni siquiera de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T10 sobre historial de correcciones de ingresos y gastos. Con

**Regla/formulación registrada (línea 2147):** F0-T10 aprobada — Historial independiente de correcciones SubTrack 1.0 conservará el registro actual de cada ingreso o gasto y guardará por separado su historial de modificaciones. Cada corrección registrará los valores anteriores, los nuevos y la fecha del cambio. El Dashboard contabilizará únicamente el valor vigente. Ya tenemos 10 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T11 — ¿Cómo gestionaremos las categorías de gastos?

**Planteamiento y opciones (fuente desde línea 2155):** Imagina que registras tus gastos en categorías como alimentación, transporte, servicios y entretenimiento. Pero después necesitas una categoría nueva llamada Universidad. Ejemplo de categorías Alimentación Transporte Servicios Universidad

**Evidencia de aprobación de GRIM (línea 2187):** Seguimos únicamente en planificación documental. No necesitas tu PC y ningún agente tiene autorización para implementar ni ejecutar comandos Git, incluidos los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T11 sobre categorías de gastos y conservación del historial. Continuemos con la siguiente decisión técnica con explicaciones sencil

**Regla/formulación registrada (línea 2189):** F0-T11 aprobada — Categorías de gastos con historial protegido SubTrack 1.0 permitirá crear y editar categorías personalizadas. Cuando una categoría tenga gastos asociados, podrás desactivarla, pero no eliminar su historial. Llevamos 11 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T12 — ¿Cómo calcularemos el Dashboard financiero?

**Planteamiento y opciones (fuente desde línea 2195):** Ahora debemos decidir cómo se actualizarán los saldos, gastos, ingresos y proyecciones que verás en la pantalla principal. Imagina que tienes la siguiente situación: Ejemplo de un mes en SubTrack Saldo inicial # $500.00 Ingreso recibido # +$300.00

**Evidencia de aprobación de GRIM (línea 2249):** Seguimos exclusivamente en planificación. No necesitas tu PC. ZYKO, COSMO y GRIM no están autorizados a implementar; Codex, Antigravity y OpenCode con OmniRoute no pueden ejecutar comandos Git de ningún tipo, ni siquiera de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T12 sobre cálculos del Dashboard financiero. Continuemos con la siguie

**Regla/formulación registrada (línea 2251):** F0-T12 aprobada — Cálculos financieros centralizados en Core Queda establecido que: - ZYKO (Core): realizará los cálculos financieros. - COSMO (Infrastructure): obtendrá y guardará los datos en SQLite. - GRIM (WinForms): mostrará los resultados sin recalcularlos. Así evitaremos que distintas partes de SubTrack muestren saldos diferentes. Ya llevamos 12 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T13 — ¿Cómo actualizaremos el Dashboard?

**Planteamiento y opciones (fuente desde línea 2263):** Imagina que tienes $500 de saldo y registras un gasto de $40. El Dashboard debe reflejar el nuevo saldo de $460. Antes del gasto # $500.00 Después del gasto # $460.00 La pregunta es cuándo debe actualizarse esta información en pantalla.

**Evidencia de aprobación de GRIM (línea 2293):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. Ningún agente tiene autorización para implementar ni ejecutar comandos Git, incluidos los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T13 sobre actualización del Dashboard. Continuemos con la siguiente decisión técnica usando explicaciones sencillas y sin mostra

**Regla/formulación registrada (línea 2295):** F0-T13 aprobada — Actualización automática del Dashboard SubTrack 1.0 actualizará sus indicadores después de registrar, corregir, confirmar o anular correctamente una operación financiera. También consultará los datos actualizados al regresar al Dashboard, sin modificar la base de datos durante esa consulta. Llevamos 13 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T14 — ¿Cómo evitaremos perder información si se cierra la aplicación?

**Planteamiento y opciones (fuente desde línea 2303):** Imagina que estás registrando un gasto y, de repente, Windows se reinicia o se corta la energía. Queremos que SubTrack conserve todas las operaciones que ya se guardaron correctamente. Ejemplo: registro de un gasto de $25 El gasto se guardó correctamente Aunque cierres SubTrack después, el gasto permanece registrado en SQLite. El programa se cerró antes de terminar el guardado Al reiniciar, SubTrack comprobará si la operación se completó antes de permitir un reintento, evitando duplicados.

**Evidencia de aprobación de GRIM (línea 2335):** Seguimos únicamente en planificación documental. No necesitas tu PC. ZYKO, COSMO y GRIM no están autorizados a implementar, y Codex, Antigravity y OpenCode con OmniRoute no pueden ejecutar comandos Git de ningún tipo, ni siquiera de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T14 sobre protección de datos y durabilidad de SQLite. Contin

**Regla/formulación registrada (línea 2338):** F0-T14 aprobada — Protección reforzada de SQLite Queda establecido que SubTrack 1.0 priorizará la integridad y conservación de la información financiera. COSMO deberá contemplar transacciones seguras, claves foráneas, sincronización reforzada con el disco y recuperación ante errores, cuando llegue la fase de implementación. Llevamos 14 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T15 — ¿Dónde guardaremos los datos de SubTrack?

**Planteamiento y opciones (fuente desde línea 2346):** Imagina que instalas SubTrack en tu computadora y comienzas a registrar suscripciones, ingresos y gastos. Necesitamos decidir dónde se guardará la base de datos para que no se pierda al cerrar la aplicación ni dependa de la carpeta donde se encuentre el programa. A. Carpeta privada del usuario de Windows (recomendada) SubTrack guardará su base de datos en una carpeta propia dentro de los datos locales del usuario de Windows. Las actualizaciones normales del programa no deberían reemplazarla y no

**Evidencia de aprobación de GRIM (línea 2372):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. ZYKO, COSMO y GRIM no tienen autorización para implementar; Codex, Antigravity y OpenCode con OmniRoute tampoco pueden ejecutar comandos Git, incluidos los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T15 sobre la ubicación de la base de datos SQLite. Continuemos

**Regla/formulación registrada (línea 2374):** F0-T15 aprobada — Ubicación de la base de datos SubTrack 1.0 guardará su base de datos SQLite en una carpeta propia dentro de los datos locales del usuario de Windows. Esto permitirá que los datos permanezcan separados de los archivos del programa y no se pierdan al realizar una actualización normal. Ya llevamos 15 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T16 — ¿Cómo protegeremos los respaldos de SubTrack?

**Planteamiento y opciones (fuente desde línea 2382):** Ya aprobamos que SubTrack realizará respaldos automáticos y permitirá crear copias manualmente. Ahora debemos decidir cómo administrar esas copias para que no ocupen espacio indefinidamente y puedas recuperar información cuando sea necesario. Ejemplo de respaldos disponibles Respaldo de hoy 20 de octubre de 2026 Reciente Respaldo anterior

**Evidencia de aprobación de GRIM (línea 2422):** Seguimos exclusivamente en planificación documental. No necesitas tu PC y ningún agente tiene autorización para implementar ni ejecutar comandos Git, incluidos los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T16 sobre la conservación de respaldos automáticos y manuales. Continuemos con la siguiente decisión técnica con explicaciones 

**Regla/formulación registrada (línea 2425):** F0-T16 aprobada — Conservación de respaldos SubTrack 1.0 conservará los 7 respaldos automáticos más recientes, generando como máximo uno por día al iniciar la aplicación. Los respaldos manuales no se eliminarán automáticamente. Las copias deberán ser consistentes y validarse antes de restaurarlas. Ya llevamos 16 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T17 — ¿Qué sucede cuando restauramos un respaldo?

**Planteamiento y opciones (fuente desde línea 2433):** Imagina que tienes información registrada hasta el 20 de octubre, pero necesitas recuperar una copia del 18 de octubre. Restaurar esa copia significa que la base de datos volverá al estado que tenía el día 18. Por eso necesitamos proteger la información actual antes de reemplazarla. Ejemplo de restauración segura 1. Seleccionas el respaldo que deseas recuperar. 2. SubTrack comprueba que el archivo sea válido y compatible. 3. Te advierte que reemplazará la información actual y solicita confirmaci

**Evidencia de aprobación de GRIM (línea 2465):** Seguimos únicamente en planificación documental. No necesitas tu PC. ZYKO, COSMO y GRIM no están autorizados a implementar; Codex, Antigravity y OpenCode con OmniRoute no pueden ejecutar comandos Git de ningún tipo, ni siquiera de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T17 sobre restauración guiada y protegida de respaldos. Continu

**Regla/formulación registrada (línea 2467):** F0-T17 aprobada — Restauración guiada y protegida SubTrack 1.0 deberá validar el respaldo, solicitar tu confirmación y crear una copia de seguridad de la base actual antes de restaurar. Si ocurre un error, protegerá la información original y evitará dejar una base de datos parcialmente restaurada. Ya llevamos 17 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T18 — ¿Qué haremos si SQLite detecta datos dañados?

**Planteamiento y opciones (fuente desde línea 2475):** Imagina que SubTrack intenta abrir su base de datos y descubre que algunos registros están dañados. Esto podría ocurrir por problemas de almacenamiento, apagados inesperados o daños en el archivo. Lo importante es que SubTrack no empeore la situación intentando reparar o reemplazar los datos sin tu permiso. Ejemplo: problema detectado No se puede garantizar la integridad de la base de datos. Por seguridad, SubTrack ha bloqueado las operaciones de escritura. El archivo dañado se conserva. No se e

**Evidencia de aprobación de GRIM (línea 2511):** Seguimos únicamente en planificación documental. No necesitas tu PC. ZYKO, COSMO y GRIM no están autorizados a implementar; Codex, Antigravity y OpenCode con OmniRoute no pueden ejecutar comandos Git de ningún tipo, ni siquiera de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T18 sobre el modo de recuperación ante corrupción de SQLite. Co

**Regla/formulación registrada (línea 2513):** F0-T18 aprobada — Modo de recuperación seguro Si SubTrack detecta corrupción en SQLite: - Bloqueará todas las operaciones financieras. - Conservará intacto el archivo dañado. - No mostrará saldos que podrían ser incorrectos. - Permitirá revisar respaldos válidos y realizar una restauración protegida. - Nunca reemplazará la base de datos sin tu autorización. Ya llevamos 18 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T19 — ¿Cómo controlaremos las actualizaciones de la base de datos?

**Planteamiento y opciones (fuente desde línea 2525):** Imagina que instalas SubTrack 1.0 y comienzas a registrar tus finanzas. Meses después, instalas una versión nueva que necesita guardar información adicional. Queremos que puedas actualizar el programa sin perder tus suscripciones, pagos, ingresos ni gastos anteriores. Ejemplo de actualización Versión instalada Versión futura

**Evidencia de aprobación de GRIM (línea 2565):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. ZYKO, COSMO y GRIM no están autorizados a implementar; Codex, Antigravity y OpenCode con OmniRoute no pueden ejecutar ningún comando Git, incluidos los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T19 sobre migraciones versionadas de SQLite. Continuemos con la si

**Regla/formulación registrada (línea 2567):** F0-T19 aprobada — Migraciones versionadas de SQLite Queda establecido que SubTrack 1.0 utilizará migraciones controladas para actualizar la estructura de su base de datos. Antes de aplicar una migración, deberá verificar la versión actual y crear un respaldo. Si ocurre un error, impedirá trabajar con una base inconsistente y ofrecerá recuperación. Avance: 19 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T20 — ¿Qué pasa si intentamos abrir SubTrack dos veces?

**Planteamiento y opciones (fuente desde línea 2575):** Imagina que tienes SubTrack abierto y, sin darte cuenta, haces doble clic nuevamente en su icono. Podrían quedar dos ventanas intentando registrar pagos o modificar la misma base de datos. Aunque SQLite dispone de mecanismos de bloqueo, debemos decidir cómo manejará SubTrack esta situación. Ejemplo sencillo SubTrack 1 Abierto SubTrack 2

**Evidencia de aprobación de GRIM (línea 2609):** Seguimos únicamente en planificación documental. No necesitas tu PC. Ningún agente está autorizado a implementar; Codex, Antigravity y OpenCode con OmniRoute tienen prohibido ejecutar cualquier comando Git, incluso de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T20 sobre permitir una sola instancia o varias instancias de la aplicación. 

**Regla/formulación registrada (línea 2611):** F0-T20 aprobada — Una sola instancia de SubTrack SubTrack 1.0 permitirá una sola instancia activa por usuario de Windows. Si intentas abrirlo nuevamente, el programa buscará la ventana existente y tratará de mostrarla, en lugar de iniciar otra instancia. Si no puede hacerlo, mostrará un aviso claro. Avance: 20 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T21 — ¿Qué sucede si una operación tarda demasiado?

**Planteamiento y opciones (fuente desde línea 2619):** Imagina que presionas Registrar pago y SQLite tarda unos segundos en responder. Durante ese tiempo, necesitamos evitar que la aplicación parezca congelada o que registres el mismo pago varias veces. Ejemplo: pago de Netflix por $12.99 Presionas «Registrar pago». SubTrack muestra «Guardando pago…». El botón permanece deshabilitado hasta conocer el resultado. Si ocurre un error, SubTrack comprobará si el pago quedó registrado antes de permitir reintentarlo, respetando las protecciones contra dupli

**Evidencia de aprobación de GRIM (línea 2649):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. Ningún agente tiene autorización para implementar, y Codex, Antigravity y OpenCode con OmniRoute tienen prohibido ejecutar cualquier comando Git, incluidos los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T21 sobre el comportamiento de la interfaz durante operaci

**Regla/formulación registrada (línea 2651):** F0-T21 aprobada — Interfaz responsiva durante operaciones financieras SubTrack 1.0 mantendrá la ventana funcionando mientras registra pagos, ingresos o gastos. Durante el guardado, mostrará un indicador de progreso, impedirá repetir la misma operación y notificará si terminó correctamente o si ocurrió un error. Esto respeta nuestra arquitectura asíncrona y la protección contra registros duplicados. Avance: 21 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T22 — ¿Cómo mostraremos los errores al usuario?

**Planteamiento y opciones (fuente desde línea 2661):** Imagina que intentas registrar un pago de $15, pero ocurre un problema. No queremos que SubTrack muestre mensajes técnicos difíciles de comprender, como nombres de tablas, excepciones o detalles internos de SQLite. Queremos que te explique qué ocurrió y qué puedes hacer. Ejemplo: pago ya registrado Mensaje técnico SQLite constraint failed: UNIQUE constraint Mensaje comprensible

**Evidencia de aprobación de GRIM (línea 2693):** Seguimos únicamente en planificación documental, sin necesidad de utilizar tu PC. Ningún agente está autorizado a implementar. Codex, Antigravity y OpenCode con OmniRoute tienen prohibido ejecutar comandos Git de cualquier tipo, incluso los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T22 sobre mensajes de error comprensibles y diagnó

**Regla/formulación registrada (línea 2695):** F0-T22 aprobada — Mensajes claros y diagnóstico separado SubTrack 1.0 mostrará errores fáciles de comprender, indicando qué ocurrió y qué puede hacer el usuario. Los detalles técnicos quedarán separados para diagnóstico, evitando mostrar información interna de SQLite o datos sensibles. Avance: 22 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T23 — ¿Cómo manejaremos los registros de errores?

**Planteamiento y opciones (fuente desde línea 2703):** Ya decidimos cómo mostrar los errores. Ahora necesitamos establecer cómo guardaremos información de esos problemas para poder investigarlos. Imagina que SubTrack se cierra inesperadamente. Al abrirlo nuevamente, sería útil contar con un registro que permita saber qué falló, sin comprometer tus datos financieros. Ejemplo de registro técnico 20/10/2026 — 2:30 p. m. Error al acceder a la base de datos No se guardaron importes, nombres ni datos financieros personales. Este registro sirve para invest

**Evidencia de aprobación de GRIM (línea 2735):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. Ningún agente tiene autorización para implementar y Codex, Antigravity y OpenCode con OmniRoute tienen prohibido ejecutar cualquier comando Git, incluso de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T23 sobre registros técnicos de errores y su conservación. Contin

**Regla/formulación registrada (línea 2738):** F0-T23 aprobada — Registros técnicos locales Queda establecido que SubTrack 1.0: - Guardará errores técnicos localmente, separados de la base financiera. - Conservará los registros durante un máximo de 30 días. - Eliminará automáticamente los registros antiguos. - No enviará diagnósticos por Internet ni registrará información financiera sensible. Avance: 23 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T24 — ¿Cómo evitaremos que se mezclen operaciones al cambiar de pantalla?

**Planteamiento y opciones (fuente desde línea 2749):** Imagina que estás registrando un gasto de $35 y, mientras SubTrack lo guarda, decides cambiar al Dashboard. ¿Qué debería hacer el programa? Ejemplo práctico Registrar gasto: $35 La operación está procesándose. Guardando Cambiar al Dashboard

**Evidencia de aprobación de GRIM (línea 2785):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. Ningún agente tiene autorización para implementar; Codex, Antigravity y OpenCode con OmniRoute tienen prohibido ejecutar cualquier comando Git, incluidos los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T24 sobre navegación durante operaciones financieras en proc

**Regla/formulación registrada (línea 2787):** F0-T24 aprobada — Navegación durante operaciones financieras SubTrack 1.0 permitirá cambiar de pantalla mientras termina de guardar una operación. El guardado continuará de forma controlada, sin duplicaciones. Cuando finalice, SubTrack notificará el resultado y actualizará el Dashboard. Si existe incertidumbre, comprobará el estado de la operación antes de permitir reintentarla. Avance: 24 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T25 — ¿Cómo evitaremos mostrar saldos mezclados o desactualizados?

**Planteamiento y opciones (fuente desde línea 2795):** Imagina que SubTrack está actualizando el Dashboard después de registrar un pago. El sistema necesita consultar el saldo, los ingresos, los gastos y las suscripciones pendientes. Queremos evitar que algunos indicadores correspondan a un momento y otros a uno diferente. Ejemplo práctico Saldo antes del pago # $500.00 Saldo después del pago

**Evidencia de aprobación de GRIM (línea 2831):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. Ningún agente está autorizado a implementar y Codex, Antigravity y OpenCode con OmniRoute tienen prohibido ejecutar cualquier comando Git, incluso de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T25 sobre consistencia de los indicadores del Dashboard. Continuemos co

**Regla/formulación registrada (línea 2834):** F0-T25 aprobada — Consistencia del Dashboard Queda establecido que SubTrack 1.0 mostrará los indicadores financieros utilizando una misma vista coherente de los datos. El saldo, los ingresos, los gastos y las obligaciones se actualizarán juntos, evitando mostrar cifras de momentos diferentes. Avance: 25 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T26 — ¿Cómo funcionarán las alertas de pagos próximos a vencer?

**Planteamiento y opciones (fuente desde línea 2842):** Ya aprobamos que el Dashboard mostrará las obligaciones próximas a vencer, con filtros de 7, 15 y 30 días. Ahora debemos decidir cómo avisará SubTrack cuando una suscripción esté cerca de su fecha de pago. Ejemplo de alertas Netflix — $12.99 Vence dentro de 3 días Próximo Spotify — $6.99

**Evidencia de aprobación de GRIM (línea 2884):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. Ningún agente tiene autorización para implementar y Codex, Antigravity y OpenCode con OmniRoute tienen prohibido ejecutar cualquier comando Git, incluidos los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T26 sobre alertas de suscripciones próximas a vencer. Conti

**Regla/formulación registrada (línea 2887):** F0-T26 aprobada — Alertas internas de vencimientos SubTrack 1.0 mostrará dentro de la aplicación las suscripciones próximas a vencer y los pagos atrasados. Las alertas se actualizarán al consultar la información y después de registrar un pago. No necesitaremos notificaciones de Windows ni procesos en segundo plano. Avance: 26 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T27 — ¿Qué ocurrirá si cierras SubTrack mientras guarda un pago?

**Planteamiento y opciones (fuente desde línea 2895):** Imagina que registras un pago de $12.99 y, justo mientras se está guardando, presionas la X para cerrar la aplicación. Necesitamos evitar que el programa se cierre de forma desordenada y deje dudas sobre si el pago se registró. Ejemplo práctico Pago de Netflix $12.99 — Guardando operación En proceso Hay una operación financiera en curso

**Evidencia de aprobación de GRIM (línea 2929):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. Ningún agente tiene autorización para implementar y Codex, Antigravity y OpenCode con OmniRoute tienen prohibido ejecutar cualquier comando Git, incluso de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T27 sobre cierre seguro de la aplicación durante operaciones fina

**Regla/formulación registrada (línea 2931):** F0-T27 aprobada — Cierre seguro de SubTrack Queda establecido que SubTrack 1.0: - Esperará a que las operaciones financieras terminen de forma segura antes de cerrarse. - Mostrará un aviso si hay un pago, ingreso o gasto en proceso. - No aceptará nuevas operaciones mientras se está cerrando. - Comprobará las operaciones inciertas al volver a iniciarse. Avance: 27 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T28 — ¿Qué haremos si una consulta tarda demasiado?

**Planteamiento y opciones (fuente desde línea 2942):** Imagina que llevas varios meses utilizando SubTrack y tienes cientos de pagos, ingresos y gastos registrados. Cuando abres el historial, el programa necesita consultar esa información. Queremos que la aplicación siga respondiendo, incluso si la consulta tarda más de lo habitual. Ejemplo: consultar historial de gastos Historial de gastos Consultando registros de SQLite… Consultando

**Evidencia de aprobación de GRIM (línea 2976):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. Ningún agente está autorizado a implementar y Codex, Antigravity y OpenCode con OmniRoute tienen prohibido ejecutar cualquier comando Git, incluso los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T28 sobre cancelación de consultas y control de resultados desactua

**Regla/formulación registrada (línea 2979):** F0-T28 aprobada — Consultas cancelables y resultados controlados SubTrack 1.0 permitirá cancelar consultas de lectura que ya no sean necesarias y descartará resultados antiguos cuando el usuario cambie de pantalla. Importante: esta cancelación no interrumpirá pagos, ingresos ni gastos que ya estén guardándose. Esas operaciones seguirán sus reglas de seguridad. Avance: 28 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T29 — ¿Cómo mostraremos los historiales cuando tengan muchos registros?

**Planteamiento y opciones (fuente desde línea 2987):** Imagina que después de un año tienes 1,500 movimientos financieros entre pagos, ingresos y gastos. Si SubTrack intenta mostrar los 1,500 registros al mismo tiempo, podría tardar innecesariamente. Necesitamos decidir cómo presentar grandes cantidades de información sin volver lenta la aplicación. Ejemplo: historial de gastos Historial de gastos 1,500 registros Internet

**Evidencia de aprobación de GRIM (línea 3035):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. Ningún agente tiene autorización para implementar y Codex, Antigravity y OpenCode con OmniRoute tienen prohibido ejecutar cualquier comando Git, incluso los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T29 sobre paginación y filtros de historiales financieros. Co

**Regla/formulación registrada (línea 3038):** F0-T29 aprobada — Historiales paginados con filtros Queda establecido que SubTrack 1.0 mostrará inicialmente 50 registros por página, ordenados del más reciente al más antiguo. Permitirá filtrar los movimientos por fecha, categoría o estado, según corresponda. SQLite consultará únicamente los registros necesarios para mantener la aplicación rápida. Avance: 29 decisiones técnicas aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T30 — ¿Cómo evitaremos errores al manejar dinero?

**Planteamiento y opciones (fuente desde línea 3046):** Imagina que SubTrack debe sumar ingresos, restar gastos y calcular cuánto dinero te queda disponible. Aunque parezca sencillo, las computadoras pueden producir pequeñas diferencias al realizar ciertos cálculos con números decimales. En una aplicación financiera, debemos evitar esos errores. Ejemplo: cálculo de saldo Saldo inicial Pago de suscripción

**Evidencia de aprobación de GRIM (línea 3090):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. Ningún agente está autorizado a implementar y Codex, Antigravity y OpenCode con OmniRoute tienen prohibido ejecutar cualquier comando Git, incluidos los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T30 sobre precisión monetaria y almacenamiento de importes en SQL

**Regla/formulación registrada (línea 3092):** F0-T30 aprobada — Precisión monetaria SubTrack 1.0 utilizará cálculos decimales exactos y guardará los importes en SQLite como centavos enteros. Por ejemplo, $12.99 se almacenará como 1299 centavos. La aplicación trabajará con dólares estadounidenses y dos decimales, evitando errores de precisión. Avance de planificación 30 decisiones técnicas 30 decisiones funcionales y 30 decisiones técnicas aprobadas. Esto no significa que la implementación o las pruebas estén terminadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T31 — ¿Cómo controlaremos las fechas de pagos y vencimientos?

**Planteamiento y opciones (fuente desde línea 3104):** Imagina que una suscripción vence el 31 de enero y se repite mensualmente. Febrero no tiene 31 días. Por eso debemos definir cómo calculará SubTrack el siguiente vencimiento sin generar fechas incorrectas. Ejemplo: suscripción mensual de $10 Enero # 31 Vence Febrero

**Evidencia de aprobación de GRIM (línea 3148):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. Ningún agente está autorizado a implementar y Codex, Antigravity y OpenCode con OmniRoute tienen prohibido ejecutar cualquier comando Git, incluidos los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T31 sobre cálculo de vencimientos mensuales en meses cortos. Cont

**Regla/formulación registrada (línea 3150):** F0-T31 aprobada — Vencimientos en meses cortos SubTrack 1.0 conservará el día original de vencimiento de cada suscripción. Si un mes no tiene ese día, utilizará el último día disponible, sin modificar permanentemente la programación. Por ejemplo: 31 de enero → 28 de febrero → 31 de marzo, en un año no bisiesto. Avance: 31 decisiones técnicas y 30 decisiones funcionales aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T32 — ¿Qué pasará cuando cambie la fecha de Windows?

**Planteamiento y opciones (fuente desde línea 3160):** Imagina que registras un pago hoy, pero después descubres que el reloj de Windows estaba configurado con una fecha incorrecta. Esto podría afectar los vencimientos, los pagos atrasados y las proyecciones financieras. Necesitamos definir cómo utilizará SubTrack la fecha del equipo. Ejemplo práctico Fecha de Windows Fecha financiera

**Evidencia de aprobación de GRIM (línea 3198):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. Ningún agente está autorizado a implementar y Codex, Antigravity y OpenCode con OmniRoute tienen prohibido ejecutar cualquier comando Git, incluso de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T32 sobre fecha de Windows, fechas financieras y actualización de alert

**Regla/formulación registrada (línea 3201):** F0-T32 aprobada — Control de fechas de Windows SubTrack 1.0 utilizará la fecha local de Windows como referencia para vencimientos y alertas, pero permitirá registrar operaciones con su fecha financiera real. Además, conservará por separado la fecha técnica del registro y actualizará las alertas si detecta cambios importantes en el reloj del equipo. Avance: 32 decisiones técnicas y 30 decisiones funcionales aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T33 — ¿Qué sucederá si intentas guardar información incorrecta?

**Planteamiento y opciones (fuente desde línea 3209):** Imagina que estás registrando una suscripción y accidentalmente escribes un importe negativo o dejas vacío el nombre. Necesitamos definir dónde se comprobarán los datos antes de guardarlos. Ejemplo: nueva suscripción Nombre Sin nombre Debes escribir un nombre. Importe mensual

**Evidencia de aprobación de GRIM (línea 3247):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. Ningún agente está autorizado a implementar y Codex, Antigravity y OpenCode con OmniRoute tienen prohibido ejecutar cualquier comando Git, incluso de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T33 sobre validación de datos en WinForms, Core y SQLite. Continuemos c

**Regla/formulación registrada (línea 3249):** F0-T33 aprobada — Validación de datos en tres niveles SubTrack 1.0 comprobará la información antes de guardarla: - GRIM (WinForms): mostrará errores fáciles de comprender. - ZYKO (Core): verificará las reglas financieras. - COSMO (SQLite): protegerá la integridad de los datos almacenados. Así evitaremos registrar importes inválidos, información incompleta y operaciones duplicadas. Avance: 33 decisiones técnicas y 30 decisiones funcionales aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T34 — ¿Cómo organizaremos las pruebas de SubTrack?

**Planteamiento y opciones (fuente desde línea 3261):** Ya definimos muchas reglas para pagos, ingresos, gastos, suscripciones y respaldos. Ahora necesitamos decidir cómo comprobaremos que esas reglas funcionan correctamente antes de entregar el programa. Ejemplo: comprobar un pago de $15 Prueba de cálculo Si tienes $100 y pagas $15, el saldo debe quedar en $85. Prueba de almacenamiento El pago debe guardarse una sola vez y conservar su comprobante.

**Evidencia de aprobación de GRIM (línea 3297):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. Ningún agente está autorizado a implementar y Codex, Antigravity y OpenCode con OmniRoute tienen prohibido ejecutar comandos Git de ningún tipo, incluidos los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T34 sobre pruebas por capas y validación final integrada. C

**Regla/formulación registrada (línea 3299):** F0-T34 aprobada — Pruebas por capas y validación integrada SubTrack 1.0 tendrá una estrategia de pruebas dividida por responsabilidades: - ZYKO: comprobará los cálculos y las reglas financieras. - COSMO: verificará el almacenamiento, las transacciones y la integridad de SQLite. - GRIM: comprobará la interfaz, navegación y mensajes. - Equipo completo: validará que las tres partes funcionen correctamente juntas, utilizando datos de prueba. Estas pruebas se realizarán únicamente cuando autorices la fase correspondiente. Avance: 34 decisiones técnicas y 30 decisiones funcionales aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T35 — ¿Cómo comprobaremos que SubTrack está listo para entregarse?

**Planteamiento y opciones (fuente desde línea 3312):** Imagina que ZYKO termina Core, COSMO termina SQLite y GRIM finaliza la interfaz. Cada parte puede funcionar correctamente por separado, pero eso no garantiza que todo el programa esté listo para entregarse. Necesitamos establecer una revisión final antes de declarar terminada la versión 1.0. Ejemplo de revisión final Cálculos financieros Saldos, ingresos, gastos y proyecciones correctos. Integridad de los datos

**Evidencia de aprobación de GRIM (línea 3354):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. Ningún agente tiene autorización para implementar y Codex, Antigravity y OpenCode con OmniRoute tienen prohibido ejecutar cualquier comando Git, incluso los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T35 sobre criterios obligatorios de aprobación antes de entre

**Regla/formulación registrada (línea 3356):** F0-T35 aprobada — Criterios obligatorios de entrega SubTrack 1.0 solamente se considerará terminado cuando sus funciones comprometidas estén completas, las pruebas críticas hayan pasado y no existan errores graves conocidos. El equipo preparará un informe de validación y la aprobación final de la entrega será tuya. Avance: 35 decisiones técnicas y 30 decisiones funcionales aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T36 — ¿Cómo organizaremos los errores encontrados durante las pruebas?

**Planteamiento y opciones (fuente desde línea 3364):** Imagina que durante la revisión final descubrimos tres problemas: Un pago se registra dos veces Crítico Puede alterar el saldo y los comprobantes. El historial no muestra algunos registros Alto Afecta una función importante de consulta.

**Evidencia de aprobación de GRIM (línea 3404):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. Ningún agente está autorizado a implementar y Codex, Antigravity y OpenCode con OmniRoute tienen prohibido ejecutar cualquier comando Git, incluso los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T36 sobre clasificación de errores y control de entrega. Continuemo

**Regla/formulación registrada (línea 3406):** F0-T36 aprobada — Clasificación de errores y control de entrega SubTrack 1.0 clasificará los errores en cuatro niveles: crítico, alto, medio y menor. Los errores críticos y altos deberán resolverse antes de aprobar la entrega. Los medios y menores podrán evaluarse individualmente, pero cualquier pendiente aceptado deberá quedar documentado y contar con tu aprobación. Avance: 36 decisiones técnicas y 30 decisiones funcionales aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T37 — ¿Cómo sabremos si una corrección dañó algo que ya funcionaba?

**Planteamiento y opciones (fuente desde línea 3414):** Imagina que COSMO corrige un problema de SQLite relacionado con los pagos. El error desaparece, pero accidentalmente esa modificación afecta la generación de comprobantes. Esto puede suceder en cualquier proyecto. Por eso necesitamos decidir cómo comprobaremos que una corrección no provoque nuevos errores en otras funciones. Ejemplo: corregimos un problema de pagos Corrección realizada Se corrige un problema que impedía registrar ciertos pagos. Comprobaciones necesarias

**Evidencia de aprobación de GRIM (línea 3452):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. Ningún agente tiene autorización para implementar. Codex, Antigravity y OpenCode con OmniRoute tienen prohibido ejecutar cualquier comando Git, incluso los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T37 sobre pruebas de regresión después de corregir errores. Co

**Regla/formulación registrada (línea 3454):** F0-T37 aprobada — Pruebas de regresión Queda establecido que, después de corregir un error en SubTrack 1.0, se comprobará que el problema desapareció y que las funciones relacionadas continúan funcionando correctamente. Antes de aprobar la entrega, también se repetirán las pruebas críticas del sistema. Avance: 37 decisiones técnicas y 30 decisiones funcionales aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T38 — ¿Cómo sabremos quién debe corregir cada error?

**Planteamiento y opciones (fuente desde línea 3462):** Imagina que durante las pruebas encontramos un problema: registras un pago de $20, pero el Dashboard muestra un saldo incorrecto. El error podría encontrarse en el cálculo, en SQLite o en la interfaz. Necesitamos evitar que los tres integrantes modifiquen distintas partes sin saber dónde está realmente el problema. Ejemplo: saldo incorrecto después de pagar Saldo anterior $100.00 Pago registrado −$20.00

**Evidencia de aprobación de GRIM (línea 3512):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. Ningún agente está autorizado a implementar y Codex, Antigravity y OpenCode con OmniRoute tienen prohibido ejecutar comandos Git de ningún tipo, incluidos los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T38 sobre diagnóstico, documentación y asignación de errore

**Regla/formulación registrada (línea 3514):** F0-T38 aprobada — Diagnóstico y asignación de errores Queda establecido que cada error detectado en SubTrack 1.0 tendrá una descripción, gravedad, pasos para reproducirlo, resultado esperado y responsable asignado. ZYKO atenderá los problemas de Core, COSMO los relacionados con SQLite y GRIM los de la interfaz. Cuando un problema afecte varias áreas, se coordinará entre los responsables. Avance: 38 decisiones técnicas y 30 decisiones funcionales aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T39 — ¿Cómo organizaremos la integración de las tres partes?

**Planteamiento y opciones (fuente desde línea 3522):** Imagina que ZYKO termina una función de cálculo, COSMO prepara su almacenamiento y GRIM diseña la pantalla correspondiente. Aunque cada parte funcione individualmente, necesitamos comprobar que se comunican correctamente entre sí. Ejemplo: registrar un pago de $15 GRIM — WinForms El usuario presiona «Registrar pago». ZYKO — Core Comprueba que el pago sea válido.

**Evidencia de aprobación de GRIM (línea 3560):** Seguimos exclusivamente en planificación documental. No necesitas tu PC. Ningún agente tiene autorización para implementar y Codex, Antigravity y OpenCode con OmniRoute tienen prohibido ejecutar cualquier comando Git, incluidos los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T39 sobre integración gradual de WinForms, Core y SQLite me

**Regla/formulación registrada (línea 3563):** F0-T39 aprobada — Integración gradual con contratos definidos Queda establecido que SubTrack 1.0 integrará las funciones de WinForms, Core y SQLite progresivamente, comprobando que cada una funcione correctamente antes de continuar. Los contratos definirán qué información recibe y devuelve cada componente. Cualquier cambio que afecte otras áreas deberá coordinarse y aprobarse previamente. Avance: 39 decisiones técnicas y 30 decisiones funcionales aprobadas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T40 — ¿Cómo controlaremos los cambios de los contratos entre integrantes?

**Planteamiento y opciones (fuente desde línea 3571):** Imagina que GRIM ya tiene preparada la pantalla para registrar pagos. La pantalla espera recibir el importe, la fecha y el número de comprobante. Pero ZYKO decide modificar la información que devuelve Core. Aunque el cambio sea correcto, la pantalla podría dejar de funcionar si GRIM no conoce la modificación. Ejemplo de comunicación entre componentes GRIM Necesita datos del pago ZYKO

**Evidencia de aprobación de GRIM (línea 3635):** Todavía no autorizaremos implementación ni comandos Git a ningún agente. Para SubTrack 1.0, apruebo la opción A para F0-T40 sobre control de cambios de contratos entre WinForms, Core y SQLite. Continuemos con la siguiente decisión técnica usando explicaciones sencillas y sin mostrar código. No autorizo implementación ni comandos Git a ningún agente

**Regla/formulación registrada (línea 3638):** F0-T40 aprobada — Control de cambios de contratos Queda establecido que cualquier cambio en los contratos entre WinForms, Core y SQLite deberá documentarse, evaluarse y aprobarse antes de implementarse. Avance: 40 de 45 decisiones técnicas propuestas. Solo faltan 5.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T41 — ¿Cómo instalaremos SubTrack 1.0 en Windows?

**Planteamiento y opciones (fuente desde línea 3644):** Imagina que terminamos el proyecto y quieres instalarlo en otra computadora. No queremos que tengas que instalar Visual Studio, configurar .NET manualmente o copiar archivos técnicos uno por uno. Ejemplo de entrega SubTrack 1.0 para Windows Paquete preparado para ejecutar la aplicación en otra PC. Abres SubTrack sin necesitar Visual Studio. La base de datos se crea en la carpeta local del usuario.

**Evidencia de aprobación de GRIM (línea 3678):** Seguimos exclusivamente en planificación. No necesitas tu PC y ningún agente está autorizado a implementar ni ejecutar comandos Git de ningún tipo, incluidos los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T41 sobre distribución e instalación de la aplicación en Windows. Continuemos con F0-T42, manteniendo el objetivo de cerrar en F0

**Regla/formulación registrada (línea 3681):** F0-T41 aprobada — Distribución autónoma para Windows SubTrack 1.0 se preparará para Windows de 64 bits, incluyendo los componentes necesarios de .NET, sin exigir que el usuario instale Visual Studio ni .NET por separado. También se verificará su funcionamiento en una computadora limpia y se protegerán los datos personales durante futuras actualizaciones. Planificación técnica 41 de 45 30 decisiones funcionales completas. Faltan 4 decisiones técnicas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T42 — ¿Cómo actualizaremos SubTrack sin perder información?

**Planteamiento y opciones (fuente desde línea 3693):** Imagina que entregamos SubTrack 1.0 y, posteriormente, corregimos un error y publicamos la versión 1.0.1. La pregunta es: ¿cómo instalamos la nueva versión sin perder suscripciones, pagos, ingresos y gastos? Ejemplo práctico VERSIÓN ANTERIOR 12 suscripciones 35 pagos

**Evidencia de aprobación de GRIM (línea 3735):** Seguimos exclusivamente en planificación documental. No se autoriza implementación ni comandos Git a Codex, Antigravity, OpenCode con OmniRoute ni a ningún otro agente. Para SubTrack 1.0, apruebo la opción A para F0-T42 sobre actualización del programa y protección de datos financieros. Continuemos con F0-T43, manteniendo el cierre en F0-T45. Sin m

**Regla/formulación registrada (línea 3738):** F0-T42 aprobada — Actualizaciones manuales y protegidas Queda establecido que SubTrack 1.0 se actualizará manualmente, conservando la base de datos del usuario y verificando su compatibilidad. Antes de modificar su estructura, se realizará una copia de seguridad. Si la actualización presenta riesgos, se detendrá. Planificación técnica 42 de 45 30 decisiones funcionales aprobadas. Solo faltan 3 decisiones técnicas.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T43 — ¿Cómo comprobaremos que SubTrack funciona en otra computadora?

**Planteamiento y opciones (fuente desde línea 3748):** Imagina que GRIM termina la interfaz, ZYKO finaliza Core y COSMO completa SQLite. En nuestras computadoras todo funciona correctamente. Sin embargo, cuando el docente abre SubTrack en otra PC, el programa podría fallar por archivos faltantes, permisos o diferencias de configuración. Necesitamos decidir cómo verificar la entrega antes de presentarla. Ejemplo: prueba en una computadora limpia 1\. Abrir el paquete Sin Visual Studio ni instalaciones manuales de .NET. 2\. Crear y conservar datos

**Evidencia de aprobación de GRIM (línea 3786):** Seguimos en planificación documental. No se autoriza implementación ni ningún comando Git a Codex, Antigravity, OpenCode con OmniRoute u otros agentes. Para SubTrack 1.0, apruebo la opción A para F0-T43 sobre validación final del paquete en un entorno Windows limpio. Continuemos con F0-T44 y mantengamos el cierre en F0-T45. Sin código, implementaci

**Regla/formulación registrada (línea 3788):** F0-T43 aprobada — Validación en Windows limpio Queda establecido que, antes de entregar SubTrack 1.0, se probará el paquete definitivo en un entorno Windows limpio, sin herramientas de desarrollo. Se comprobarán el inicio del programa, las operaciones financieras, la conservación de datos, los respaldos, la restauración y la reapertura. Los resultados quedarán documentados. Planificación técnica 43 de 45 30 decisiones funcionales aprobadas. Faltan únicamente F0-T44 y F0-T45.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T44 — ¿Cómo organizaremos la entrega final del proyecto?

**Planteamiento y opciones (fuente desde línea 3800):** Imagina que llega el día de presentar SubTrack 1.0. El programa funciona, pero necesitamos identificar cuál es la versión definitiva, qué documentación la acompaña y qué pruebas demuestran que está lista. La idea es evitar entregar archivos incompletos o una versión distinta de la que probamos. Ejemplo de paquete de entrega SubTrack 1.0 — Entrega final Aplicación Paquete Windows definitivo y probado.

**Evidencia de aprobación de GRIM (línea 3840):** Seguimos exclusivamente en planificación documental. Ningún agente está autorizado a implementar ni ejecutar comandos Git de ningún tipo, incluidos los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T44 sobre organización y documentación de la entrega final. Continuemos con la última decisión técnica F0-T45, sin agregar más decisiones. 

**Regla/formulación registrada (línea 3842):** F0-T44 aprobada — Entrega formal y organizada Queda establecido que SubTrack 1.0 tendrá una única versión definitiva, acompañada de manual de usuario, instrucciones de instalación, evidencias de pruebas y registro de limitaciones conocidas. El paquete deberá revisarse y aprobarse antes de su entrega. Planificación técnica 44 de 45 30 decisiones funcionales completas. Solo falta una decisión técnica.

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

### F0-T45 — ¿Cómo evitaremos agregar funciones nuevas antes de la entrega?

**Planteamiento y opciones (fuente desde línea 3854):** Esta será nuestra última decisión técnica. Imagina que faltan pocos días para presentar SubTrack 1.0 y alguien propone agregar gráficos avanzados, sincronización en la nube o notificaciones de Windows. Son ideas interesantes, pero podrían provocar errores y retrasar la entrega del 2 de noviembre de 2026. Necesitamos establecer qué haremos con esas propuestas. Ejemplo: aparece una idea nueva Propuesta: sincronizar datos por internet SubTrack 1.0

**Evidencia de aprobación de GRIM (línea 3892):** Seguimos únicamente en planificación documental. Ningún agente tiene autorización para implementar ni ejecutar comandos Git, incluidos los de lectura. Para SubTrack 1.0, apruebo la opción A para F0-T45 sobre congelación del alcance. Damos por cerradas las 30 decisiones funcionales y 45 decisiones técnicas, sin agregar más decisiones. El siguiente p

**Regla/formulación registrada (línea 3894):** F0-T45 aprobada — Congelación del alcance Con esta aprobación, cerramos oficialmente la etapa de decisiones de SubTrack 1.0. # 30/30 Funcionales # 45/45 Técnicas # 75/75

**Dependencia:** GRIM/ZYKO/COSMO según el ámbito; contratos concretos pendientes de aprobación.

## 11. Aclaraciones, contradicciones y verificaciones humanas

- **F0-04:** La aprobación literal aparece en línea 184, pero no con encabezado uniforme «F0-04 aprobada». Confirmar que la formulación posterior no amplía lo ratificado.
- **F0-06:** Aclaración posterior en líneas 418–451: quincenal significa días 15 y último día del mes, no intervalos de 15 días; confirmar que toda implementación respeta la aclaración.
- **F0-10:** La aprobación literal aparece en línea 416 sin encabezado uniforme «F0-10 aprobada». El detalle de cancelación de cobros futuros debe verificarse con el texto aprobado.
- **F0-12:** Se aprobó opción A (fecha real elegida por usuario), aunque la recomendación previa favorecía opción C. No sustituir la aprobación por la recomendación.
- **F0-T40:** La conversación incluye una etapa donde se menciona pendiente antes de la aprobación final; usar la aprobación posterior en línea 3638.
- **F0-T45:** Cierra el alcance funcional/técnico; no convierte automáticamente en aprobados los contratos C# concretos.

## 12. Matriz de trazabilidad operativa

La tabla de la sección 8 cubre los 75 identificadores y sus ubicaciones. Para cada decisión, las secciones 9 y 10 enlazan la formulación de aprobación con el planteamiento y el extracto de la fuente. La matriz no constituye una auditoría del código fuente.

## 13. Procedimiento de aprobación por GRIM

1. Revisar cada uno de los 75 registros contra las líneas indicadas en la fuente original.
2. Resolver por escrito las aclaraciones y ambigüedades, sin introducir decisiones nuevas en el conjunto congelado.
3. Confirmar que los tres formatos contienen el mismo contenido sustantivo.
4. Registrar aprobación explícita de GRIM, versión y fecha.
5. Solo entonces incorporar manualmente el Markdown a `docs/SUBTRACK-1.0-D1-DOCUMENTO-MAESTRO-75-DECISIONES.md`.
6. Realizar Git exclusivamente por GRIM; durante D1 no se autoriza integración de ramas.

## 14. Anexo de procedencia

Fuente primaria: archivo proporcionado `75 desiciones(2).txt` (4007 líneas). Este documento contiene extractos con líneas; la transcripción completa se conserva como fuente independiente para evitar mezclar propuestas históricas con requisitos aprobados.
