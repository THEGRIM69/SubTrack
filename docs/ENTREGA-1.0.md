# SubTrack 1.0 — Nota de entrega

## Estado

Aplicación de escritorio funcional para Windows. La interfaz consume exclusivamente
las interfaces de `SubTrack.Core`; no contiene SQL ni cálculos financieros.
Los importes se almacenan en centavos (`long`), las fechas de negocio usan
`DateOnly` y las marcas técnicas se guardan en UTC.

## Módulos

| Módulo | Flujos disponibles |
| --- | --- |
| Resumen | KPIs del mes, gráfico de 6 meses, próximos cobros, cobros atrasados, generación de cobros |
| Suscripciones | Alta, edición versionada, desactivación, búsqueda, historial de versiones, cobro inicial |
| Cobros y pagos | Cobros pendientes, pago con fecha, ajuste de importe variable, anulación de cobro, historial de pagos con comprobante |
| Gastos | Alta y edición, confirmación, anulación, correcciones, filtros por estado, categoría y rango de fechas |
| Ingresos | Alta y edición, confirmación, anulación, correcciones, plantillas recurrentes y generación de ingresos previstos |
| Configuración | Categorías de gasto, saldo inicial con historial, respaldos, verificación de integridad y optimización |

## Reglas de negocio cubiertas

- Día ancla preservado en los cálculos mensuales (31/01 → 28/02 → 31/03).
- Los cobros se generan de forma explícita; nunca de forma automática.
- La fecha de pago no puede ser futura; el comprobante usa el año de la fecha de pago.
- Solo los cobros de importe variable se pueden ajustar.
- Los ingresos previstos son los confirmados a futuro; el saldo inicial no configurado es cero.
- Creación idempotente mediante `OperationId` en pagos, gastos, ingresos, suscripciones y plantillas.
- Ediciones de registros confirmados dejan rastro en el historial de correcciones.

## Ejecución

```powershell
dotnet build SubTrack.slnx
dotnet run --project src/SubTrack.WinForms
```

La base local se crea en `%LOCALAPPDATA%\SubTrack\data\subtrack.db` y se migra
automáticamente al arrancar. Para empezar de cero, cierra la aplicación y elimina
ese archivo (la aplicación vuelve a crear el esquema y las categorías iniciales).

## Atajos

- `Ctrl+1` … `Ctrl+6`: navegar entre módulos.
- `F5`: actualizar la vista actual.
- `Esc`: cerrar diálogos.

## Pruebas

```powershell
dotnet test SubTrack.slnx
```

134 pruebas automatizadas: dominio, calculadores, servicios, persistencia,
idempotencia, correcciones y dashboard.

## Nota de entorno

Si Windows bloquea el ejecutable recién compilado por Smart App Control, ejecuta
la aplicación con `dotnet run --project src/SubTrack.WinForms` mientras se revisa
la configuración de seguridad del equipo.
