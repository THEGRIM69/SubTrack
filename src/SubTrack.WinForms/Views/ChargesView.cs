namespace SubTrack.WinForms.Views;

internal sealed class ChargesView : ModuleDataView
{
    public ChargesView() : base(
        "Cobros",
        "Controla cargos programados y fechas de vencimiento.",
        "Cobros pendientes",
        "No hay cobros disponibles: este destino no incluye un modelo ni un servicio de cobros.",
        ("Fecha", 22F), ("Suscripción", 42F), ("Importe", 18F), ("Estado", 18F))
    {
    }
}
