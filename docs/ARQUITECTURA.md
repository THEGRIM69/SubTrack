# SubTrack 1.0 — Arquitectura

## Objetivo
Aplicación de escritorio para administrar suscripciones,
pagos y gastos mediante Windows Forms y SQLite.

## SubTrack.Core — ZYKO
Responsable de:
- Entidades y enumeraciones del dominio.
- Interfaces de repositorios.
- Reglas de negocio y cálculos financieros.
- Validaciones independientes de la interfaz.

No debe depender de WinForms ni de SQLite.

## SubTrack.Infrastructure — COSMO
Responsable de:
- Conexión y configuración de SQLite.
- Implementación de repositorios definidos en Core.
- Creación y actualización del esquema.
- Transacciones y respaldos.

Depende de SubTrack.Core.

## SubTrack.WinForms — GRIM
Responsable de:
- Formularios y navegación.
- Dashboard y componentes visuales.
- Presentación de suscripciones, pagos y gastos.
- Configuración inicial de dependencias.

Depende de Core e Infrastructure.
No debe contener consultas SQL ni cálculos financieros complejos.

## SubTrack.Tests
Pruebas de reglas de negocio y persistencia.
Depende de Core e Infrastructure.

## Reglas de integración
1. Definir contratos en Core antes de implementar su uso.
2. Evitar modificar archivos asignados a otro desarrollador.
3. Utilizar importes monetarios enteros en centavos.
4. Utilizar DateOnly para fechas de negocio.
5. Mantener SQLite local y activar claves foráneas.
6. Revisar y probar los cambios antes de integrarlos.

## Política de agentes IA
Codex y Antigravity no pueden ejecutar ningún comando Git,
ni siquiera comandos de lectura.
Solo los desarrolladores humanos realizan operaciones Git
después de revisar los cambios.
