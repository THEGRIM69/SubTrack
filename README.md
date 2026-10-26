# SubTrack 1.0

Sistema de escritorio para administrar suscripciones, pagos y gastos.

## Tecnologías
- C# y .NET 10
- Windows Forms
- SQLite
- xUnit

## Equipo
- GRIM: interfaz gráfica (SubTrack.WinForms)
- COSMO: base de datos (SubTrack.Infrastructure)
- ZYKO: lógica de negocio (SubTrack.Core)

## Estructura
- src/SubTrack.WinForms
- src/SubTrack.Core
- src/SubTrack.Infrastructure
- tests/SubTrack.Tests
- docs

## Compilación
dotnet build SubTrack.slnx

## Ejecución
dotnet run --project src/SubTrack.WinForms

La base de datos local se crea en `%LOCALAPPDATA%\SubTrack\data\subtrack.db`.
Para volver a los datos iniciales, cierra la aplicación y elimina ese archivo.

## Pruebas
dotnet test SubTrack.slnx

## Entrega prevista
2 de noviembre de 2026.

## Política de Git
Solo los desarrolladores humanos pueden ejecutar operaciones Git.
Los agentes de IA no tienen autorización para ejecutar comandos Git.
