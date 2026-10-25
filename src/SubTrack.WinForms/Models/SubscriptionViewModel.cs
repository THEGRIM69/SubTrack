using System.Globalization;

namespace SubTrack.WinForms.Models;

internal enum SubscriptionStatus
{
    Active,
    DueSoon,
    Paused
}

internal sealed class SubscriptionViewModel
{
    public static readonly string[] PeriodicidadesDisponibles =
    [
        "Diaria",
        "Semanal",
        "Mensual",
        "Trimestral",
        "Semestral",
        "Anual"
    ];

    public static readonly string[] CategoriasSugeridas =
    [
        "Entretenimiento",
        "Almacenamiento",
        "Música",
        "Diseño",
        "Educación",
        "Software",
        "Servicios",
        "General"
    ];

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Nombre { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public long MontoCentavos { get; set; }
    public string Periodicidad { get; set; } = "Mensual";
    public DateOnly ProximoCobro { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public SubscriptionStatus Estado { get; set; } = SubscriptionStatus.Active;

    public decimal MontoDecimal => MontoCentavos / 100m;
    public string MontoFormateado => $"${MontoDecimal:F2}";

    public string EstadoTexto => Estado switch
    {
        SubscriptionStatus.Active => "Activa",
        SubscriptionStatus.DueSoon => "Próxima",
        SubscriptionStatus.Paused => "Pausada",
        _ => "Desconocido"
    };

    public string ProximoCobroFormateado => ProximoCobro.ToString("dd MMM yyyy", CultureInfo.GetCultureInfo("es-ES"));

    public SubscriptionViewModel Clone() => new()
    {
        Id = Id,
        Nombre = Nombre,
        Categoria = Categoria,
        MontoCentavos = MontoCentavos,
        Periodicidad = Periodicidad,
        ProximoCobro = ProximoCobro,
        Estado = Estado
    };
}
