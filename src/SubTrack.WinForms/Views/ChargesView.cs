using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Domain.Enums;
using SubTrack.WinForms.Theme;
using SubTrack.WinForms.UI;

namespace SubTrack.WinForms.Views;

internal enum ChargesViewMode
{
    All,
    Charges,
    Payments
}

internal enum ChargeFilter
{
    All,
    Pending,
    Paid,
    Overdue,
    Cancelled
}

internal class ChargesView : ViewBase
{
    private static readonly (ChargeFilter Value, string Text)[] ChargeFilters =
    {
        (ChargeFilter.All, "Todos los cobros"),
        (ChargeFilter.Pending, "Pendientes"),
        (ChargeFilter.Paid, "Pagados"),
        (ChargeFilter.Overdue, "Vencidos"),
        (ChargeFilter.Cancelled, "Anulados")
    };

    private readonly ChargesViewMode _mode;
    private Card _pendingCard = null!;
    private Card _paymentsCard = null!;
    private TableLayoutPanel _root = null!;

    private readonly ComboBox _chargeFilter = Combo.Create(ChargeFilters, ChargeFilter.All, 180);

    private readonly DataGridView _chargesGrid = Ui.Grid(
        ("Vencimiento", 18f),
        ("Suscripción / concepto", 42f),
        ("Importe", 18f),
        ("Estado", 22f));

    private readonly DataGridView _paymentsGrid = Ui.Grid(
        ("Fecha", 11f),
        ("Suscripción", 20f),
        ("Vencimiento", 13f),
        ("Importe", 13f),
        ("Método", 10f),
        ("Comprobante", 20f),
        ("Estado", 13f));

    private readonly Pager _pager = new();
    private readonly Label _pendingTitle = new()
    {
        AutoSize = true,
        Font = Palette.Font(11f, FontStyle.Bold),
        ForeColor = Palette.Text,
        Margin = new Padding(0, 0, 0, 0)
    };

    private Dictionary<Guid, string> _names = new();
    private IReadOnlyList<ScheduledCharge> _charges = Array.Empty<ScheduledCharge>();
    private Dictionary<Guid, ScheduledCharge> _chargesById = new();
    private int _page = 1;
    private int _totalPages = 1;

    public ChargesView(ViewContext context)
        : this(context, ChargesViewMode.All)
    {
    }

    public ChargesView(ViewContext context, ChargesViewMode mode)
        : base(context)
    {
        _mode = mode;
        ApplyMode();
    }

    public override string Title => _mode switch
    {
        ChargesViewMode.Charges => "Cobros",
        ChargesViewMode.Payments => "Pagos",
        _ => "Cobros y pagos"
    };

    public override string Subtitle => _mode switch
    {
        ChargesViewMode.Charges => "Controla los vencimientos y registra pagos de suscripciones.",
        ChargesViewMode.Payments => "Historial de pagos registrados y comprobantes.",
        _ => "Registra pagos y controla los vencimientos."
    };

    protected override void Build()
    {
        var pendingActions = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        pendingActions.Controls.Add(_chargeFilter);
        pendingActions.Controls.Add(Ui.Compact(Ui.Action("Pagar", PayAsync, ButtonKind.Primary)));
        pendingActions.Controls.Add(Ui.Compact(Ui.Action("Ajustar", AdjustAsync)));
        pendingActions.Controls.Add(Ui.Menu(
            "Más",
            ("Ver detalles", ShowChargeDetailsAsync),
            ("Generar cobros", GenerateAsync),
            ("Anular cobro", CancelChargeAsync)));

        var pendingHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 40,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Palette.Surface,
            Margin = new Padding(0)
        };

        pendingHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pendingHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _pendingTitle.AutoSize = false;
        _pendingTitle.Dock = DockStyle.Fill;
        _pendingTitle.TextAlign = ContentAlignment.MiddleLeft;

        pendingHeader.Controls.Add(_pendingTitle, 0, 0);
        pendingHeader.Controls.Add(pendingActions, 1, 0);

        var paymentActions = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        paymentActions.Controls.Add(Ui.Compact(Ui.Action("Detalles", ShowPaymentDetailsAsync)));
        paymentActions.Controls.Add(Ui.Compact(Ui.Action("Anular pago", CancelPaymentAsync)));
        paymentActions.Controls.Add(_pager);

        var paymentsHeader = Ui.Header("Pagos registrados", paymentActions);

        _pendingCard = new Card
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 6, 18, 12),
            Margin = new Padding(0, 0, 0, 14)
        };

        _pendingCard.Controls.Add(_chargesGrid);
        _pendingCard.Controls.Add(pendingHeader);

        _paymentsCard = new Card
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 6, 18, 12),
            Margin = new Padding(0)
        };

        _paymentsCard.Controls.Add(_paymentsGrid);
        _paymentsCard.Controls.Add(paymentsHeader);

        _root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Palette.Canvas,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 54));
        _root.RowStyles.Add(new RowStyle(SizeType.Percent, 46));

        _root.Controls.Add(_pendingCard, 0, 0);
        _root.Controls.Add(_paymentsCard, 0, 1);

        _chargesGrid.RowTemplate.Height = 42;
        _chargesGrid.ColumnHeadersHeight = 36;
        _paymentsGrid.RowTemplate.Height = 42;
        _paymentsGrid.ColumnHeadersHeight = 36;

        Ui.Align(_chargesGrid, "Importe");
        Ui.Align(_paymentsGrid, "Importe");

        _chargeFilter.SelectedIndexChanged += (_, _) => ApplyChargeFilter();
        _chargesGrid.CellDoubleClick += async (_, args) =>
        {
            if (args.RowIndex >= 0)
            {
                await ShowChargeDetailsAsync();
            }
        };
        _paymentsGrid.CellDoubleClick += async (_, args) =>
        {
            if (args.RowIndex >= 0)
            {
                await ShowPaymentDetailsAsync();
            }
        };

        _pager.Previous += async () =>
        {
            _page = Math.Max(1, _page - 1);
            await RefreshAsync();
        };

        _pager.Next += async () =>
        {
            _page = Math.Min(_totalPages, _page + 1);
            await RefreshAsync();
        };

        Controls.Add(_root);
    }

    private void ApplyMode()
    {
        if (_mode == ChargesViewMode.Charges)
        {
            _root.Controls.Remove(_pendingCard);
            _root.Visible = false;
            _pendingCard.Margin = Padding.Empty;
            _pendingCard.Dock = DockStyle.Fill;
            Controls.Add(_pendingCard);
            _pendingCard.BringToFront();
        }
        else if (_mode == ChargesViewMode.Payments)
        {
            _root.Controls.Remove(_paymentsCard);
            _root.Visible = false;
            _paymentsCard.Margin = Padding.Empty;
            _paymentsCard.Dock = DockStyle.Fill;
            Controls.Add(_paymentsCard);
            _paymentsCard.BringToFront();
        }
    }

    protected override async Task LoadAsync()
    {
        var subscriptions = await Context.Services.Subscriptions.GetAllAsync();

        if (Failed(subscriptions))
        {
            return;
        }

        _names = subscriptions.Value.ToDictionary(subscription => subscription.Id, subscription => subscription.Name);

        if (_mode != ChargesViewMode.Payments)
        {
            await LoadChargesAsync();
        }

        if (_mode == ChargesViewMode.Payments)
        {
            var charges = await Context.Services.Charges.GetAllAsync();
            if (Failed(charges))
            {
                return;
            }

            _charges = charges.Value;
            _chargesById = _charges.ToDictionary(charge => charge.Id);
        }

        if (_mode != ChargesViewMode.Charges)
        {
            await LoadPaymentsAsync();
        }
    }

    private async Task LoadChargesAsync()
    {
        var result = await Context.Services.Charges.GetAllAsync();

        if (Failed(result))
        {
            return;
        }

        _charges = result.Value;
        _chargesById = _charges.ToDictionary(charge => charge.Id);
        ApplyChargeFilter();
    }

    private void ApplyChargeFilter()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var filter = Combo.Value<ChargeFilter>(_chargeFilter);
        var charges = _charges.Where(charge => filter switch
        {
            ChargeFilter.Pending => charge.Status == ScheduledChargeStatus.Pending,
            ChargeFilter.Paid => charge.Status == ScheduledChargeStatus.Paid,
            ChargeFilter.Overdue => charge.Status == ScheduledChargeStatus.Pending
                && charge.ScheduledDate < today,
            ChargeFilter.Cancelled => charge.Status == ScheduledChargeStatus.Cancelled,
            _ => true
        }).ToList();

        _chargesGrid.Rows.Clear();

        foreach (var charge in charges)
        {
            var name = _names.TryGetValue(charge.SubscriptionId, out var value) ? value : "—";
            var overdue = charge.Status == ScheduledChargeStatus.Pending && charge.ScheduledDate < today;
            var status = overdue ? "● Vencido" : $"● {Format.ChargeStatus(charge.Status)}";

            var index = _chargesGrid.Rows.Add(
                Format.Date(charge.ScheduledDate),
                name,
                Format.Money(charge.AmountCents),
                status);

            var row = _chargesGrid.Rows[index];
            row.Tag = charge;

            var color = overdue
                ? Palette.Danger
                : charge.Status switch
                {
                    ScheduledChargeStatus.Paid => Palette.PrimaryDeep,
                    ScheduledChargeStatus.Cancelled => Palette.Muted,
                    _ => Palette.Warning
                };
            Ui.Tint(row, "Estado", color);
        }

        _pendingTitle.Text = charges.Count == 0
            ? $"{ChargeFilterLabel(filter)} · sin resultados"
            : $"{ChargeFilterLabel(filter)} · {charges.Count}";
    }

    private static string ChargeFilterLabel(ChargeFilter filter) => filter switch
    {
        ChargeFilter.Pending => "Cobros pendientes",
        ChargeFilter.Paid => "Cobros pagados",
        ChargeFilter.Overdue => "Cobros vencidos",
        ChargeFilter.Cancelled => "Cobros anulados",
        _ => "Todos los cobros"
    };

    private async Task ShowChargeDetailsAsync()
    {
        if (Ui.Selected<ScheduledCharge>(_chargesGrid) is not { } charge)
        {
            Notify("Selecciona un cobro para consultar sus detalles.", true);
            return;
        }

        var result = await Context.Services.Payments.GetByScheduledChargeIdAsync(charge.Id);

        if (Failed(result))
        {
            return;
        }

        var name = _names.TryGetValue(charge.SubscriptionId, out var value) ? value : "Suscripción no disponible";
        var today = DateOnly.FromDateTime(DateTime.Today);
        var status = charge.Status == ScheduledChargeStatus.Pending && charge.ScheduledDate < today
            ? "Vencido"
            : Format.ChargeStatus(charge.Status);
        var rows = result.Value.Count == 0
            ? new[] { new[] { "—", "—", "—", "Sin pagos asociados", "—" } }
            : result.Value.Select(payment => new[]
            {
                Format.Date(payment.PaymentDate),
                Format.Money(payment.AmountCents),
                payment.VoucherNumber,
                Format.PaymentStatus(payment.Status),
                payment.CancellationReason ?? "—"
            });

        TableView.Show(
            FindForm(),
            "Detalles del cobro",
            $"{name} · vencimiento {Format.Date(charge.ScheduledDate)} · {Format.Currency(charge.AmountCents)} · {status}",
            new[]
            {
                ("Fecha de pago", 18f),
                ("Importe", 16f),
                ("Comprobante", 22f),
                ("Estado", 16f),
                ("Motivo de anulación", 28f)
            },
            rows);
    }

    private async Task LoadPaymentsAsync()
    {
        while (true)
        {
            var result = await Context.Services.Payments.GetHistoryAsync(new PageRequest(_page, 50));

            if (Failed(result))
            {
                return;
            }

            _totalPages = Math.Max(result.Value.TotalPages, 1);

            if (_page > _totalPages)
            {
                _page = _totalPages;
                continue;
            }

            _paymentsGrid.Rows.Clear();

            foreach (var payment in result.Value.Items)
            {
                var charge = _chargesById.GetValueOrDefault(payment.ScheduledChargeId);
                var subscriptionName = charge is not null
                    && _names.TryGetValue(charge.SubscriptionId, out var name)
                        ? name
                        : "—";
                var index = _paymentsGrid.Rows.Add(
                    Format.Date(payment.PaymentDate),
                    subscriptionName,
                    charge is null ? "—" : Format.Date(charge.ScheduledDate),
                    Format.Money(payment.AmountCents),
                    "No registrado en 1.0",
                    payment.VoucherNumber,
                    payment.Status == PaymentStatus.Active ? "● Vigente" : "● Anulado");

                var row = _paymentsGrid.Rows[index];
                row.Tag = payment;

                Ui.Tint(row, "Estado", payment.Status == PaymentStatus.Active
                    ? Palette.PrimaryDeep
                    : Palette.Muted);
            }

            _pager.SetPage(result.Value.Page, result.Value.TotalPages);
            return;
        }
    }

    private async Task PayAsync()
    {
        if (Ui.Selected<ScheduledCharge>(_chargesGrid) is not { } charge)
        {
            Notify("Selecciona un cobro pendiente.", true);
            return;
        }

        if (charge.Status != ScheduledChargeStatus.Pending)
        {
            Notify("Solo se pueden pagar cobros pendientes.", true);
            return;
        }

        var name = _names.TryGetValue(charge.SubscriptionId, out var value) ? value : "la suscripción";

        using var dialog = new FormDialog(
            "Registrar pago",
            $"{Format.Currency(charge.AmountCents)} para {name} · cobro del {Format.Date(charge.ScheduledDate)}. El comprobante se emite automáticamente.",
            "Registrar pago");

        var date = dialog.AddDate("Fecha de pago", DateTime.Today, maxToday: true);

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        using var operation = BeginOperation();
        var result = await Context.Services.Payments.RegisterAsync(
            new RegisterPaymentRequest(
                charge.Id,
                DateOnly.FromDateTime(date.Value),
                Guid.NewGuid()));

        if (Failed(result))
        {
            return;
        }

        Notify($"Pago registrado · comprobante {result.Value.VoucherNumber}.");
        await DataChangedAsync(DataChangeKind.Payments | DataChangeKind.Charges);
    }

    private async Task AdjustAsync()
    {
        if (Ui.Selected<ScheduledCharge>(_chargesGrid) is not { } charge)
        {
            Notify("Selecciona un cobro pendiente.", true);
            return;
        }

        if (charge.Status != ScheduledChargeStatus.Pending)
        {
            Notify("Solo se pueden ajustar cobros pendientes.", true);
            return;
        }

        using var dialog = new FormDialog(
            "Ajustar importe",
            "Solo las suscripciones de importe variable admiten ajuste antes del pago.",
            "Guardar importe");

        var amount = dialog.AddMoney("Importe real", charge.AmountCents / 100m);

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        using var operation = BeginOperation();
        var result = await Context.Services.Charges.AdjustAmountAsync(
            new AdjustChargeAmountRequest(charge.Id, Format.Cents(amount.Value)));

        if (Failed(result))
        {
            return;
        }

        Notify("Importe del cobro actualizado.");
        await DataChangedAsync(DataChangeKind.Charges);
    }

    private async Task CancelChargeAsync()
    {
        if (Ui.Selected<ScheduledCharge>(_chargesGrid) is not { } charge)
        {
            Notify("Selecciona un cobro pendiente.", true);
            return;
        }

        if (charge.Status != ScheduledChargeStatus.Pending)
        {
            Notify("Solo se pueden anular cobros pendientes. Si existe un pago vigente, anúlalo primero.", true);
            return;
        }

        var confirmed = Confirm.Ask(
            FindForm(),
            "Anular cobro",
            $"¿Anular el cobro de {Format.Currency(charge.AmountCents)} del {Format.Date(charge.ScheduledDate)}? Esta acción no se puede deshacer.",
            "Anular cobro",
            danger: true);

        if (!confirmed)
        {
            return;
        }

        using var operation = BeginOperation();
        var result = await Context.Services.Charges.CancelAsync(charge.Id);

        if (Failed(result))
        {
            return;
        }

        Notify("Cobro anulado.");
        await DataChangedAsync(DataChangeKind.Charges);
    }

    private async Task GenerateAsync()
    {
        var today = DateTime.Today;
        var suggested = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

        using var dialog = new FormDialog(
            "Generar cobros",
            "Genera los cobros pendientes de todas las suscripciones activas hasta la fecha indicada (máximo 12 meses).",
            "Generar");

        var until = dialog.AddDate("Generar hasta", suggested);
        until.MaxDate = today.AddMonths(12);

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        using var operation = BeginOperation();
        var subscriptions = await Context.Services.Subscriptions.GetAllAsync();
        if (Failed(subscriptions))
        {
            return;
        }

        var created = 0;
        var failures = new List<string>();
        var limit = DateOnly.FromDateTime(until.Value);
        foreach (var sub in subscriptions.Value.Where(s => s.IsActive))
        {
            var res = await Context.Services.Charges.GenerateAsync(new GenerateChargesRequest(sub.Id, limit));
            if (res.IsSuccess)
            {
                created += res.Value.Count;
            }
            else
            {
                failures.Add($"{sub.Name}: {Describe(res.Errors)}");
            }
        }

        var message = created == 0 ? "No había cobros nuevos por generar." : $"{created} cobros generados.";
        if (failures.Count > 0)
        {
            message += $" No se completaron {failures.Count} suscripciones: {string.Join(" · ", failures)}";
        }

        Notify(message, failures.Count > 0);
        await DataChangedAsync(DataChangeKind.Subscriptions | DataChangeKind.Charges);
    }

    private async Task CancelPaymentAsync()
    {
        if (Ui.Selected<Payment>(_paymentsGrid) is not { } payment)
        {
            Notify("Selecciona un pago registrado.", true);
            return;
        }

        if (payment.Status == PaymentStatus.Cancelled)
        {
            Notify("Este pago ya está anulado. Su historial se conserva.", true);
            return;
        }

        using var dialog = new FormDialog(
            "Anular pago",
            $"Comprobante {payment.VoucherNumber} por {Format.Currency(payment.AmountCents)}. El número se conserva para auditoría.",
            "Anular pago");

        dialog.AcceptKind = ButtonKind.Danger;
        var reason = dialog.AddText("Motivo");

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(reason.Text))
        {
            Notify("El motivo de anulación es obligatorio.", true);
            return;
        }

        using var operation = BeginOperation();
        var result = await Context.Services.Payments.CancelAsync(
            new CancelPaymentRequest(payment.Id, reason.Text.Trim()));

        if (Failed(result))
        {
            return;
        }

        Notify("Pago anulado.");
        await DataChangedAsync(DataChangeKind.Payments | DataChangeKind.Charges);
    }

    private async Task ShowPaymentDetailsAsync()
    {
        if (Ui.Selected<Payment>(_paymentsGrid) is not { } selected)
        {
            Notify("Selecciona un pago para consultar sus detalles.", true);
            return;
        }

        var result = await Context.Services.Payments.GetByIdAsync(selected.Id);
        if (Failed(result))
        {
            return;
        }

        var payment = result.Value;
        var hasCharge = _chargesById.TryGetValue(payment.ScheduledChargeId, out var charge);
        var subscriptionName = hasCharge
            && _names.TryGetValue(charge!.SubscriptionId, out var name)
                ? name
                : "No disponible";
        var chargeDescription = hasCharge
            ? $"{subscriptionName} · vencimiento {Format.Date(charge!.ScheduledDate)}"
            : "Cobro relacionado no disponible";
        var details = new[]
        {
            new[] { "Fecha de pago", Format.Date(payment.PaymentDate) },
            new[] { "Importe", Format.Currency(payment.AmountCents) },
            new[] { "Método", "No registrado en SubTrack 1.0" },
            new[] { "Cobro relacionado", chargeDescription },
            new[] { "Comprobante", payment.VoucherNumber },
            new[] { "Estado", Format.PaymentStatus(payment.Status) },
            new[] { "Anulado en", payment.CancelledAt is { } cancelledAt ? Format.Stamp(cancelledAt) : "—" },
            new[] { "Motivo de anulación", payment.CancellationReason ?? "—" }
        };

        TableView.Show(
            FindForm(),
            "Detalles del pago",
            $"{subscriptionName} · {payment.VoucherNumber}",
            new[] { ("Dato", 32f), ("Detalle", 68f) },
            details);
    }
}
