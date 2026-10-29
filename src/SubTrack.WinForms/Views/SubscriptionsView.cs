using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Domain.Enums;
using SubTrack.WinForms.Theme;
using SubTrack.WinForms.UI;

namespace SubTrack.WinForms.Views;

internal sealed class SubscriptionsView : ViewBase
{
    private static readonly (AmountType Value, string Text)[] AmountTypes =
    {
        (AmountType.Fixed, "Fija"),
        (AmountType.Variable, "Variable")
    };

    private static readonly (BillingFrequency Value, string Text)[] Frequencies =
    {
        (BillingFrequency.Daily, "Diaria"),
        (BillingFrequency.Weekly, "Semanal"),
        (BillingFrequency.Monthly, "Mensual"),
        (BillingFrequency.Quarterly, "Trimestral"),
        (BillingFrequency.SemiAnnual, "Semestral"),
        (BillingFrequency.Annual, "Anual")
    };

    private readonly SearchBox _search = Ui.Search("Buscar suscripción");
    private readonly DataGridView _grid = Ui.Grid(
        ("Nombre", 30f),
        ("Importe", 14f),
        ("Tipo", 10f),
        ("Frecuencia", 13f),
        ("Próximo cobro", 14f),
        ("Estado", 12f));

    private readonly Label _summary = new()
    {
        AutoSize = true,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
        Font = Palette.Font(8.5f),
        ForeColor = Palette.Muted,
        Margin = new Padding(0)
    };

    private IReadOnlyList<Subscription> _items = Array.Empty<Subscription>();

    public SubscriptionsView(ViewContext context)
        : base(context)
    {
    }

    public override string Title => "Suscripciones";

    public override string Subtitle => "Altas, cambios de precio y cobros programados.";

    protected override void Build()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Palette.Canvas,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var more = Ui.Menu(
            "Más",
            ("Ver versiones", ShowVersionsAsync),
            ("Registrar cobro inicial", InitialChargeAsync),
            ("Desactivar suscripción", DeactivateAsync));

        var toolbar = Ui.Toolbar(
            new Control[] { _search },
            new Control[]
            {
                Ui.Action("Nueva suscripción", NewAsync, ButtonKind.Primary),
                Ui.Action("Editar", EditAsync),
                Ui.Action("Generar cobros", GenerateAsync),
                more
            });

        var card = new Card
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 8, 18, 12)
        };

        card.Controls.Add(_grid);
        card.Controls.Add(Ui.Header("Tus suscripciones", _summary));

        _search.Editor.TextChanged += (_, _) => ApplyFilter();
        _grid.CellDoubleClick += async (_, args) =>
        {
            if (args.RowIndex >= 0)
            {
                await EditAsync();
            }
        };

        Ui.Align(_grid, "Importe");

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(card, 0, 1);

        Controls.Add(root);
    }

    protected override async Task LoadAsync()
    {
        var result = await Context.Services.Subscriptions.GetAllAsync();

        if (Failed(result))
        {
            return;
        }

        _items = result.Value
            .OrderByDescending(subscription => subscription.IsActive)
            .ThenBy(subscription => subscription.Name)
            .ToList();

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var term = _search.Editor.Text.Trim();
        var filtered = _items.Where(subscription =>
            term.Length == 0 || subscription.Name.Contains(term, StringComparison.OrdinalIgnoreCase));

        _grid.Rows.Clear();

        foreach (var subscription in filtered)
        {
            var index = _grid.Rows.Add(
                subscription.Name,
                Format.Money(subscription.AmountCents),
                Format.AmountType(subscription.AmountType),
                Format.Frequency(subscription.Frequency),
                subscription.NextChargeDate is { } next ? Format.Date(next) : "—",
                subscription.IsActive ? "● Activa" : "● Inactiva");

            var row = _grid.Rows[index];
            row.Tag = subscription;

            Ui.Tint(row, "Estado", subscription.IsActive ? Palette.PrimaryDeep : Palette.Muted);

            if (!subscription.IsActive)
            {
                row.Cells["Nombre"].Style.ForeColor = Palette.Muted;
            }
        }

        var active = _items.Count(subscription => subscription.IsActive);
        _summary.Text = $"{active} activas · {_items.Count} en total";
    }

    private async Task NewAsync()
    {
        using var dialog = new FormDialog(
            "Nueva suscripción",
            "Configura los datos de la suscripción y su primer cobro.",
            "Crear suscripción");

        var name = dialog.AddText("Nombre");
        var amount = dialog.AddMoney("Importe por cobro");
        var type = dialog.AddCombo("Tipo de importe", AmountTypes, AmountType.Fixed);
        var frequency = dialog.AddCombo("Frecuencia", Frequencies, BillingFrequency.Monthly);
        var start = dialog.AddDate("Primer cobro", DateTime.Today);

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        var startDate = DateOnly.FromDateTime(start.Value);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var cents = Format.Cents(amount.Value);

        var initialCharges = startDate < today
            ? new[] { new InitialPendingCharge(startDate, cents) }
            : null;

        var result = await Context.Services.Subscriptions.CreateAsync(new CreateSubscriptionRequest(
            name.Text.Trim(),
            cents,
            FormDialog.Value<AmountType>(type),
            FormDialog.Value<BillingFrequency>(frequency),
            startDate,
            Guid.NewGuid(),
            initialCharges));

        if (Failed(result))
        {
            return;
        }

        var endOfMonth = new DateOnly(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));
        var until = startDate > endOfMonth ? startDate : endOfMonth;
        if (until >= today)
        {
            await Context.Services.Charges.GenerateAsync(new GenerateChargesRequest(
                result.Value.Id,
                until));
        }

        Notify($"Suscripción creada: {result.Value.Name}.");
        await DataChangedAsync(DataChangeKind.Subscriptions | DataChangeKind.Charges);
    }

    private async Task EditAsync()
    {
        if (Selected() is not { } subscription)
        {
            return;
        }

        using var dialog = new FormDialog(
            "Editar suscripción",
            "El cambio aplica desde la fecha indicada; los cobros anteriores conservan su importe.",
            "Guardar cambios");

        var name = dialog.AddText("Nombre", subscription.Name);
        var amount = dialog.AddMoney("Importe por cobro", subscription.AmountCents / 100m);
        var type = dialog.AddCombo("Tipo de importe", AmountTypes, subscription.AmountType);
        var frequency = dialog.AddCombo("Frecuencia", Frequencies, subscription.Frequency);
        var effective = dialog.AddDate("Vigente desde", DateTime.Today);

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        var cents = Format.Cents(amount.Value);

        var result = await Context.Services.Subscriptions.UpdateAsync(new UpdateSubscriptionRequest(
            subscription.Id,
            name.Text.Trim(),
            cents,
            FormDialog.Value<AmountType>(type),
            FormDialog.Value<BillingFrequency>(frequency),
            DateOnly.FromDateTime(effective.Value)));

        if (Failed(result))
        {
            return;
        }

        Notify($"Suscripción actualizada: {result.Value.Name}.");
        await DataChangedAsync(DataChangeKind.Subscriptions | DataChangeKind.Charges);
    }

    private async Task DeactivateAsync()
    {
        if (Selected() is not { } subscription)
        {
            return;
        }

        using var dialog = new FormDialog(
            "Desactivar suscripción",
            "Se conservan los cobros y pagos anteriores. No se generarán cobros nuevos.",
            "Desactivar");

        dialog.AcceptKind = ButtonKind.Danger;
        var effective = dialog.AddDate("Vigente desde", DateTime.Today);

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        var result = await Context.Services.Subscriptions.DeactivateAsync(
            new DeactivateSubscriptionRequest(subscription.Id, DateOnly.FromDateTime(effective.Value)));

        if (Failed(result))
        {
            return;
        }

        Notify($"Suscripción desactivada: {result.Value.Name}.");
        await DataChangedAsync(DataChangeKind.Subscriptions | DataChangeKind.Charges);
    }

    private async Task GenerateAsync()
    {
        if (Selected() is not { } subscription)
        {
            return;
        }

        var today = DateTime.Today;

        var suggested = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

        if (suggested < today)
        {
            suggested = today;
        }

        using var dialog = new FormDialog(
            "Generar cobros",
            $"Se crearán los cobros pendientes de {subscription.Name} hasta la fecha indicada (máximo 12 meses).",
            "Generar");

        var until = dialog.AddDate("Generar hasta", suggested);
        until.MaxDate = today.AddMonths(12);

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        var result = await Context.Services.Charges.GenerateAsync(
            new GenerateChargesRequest(subscription.Id, DateOnly.FromDateTime(until.Value)));

        if (Failed(result))
        {
            return;
        }

        Notify(result.Value.Count == 0
            ? "No había cobros nuevos por generar."
            : $"{result.Value.Count} cobros generados para {subscription.Name}.");

        await DataChangedAsync(DataChangeKind.Charges);
    }

    private async Task InitialChargeAsync()
    {
        if (Selected() is not { } subscription)
        {
            return;
        }

        using var dialog = new FormDialog(
            "Cobro anterior",
            "Registra una obligación anterior a la instalación que sigue pendiente de pago.",
            "Registrar");

        var amount = dialog.AddMoney("Importe", subscription.AmountCents / 100m);
        var date = dialog.AddDate("Fecha del cobro", DateTime.Today);

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        var result = await Context.Services.Charges.RegisterInitialPendingAsync(
            new RegisterInitialPendingChargeRequest(
                subscription.Id,
                DateOnly.FromDateTime(date.Value),
                Format.Cents(amount.Value)));

        if (Failed(result))
        {
            return;
        }

        Notify("Cobro inicial registrado.");
        await DataChangedAsync(DataChangeKind.Charges);
    }

    private async Task ShowVersionsAsync()
    {
        if (Selected() is not { } subscription)
        {
            return;
        }

        var result = await Context.Services.Subscriptions.GetVersionsAsync(subscription.Id);

        if (Failed(result))
        {
            return;
        }

        TableView.Show(
            FindForm(),
            "Versiones",
            $"{subscription.Name} · cambios de precio y frecuencia",
            new[]
            {
                ("Versión", 12f),
                ("Vigente desde", 18f),
                ("Importe", 18f),
                ("Tipo", 14f),
                ("Frecuencia", 20f),
                ("Día ancla", 18f)
            },
            result.Value
                .OrderBy(version => version.VersionNumber)
                .Select(version => new[]
                {
                    $"v{version.VersionNumber}",
                    Format.Date(version.EffectiveFrom),
                    Format.Money(version.AmountCents),
                    Format.AmountType(version.AmountType),
                    Format.Frequency(version.Frequency),
                    version.AnchorDay.ToString()
                }));
    }

    private Subscription? Selected()
    {
        var subscription = Ui.Selected<Subscription>(_grid);

        if (subscription is null)
        {
            Notify("Selecciona una suscripción.", true);
        }

        return subscription;
    }
}
