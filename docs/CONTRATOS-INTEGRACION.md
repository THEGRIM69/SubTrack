# SubTrack 1.0 — Contratos de integración

Estado: PROPUESTA PENDIENTE DE APROBACION DEL EQUIPO.

## Principios
- Core define las entidades y las interfaces.
- Infrastructure implementa las interfaces de persistencia.
- WinForms consume servicios y contratos de Core.
- Ningun formulario ejecuta SQL directamente.
- Los importes se representan en centavos mediante long.
- Las fechas de negocio utilizan DateOnly.

## Suscripciones
Datos iniciales propuestos:
- Id: identificador unico.
- Nombre: nombre del servicio.
- MontoCentavos: costo de la suscripcion.
- FechaInicio: fecha de inicio.
- ProximoCobro: siguiente fecha de cobro.
- Frecuencia: periodicidad del cobro.
- Activa: estado de la suscripcion.

Operaciones propuestas:
- Registrar suscripcion.
- Consultar suscripciones.
- Actualizar suscripcion.
- Desactivar suscripcion.

## Pagos
Datos iniciales propuestos:
- Id: identificador unico.
- SuscripcionId: suscripcion relacionada.
- MontoCentavos: importe pagado.
- FechaPago: fecha del pago.

Operaciones propuestas:
- Registrar pago.
- Consultar historial de pagos.

## Gastos
Datos iniciales propuestos:
- Id: identificador unico.
- Concepto: descripcion del gasto.
- MontoCentavos: importe.
- Fecha: fecha del gasto.

Operaciones propuestas:
- Registrar gasto.
- Consultar gastos.

## Acuerdos pendientes
- Tipo definitivo de los identificadores.
- Nombres y firmas de las interfaces C#.
- Enumeracion de frecuencias.
- Reglas de calculo del proximo cobro.
- Validaciones y manejo de errores.
- Comportamiento de eliminacion y actualizacion.

## Integracion
Ningun desarrollador debe considerar definitivos estos
contratos hasta que GRIM, COSMO y ZYKO los aprueben.

Los agentes IA no pueden ejecutar comandos Git.

## Decision aprobada: Identificadores
- Todas las entidades principales utilizaran Guid como Id.
- Los identificadores nuevos se generaran con Guid.NewGuid().
- Infrastructure almacenara los Guid en SQLite como TEXT en formato estandar.
- Las relaciones entre entidades utilizaran Guid.

## Decision aprobada: Frecuencias de cobro
- Diaria: cada 1 dia.
- Semanal: cada 7 dias.
- Mensual: cada 1 mes.
- Trimestral: cada 3 meses.
- Semestral: cada 6 meses.
- Anual: cada 12 meses.
- ZYKO implementara el calculo de fechas mediante DateOnly y meses calendario cuando corresponda.
- COSMO almacenara la frecuencia en SQLite.
- GRIM mostrara las seis opciones en WinForms.

## Decision aprobada: Calculo del proximo cobro
- El siguiente cobro se calcula desde la ultima fecha de cobro programada.
- Si el mes siguiente tiene menos dias, se utiliza su ultimo dia.
- El cambio de dia se conserva para los cobros posteriores.
- Ejemplo mensual: 31/01/2027 -> 28/02/2027 -> 28/03/2027 -> 28/04/2027.
- ZYKO implementara esta regla mediante DateOnly.AddMonths().
- Los calculos trimestrales, semestrales y anuales seguiran el mismo principio.

## Decision aprobada: Registro de pagos
- El usuario registra manualmente los pagos.
- Cada pago genera un comprobante interno.
- El comprobante contiene monto, fecha y suscripcion.
- El cobro correspondiente queda marcado como pagado.
- Se calcula el siguiente cobro segun la frecuencia.
- El historial de pagos se conserva.
- No se realizan transferencias bancarias.
- El registro y la actualizacion del cobro deben guardarse en una misma transaccion SQLite.
- Queda pendiente definir pagos atrasados y anticipados.

## Decision aprobada: Pagos atrasados y anticipados
- Se distingue la fecha programada del cobro y la fecha real del pago.
- Los pagos pueden registrarse antes o despues del vencimiento.
- La fecha real del pago no modifica el calendario de cobros.
- El siguiente cobro se calcula desde la ultima fecha programada.
- Ejemplo: cobro 28/10/2026, pago 25/10/2026 o 02/11/2026.
- En ambos casos, el siguiente cobro sera 28/11/2026.
- Cada pago debe asociarse al cobro programado correspondiente.
- ZYKO implementara esta regla en Core.
- COSMO conservara ambas fechas en SQLite.
- GRIM mostrara la fecha programada y la fecha de pago.

## Decision aprobada: Prevencion de pagos duplicados
- Cada cobro programado solo puede registrarse como pagado una vez.
- Si ya esta pagado, se rechazara un segundo registro.
- Mensaje: Este cobro ya fue registrado como pagado.
- ZYKO validara esta regla en Core.
- COSMO implementara una restriccion UNIQUE en SQLite
  para impedir pagos duplicados del mismo cobro.
- GRIM mostrara el estado Pagado y deshabilitara
  la accion de registrar nuevamente ese cobro.
- La proteccion debe funcionar incluso ante
  dos intentos simultaneos de registro.
- Las correcciones de pagos se definiran por separado.

## Decision aprobada: Anulacion de pagos
- Los pagos registrados por error pueden anularse.
- No se permite eliminar definitivamente el historial.
- Se solicita confirmacion antes de anular.
- Se registra fecha y motivo de anulacion.
- El pago cambia al estado Anulado.
- El cobro asociado vuelve al estado Pendiente.
- Los pagos anulados no cuentan en los totales financieros.
- La anulacion debe ejecutarse en una transaccion SQLite.
- ZYKO implementara las validaciones.
- COSMO implementara la persistencia.
- GRIM implementara la confirmacion y visualizacion.

## Decision aprobada: Gestion de gastos
- Los gastos independientes se registraran manualmente.
- Cada gasto tendra un identificador Guid.
- Se almacenara concepto, categoria, monto y fecha.
- Los montos utilizaran long en centavos.
- Las fechas utilizaran DateOnly.
- Los gastos podran editarse y anularse.
- Las anulaciones conservaran el historial.
- Los gastos anulados no contaran en los totales.
- Los pagos de suscripciones y los gastos independientes
  se sumaran para calcular los egresos totales.
- Un pago de suscripcion no se registrara nuevamente
  como gasto independiente para evitar duplicaciones.
- ZYKO implementara las reglas de negocio.
- COSMO implementara la persistencia SQLite.
- GRIM implementara los formularios de gastos.

## Decision aprobada: Gestion de ingresos
- Se permitira registrar ingresos mensuales y extraordinarios.
- Cada ingreso tendra Id Guid, concepto, monto y fecha.
- Los importes utilizaran long en centavos.
- Las fechas utilizaran DateOnly.
- Los ingresos podran corregirse o anularse.
- Los ingresos anulados no contaran en los calculos.
- Los ingresos extraordinarios se sumaran al mes correspondiente.

## Decision aprobada: Dashboard financiero
El Dashboard mostrara:
1. Suscripciones activas.
2. Proximos cobros dentro de los siguientes 7 dias.
3. Pagos de suscripciones del mes.
4. Gastos independientes del mes.
5. Egresos totales del mes.
6. Cobros vencidos y pendientes.
7. Ingresos registrados del mes.
8. Saldo mensual actual estimado.
9. Ahorro proyectado al finalizar el mes.

## Reglas de calculo
- Egresos realizados = pagos validos + gastos validos.
- Saldo mensual = ingresos registrados - egresos realizados.
- Ahorro proyectado = ingresos previstos - egresos realizados
  - obligaciones pendientes del mes.
- Los ingresos previstos incluyen los registrados y los
  ingresos futuros confirmados, sin duplicarlos.
- Las obligaciones pendientes incluyen cobros programados
  y gastos futuros registrados, sin duplicaciones.
- Los movimientos anulados no se contabilizan.
- Los calculos mensuales utilizan meses calendario.
- Los importes se almacenan en centavos y se muestran
  en dolares con dos decimales.
- Un ahorro proyectado negativo representa un deficit.
- El ahorro proyectado es una estimacion, no una garantia.

## Responsabilidades
- ZYKO: entidades, validaciones y calculos financieros.
- COSMO: persistencia de ingresos y consultas financieras.
- GRIM: formularios de ingresos y Dashboard financiero.

## Cierre de acuerdos iniciales
Fecha: 19/10/2026
Version: 1.0 - Base funcional aprobada por GRIM.

## Acuerdos funcionales aprobados
- Identificadores Guid.
- Seis frecuencias de cobro.
- Calculo del siguiente cobro desde la ultima fecha programada.
- Registro manual de pagos.
- Pagos anticipados y atrasados.
- Prevencion de pagos duplicados.
- Anulacion de pagos conservando historial.
- Gestion de gastos independientes.
- Gestion de ingresos mensuales y extraordinarios.
- Dashboard financiero y ahorro proyectado.

## Responsabilidades
GRIM:
- Formularios, navegacion y Dashboard.
- Integracion visual con los servicios de Core.

COSMO:
- SQLite, repositorios, transacciones y respaldos.
- Persistencia de suscripciones, pagos, gastos e ingresos.

ZYKO:
- Entidades, interfaces y reglas de negocio.
- Calculos financieros y pruebas unitarias.

## Pendientes tecnicos antes de implementar
- Definir firmas exactas de interfaces y servicios C#.
- Definir estados y enumeraciones.
- Definir contratos para cobros programados.
- Acordar tratamiento de ingresos futuros y gastos previstos.
- Aprobar convenciones de errores y validaciones.

## Politica obligatoria
- Ningun agente IA ejecutara comandos Git.
- Las operaciones Git seran exclusivamente humanas.
- Toda integracion requiere revision y pruebas previas.
- Los cambios a contratos compartidos requieren acuerdo
  entre los desarrolladores afectados.
