namespace SubTrack.WinForms.Views;

internal sealed class PaymentsView : ModuleDataView
{
    public PaymentsView() : base(
        "Pagos",
        "Consulta los pagos registrados de tus suscripciones.",
        "Pagos registrados",
        "No hay pagos disponibles: este destino no incluye un modelo ni un servicio de pagos.",
        ("Fecha", 22F), ("Suscripción", 34F), ("Comprobante", 28F), ("Estado", 16F))
    {
    }
}
