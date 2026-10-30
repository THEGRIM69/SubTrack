namespace SubTrack.WinForms.Views;

internal sealed class ExpensesView : ModuleDataView
{
    public ExpensesView() : base(
        "Gastos",
        "Revisa gastos previstos, confirmados y sus categorías.",
        "Gastos registrados",
        "No hay gastos disponibles: GRIM no incluye modelos, categorías ni servicios de gastos en esta rama.",
        ("Fecha", 14F), ("Concepto", 34F), ("Categoría", 20F), ("Importe", 16F), ("Estado", 16F))
    {
    }
}
