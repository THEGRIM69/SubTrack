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

internal class ChargesView : ViewBase
{
    private readonly ChargesViewMode _mode;
    private Card _pendingCard = null!;
    private Card _paymentsCard = null!;
    private TableLayoutPanel _root = null!;

    private readonly DataGridView _pendingGrid = Ui.Grid(
        ("Fecha", 22f),
        ("Suscripción", 42f),
        ("Importe", 18f),
        ("Estado", 18f));

    private readonly DataGridView _paymentsGrid = Ui.Grid(
        ("Fecha", 22f),
        ("Importe", 20f),
        ("Comprobante", 34f),
        ("Estado", 24f));

    private readonly Pager _pager = new();
    private readonly Label _pendingTitle = new()
    {
        AutoSize = true,
        Font = Palette.Font(11f, FontStyle.Bold),
        ForeColor = Palette.Text,
        Margin = new Padding(0, 0, 0, 0)
    };

    private Dictionary<Guid, string> _names = new();
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

        pendingActions.Controls.Add(Ui.Compact(Ui.Action("Pagar", PayAsync, ButtonKind.Primary)));
        pendingActions.Controls.Add(Ui.Compact(Ui.Action("Ajustar", AdjustAsync)));
        pendingActions.Controls.Add(Ui.Compact(Ui.Action("Generar cobros", GenerateAsync)));
        pendingActions.Controls.Add(Ui.Compact(Ui.Action("Anular cobro", CancelChargeAsync)));

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

        paymentActions.Controls.Add(Ui.Compact(Ui.Action("Anular pago", CancelPaymentAsync)));
        paymentActions.Controls.Add(_pager);

        var paymentsHeader = Ui.Header("Pagos registrados", paymentActions);

        _pendingCard = new Card
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 6, 18, 12),
            Margin = new Padding(0, 0, 0, 14)
        };

        _pendingCard.Controls.Add(_pendingGrid);
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

        _pendingGrid.RowTemplate.Height = 42;
        _pendingGrid.ColumnHeadersHeight = 36;
        _paymentsGrid.RowTemplate.Height = 42;
        _paymentsGrid.ColumnHeadersHeight = 36;

        Ui.Align(_pendingGrid, "Importe");
        Ui.Align(_paymentsGrid, "Importe");

        _pendingGrid.CellDoubleClick += async (_, args) =>
        {
            if (args.RowIndex >= 0)
            {
                await PayAsync();
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
        if (_mode != ChargesViewMode.Payments)
        {
            var subscriptions = await Context.Services.Subscriptions.GetAllAsync();

            if (Failed(subscriptions))
            {
                return;
            }

            _names = subscriptions.Value.ToDictionary(subscription => subscription.Id, subscription => subscription.Name);

            await LoadPendingAsync();
        }

        if (_mode != ChargesViewMode.Charges)
        {
            await LoadPaymentsAsync();
        }
    }

    private async Task LoadPendingAsync()
    {
        var result = await Context.Services.Charges.GetPendingAsync();

        if (Failed(result))
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);

        _pendingGrid.Rows.Clear();

        foreach (var charge in result.Value.OrderBy(item => item.ScheduledDate))
        {
            var name = _names.TryGetValue(charge.SubscriptionId, out var value) ? value : "—";
            var overdue = charge.ScheduledDate < today;

            var index = _pendingGrid.Rows.Add(
                Format.Date(charge.ScheduledDate),
                name,
                Format.Money(charge.AmountCents),
                overdue ? "● Atrasado" : "● Pendiente");

            var row = _pendingGrid.Rows[index];
            row.Tag = charge;

            Ui.Tint(row, "Estado", overdue ? Palette.Danger : Palette.Warning);
        }

        _pendingTitle.Text = result.Value.Count == 0
            ? "Cobros pendientes"
            : $"Cobros pendientes · {result.Value.Count}";
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
                var index = _paymentsGrid.Rows.Add(
                    Format.Date(payment.PaymentDate),
                    Format.Money(payment.AmountCents),
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
        if (Ui.Selected<ScheduledCharge>(_pendingGrid) is not { } charge)
        {
            Notify("Selecciona un cobro pendiente.", true);
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
        if (Ui.Selected<ScheduledCharge>(_pendingGrid) is not { } charge)
        {
            Notify("Selecciona un cobro pendiente.", true);
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
        if (Ui.Selected<ScheduledCharge>(_pendingGrid) is not { } charge)
        {
            Notify("Selecciona un cobro pendiente.", true);
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

        var subscriptions = await Context.Services.Subscriptions.GetAllAsync();
        if (Failed(subscriptions))
        {
            return;
        }

        var created = 0;
        var limit = DateOnly.FromDateTime(until.Value);
        foreach (var sub in subscriptions.Value.Where(s => s.IsActive))
        {
            var res = await Context.Services.Charges.GenerateAsync(new GenerateChargesRequest(sub.Id, limit));
            if (res.IsSuccess)
            {
                created += res.Value.Count;
            }
        }

        Notify(created == 0 ? "No había cobros nuevos por generar." : $"{created} cobros generados.");
        await DataChangedAsync(DataChangeKind.Charges);
    }

    private async Task CancelPaymentAsync()
    {
        if (Ui.Selected<Payment>(_paymentsGrid) is not { } payment)
        {
            Notify("Selecciona un pago registrado.", true);
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

        var result = await Context.Services.Payments.CancelAsync(
            new CancelPaymentRequest(payment.Id, reason.Text.Trim()));

        if (Failed(result))
        {
            return;
        }

        Notify("Pago anulado.");
        await DataChangedAsync(DataChangeKind.Payments | DataChangeKind.Charges);
    }
}
