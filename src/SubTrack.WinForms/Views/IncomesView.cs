using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Domain.Enums;
using SubTrack.WinForms.Theme;
using SubTrack.WinForms.UI;

namespace SubTrack.WinForms.Views;

internal sealed class IncomesView : ViewBase
{
    private static readonly (IncomeType Value, string Text)[] IncomeTypes =
    {
        (IncomeType.Monthly, "Mensual"),
        (IncomeType.Biweekly, "Quincenal"),
        (IncomeType.Extraordinary, "Extraordinario")
    };

    private static readonly (IncomeType Value, string Text)[] TemplateTypes =
    {
        (IncomeType.Monthly, "Mensual"),
        (IncomeType.Biweekly, "Quincenal")
    };

    private static readonly (IncomeStatus? Value, string Text)[] Statuses =
    {
        (null, "Todos los estados"),
        (IncomeStatus.Planned, "Previstos"),
        (IncomeStatus.Confirmed, "Confirmados"),
        (IncomeStatus.Cancelled, "Anulados")
    };

    private readonly FlatButton _tabIncomes = new("Ingresos", ButtonKind.Primary);
    private readonly FlatButton _tabTemplates = new("Plantillas", ButtonKind.Ghost);

    private readonly Panel _incomesHost = new() { Dock = DockStyle.Fill, BackColor = Palette.Canvas };
    private readonly Panel _templatesHost = new() { Dock = DockStyle.Fill, BackColor = Palette.Canvas, Visible = false };

    private readonly CheckBox _useRange = new()
    {
        Text = "Rango de fechas",
        AutoSize = true,
        Font = Palette.Font(9f),
        ForeColor = Palette.Muted,
        Margin = new Padding(0, 11, 12, 0)
    };

    private readonly DateTimePicker _from = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy",
        Width = 120,
        Enabled = false,
        Font = Palette.Font(9.5f),
        Margin = new Padding(0, 7, 8, 0)
    };

    private readonly DateTimePicker _to = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy",
        Width = 120,
        Enabled = false,
        Font = Palette.Font(9.5f),
        Margin = new Padding(0, 7, 8, 0)
    };

    private readonly ComboBox _status = Combo.Create(Statuses, null, 160);

    private readonly DataGridView _grid = Ui.Grid(
        ("Previsto", 14f),
        ("Real", 14f),
        ("Concepto", 32f),
        ("Importe", 14f),
        ("Tipo", 12f),
        ("Estado", 14f));

    private readonly Pager _pager = new();

    private readonly Label _summary = new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
        Font = Palette.Font(8.5f),
        ForeColor = Palette.Muted,
        Margin = new Padding(0)
    };

    private readonly DataGridView _templatesGrid = Ui.Grid(
        ("Concepto", 34f),
        ("Importe", 16f),
        ("Frecuencia", 16f),
        ("Día", 12f),
        ("Inicio", 12f),
        ("Estado", 10f));

    private readonly Label _templatesSummary = new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
        Font = Palette.Font(8.5f),
        ForeColor = Palette.Muted,
        Margin = new Padding(0)
    };

    private int _page = 1;
    private int _totalPages = 1;
    private bool _suppress;

    public IncomesView(ViewContext context)
        : base(context)
    {
    }

    public override string Title => "Ingresos";

    public override string Subtitle => "Ingresos confirmados, previstos y plantillas recurrentes.";

    protected override void Build()
    {
        var today = DateTime.Today;

        _from.Value = new DateTime(today.Year, today.Month, 1);
        _to.Value = today;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Palette.Canvas,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var tabsCard = new Card
        {
            Size = new Size(232, 44),
            Radius = 12,
            Padding = new Padding(4),
            Location = new Point(0, 0)
        };

        var tabsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        _tabIncomes.Size = new Size(108, 34);
        _tabIncomes.MinimumSize = new Size(108, 34);
        _tabIncomes.Margin = new Padding(0, 0, 4, 0);

        _tabTemplates.Size = new Size(108, 34);
        _tabTemplates.MinimumSize = new Size(108, 34);
        _tabTemplates.Margin = new Padding(0);

        tabsFlow.Controls.Add(_tabIncomes);
        tabsFlow.Controls.Add(_tabTemplates);
        tabsCard.Controls.Add(tabsFlow);

        var tabsHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Palette.Canvas
        };

        tabsHost.Controls.Add(tabsCard);

        _tabIncomes.Click += (_, _) => SelectTab(true);
        _tabTemplates.Click += (_, _) => SelectTab(false);

        _incomesHost.Controls.Add(BuildIncomesPanel());
        _templatesHost.Controls.Add(BuildTemplatesPanel());

        root.Controls.Add(tabsHost, 0, 0);
        root.Controls.Add(_incomesHost, 0, 1);
        root.Controls.Add(_templatesHost, 0, 1);

        Controls.Add(root);
    }

    protected override async Task LoadAsync()
    {
        await LoadIncomesAsync();
        await LoadTemplatesAsync();
    }

    private Control BuildIncomesPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Palette.Canvas,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var filters = new Card
        {
            Dock = DockStyle.Top,
            Height = 64,
            Padding = new Padding(16, 8, 16, 8),
            Margin = new Padding(0, 0, 0, 14)
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            AutoSize = false,
            Margin = new Padding(0)
        };

        var dash = Ui.Label("a", 9f, Palette.Muted);
        dash.Margin = new Padding(0, 13, 8, 0);

        var clear = Ui.Compact(new FlatButton("Limpiar", ButtonKind.Ghost));
        clear.Margin = new Padding(4, 7, 0, 0);

        flow.Controls.Add(_useRange);
        flow.Controls.Add(_from);
        flow.Controls.Add(dash);
        flow.Controls.Add(_to);
        flow.Controls.Add(_status);
        flow.Controls.Add(clear);

        filters.Controls.Add(flow);

        var toolbar = Ui.Toolbar(
            Array.Empty<Control>(),
            new Control[]
            {
                Ui.Action("Nuevo ingreso", NewAsync, ButtonKind.Primary),
                Ui.Action("Editar", EditAsync),
                Ui.Action("Confirmar", ConfirmAsync),
                Ui.Action("Anular", CancelAsync),
                Ui.Menu("Más", ("Ver correcciones", CorrectionsAsync))
            });

        var card = new Card
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 8, 18, 10)
        };

        var pagerHost = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0),
            Margin = new Padding(0)
        };

        pagerHost.Controls.Add(_pager);

        card.Controls.Add(_grid);
        card.Controls.Add(pagerHost);
        card.Controls.Add(Ui.Header("Ingresos", _summary));

        panel.Controls.Add(toolbar, 0, 0);
        panel.Controls.Add(filters, 0, 1);
        panel.Controls.Add(card, 0, 2);

        Ui.Align(_grid, "Importe");

        _useRange.CheckedChanged += OnFilterChanged;
        _from.ValueChanged += OnFilterChanged;
        _to.ValueChanged += OnFilterChanged;
        _status.SelectedIndexChanged += OnFilterChanged;

        clear.Click += async (_, _) => await ClearFiltersAsync();

        _grid.CellDoubleClick += async (_, args) =>
        {
            if (args.RowIndex >= 0)
            {
                await EditAsync();
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

        return panel;
    }

    private Control BuildTemplatesPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Palette.Canvas,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbar = Ui.Toolbar(
            Array.Empty<Control>(),
            new Control[]
            {
                Ui.Action("Nueva plantilla", NewTemplateAsync, ButtonKind.Primary),
                Ui.Action("Generar previstos", GenerateTemplateAsync)
            });

        var card = new Card
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 8, 18, 12)
        };

        card.Controls.Add(_templatesGrid);
        card.Controls.Add(Ui.Header("Plantillas recurrentes", _templatesSummary));

        panel.Controls.Add(toolbar, 0, 0);
        panel.Controls.Add(card, 0, 1);

        Ui.Align(_templatesGrid, "Importe");

        _templatesGrid.CellDoubleClick += async (_, args) =>
        {
            if (args.RowIndex >= 0)
            {
                await GenerateTemplateAsync();
            }
        };

        return panel;
    }

    private void SelectTab(bool incomes)
    {
        _incomesHost.Visible = incomes;
        _templatesHost.Visible = !incomes;

        _tabIncomes.Kind = incomes ? ButtonKind.Primary : ButtonKind.Ghost;
        _tabTemplates.Kind = incomes ? ButtonKind.Ghost : ButtonKind.Primary;

        _tabIncomes.Invalidate();
        _tabTemplates.Invalidate();
    }

    private async Task LoadIncomesAsync()
    {
        while (true)
        {
            var request = new IncomeHistoryRequest(
                new PageRequest(_page, 50),
                _useRange.Checked ? DateOnly.FromDateTime(_from.Value) : null,
                _useRange.Checked ? DateOnly.FromDateTime(_to.Value) : null,
                SelectedStatus());

            var result = await Context.Services.Incomes.GetHistoryAsync(request);

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

            _grid.Rows.Clear();

            foreach (var income in result.Value.Items)
            {
                var (text, color) = Ui.IncomeBadge(income.Status);

                var index = _grid.Rows.Add(
                    Format.Date(income.PlannedDate),
                    Format.Date(income.ActualDate),
                    income.Concept,
                    Format.Money(income.AmountCents),
                    Format.IncomeType(income.Type),
                    text);

                var row = _grid.Rows[index];
                row.Tag = income;

                Ui.Tint(row, "Estado", color);

                if (income.Status == IncomeStatus.Cancelled)
                {
                    row.Cells["Concepto"].Style.ForeColor = Palette.Muted;
                }
            }

            _summary.Text = $"{result.Value.TotalCount} movimientos";
            _pager.SetPage(result.Value.Page, result.Value.TotalPages);
            return;
        }
    }

    private async Task LoadTemplatesAsync()
    {
        var result = await Context.Services.Incomes.GetTemplatesAsync(includeInactive: true);

        if (Failed(result))
        {
            return;
        }

        _templatesGrid.Rows.Clear();

        foreach (var template in result.Value.OrderBy(item => item.Concept))
        {
            var day = template.Frequency == IncomeType.Biweekly && template.AnchorDay == 31
                ? "Último día"
                : template.AnchorDay.ToString();

            var index = _templatesGrid.Rows.Add(
                template.Concept,
                Format.Money(template.AmountCents),
                Format.IncomeType(template.Frequency),
                day,
                Format.Date(template.StartDate),
                template.IsActive ? "● Activa" : "● Inactiva");

            var row = _templatesGrid.Rows[index];
            row.Tag = template;

            Ui.Tint(row, "Estado", template.IsActive ? Palette.PrimaryDeep : Palette.Muted);
        }

        _templatesSummary.Text = $"{result.Value.Count(item => item.IsActive)} activas · {result.Value.Count} en total";
    }

    private async void OnFilterChanged(object? sender, EventArgs e)
    {
        if (_suppress)
        {
            return;
        }

        _from.Enabled = _useRange.Checked;
        _to.Enabled = _useRange.Checked;
        _page = 1;

        await RefreshAsync();
    }

    private async Task ClearFiltersAsync()
    {
        _suppress = true;

        var today = DateTime.Today;

        _useRange.Checked = false;
        _from.Value = new DateTime(today.Year, today.Month, 1);
        _to.Value = today;
        _status.SelectedIndex = 0;
        _from.Enabled = false;
        _to.Enabled = false;
        _suppress = false;

        _page = 1;
        await RefreshAsync();
    }

    private async Task NewAsync()
    {
        using var dialog = new FormDialog(
            "Nuevo ingreso",
            "Si lo marcas como recibido, afecta tu saldo de inmediato.",
            "Guardar ingreso");

        var concept = dialog.AddText("Concepto");
        var amount = dialog.AddMoney("Importe");
        var type = dialog.AddCombo("Tipo", IncomeTypes, IncomeType.Monthly);
        var planned = dialog.AddDate("Fecha prevista", DateTime.Today);
        var received = dialog.AddCheck("Ya lo recibí");

        var actual = dialog.AddDate("Fecha real", DateTime.Today, maxToday: true);
        actual.Enabled = false;

        received.CheckedChanged += (_, _) => actual.Enabled = received.Checked;

        type.SelectedIndexChanged += (_, _) =>
        {
            var extraordinary = FormDialog.Value<IncomeType>(type) == IncomeType.Extraordinary;
            received.Enabled = !extraordinary;

            if (extraordinary)
            {
                received.Checked = true;
                actual.Enabled = true;
            }
        };

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        var incomeType = FormDialog.Value<IncomeType>(type);
        var extraordinary = incomeType == IncomeType.Extraordinary;

        var result = await Context.Services.Incomes.CreateAsync(new CreateIncomeRequest(
            concept.Text.Trim(),
            Format.Cents(amount.Value),
            incomeType,
            extraordinary ? null : DateOnly.FromDateTime(planned.Value),
            received.Checked ? DateOnly.FromDateTime(actual.Value) : null,
            null,
            Guid.NewGuid()));

        if (Failed(result))
        {
            return;
        }

        Notify($"Ingreso registrado: {result.Value.Concept}.");
        await DataChangedAsync(DataChangeKind.Incomes);
    }

    private async Task EditAsync()
    {
        if (Selected() is not { } income)
        {
            return;
        }

        if (income.Status == IncomeStatus.Cancelled)
        {
            Notify("Un ingreso anulado ya no se puede editar.", true);
            return;
        }

        using var dialog = new FormDialog(
            "Editar ingreso",
            "La edición guarda el valor anterior en el historial de correcciones.",
            "Guardar cambios");

        var concept = dialog.AddText("Concepto", income.Concept);
        var amount = dialog.AddMoney("Importe", income.AmountCents / 100m);
        var planned = dialog.AddDate("Fecha prevista", income.PlannedDate.ToDateTime(TimeOnly.MinValue));

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        var result = await Context.Services.Incomes.UpdateAsync(new UpdateIncomeRequest(
            income.Id,
            concept.Text.Trim(),
            Format.Cents(amount.Value),
            DateOnly.FromDateTime(planned.Value)));

        if (Failed(result))
        {
            return;
        }

        Notify("Ingreso actualizado.");
        await DataChangedAsync(DataChangeKind.Incomes);
    }

    private async Task ConfirmAsync()
    {
        if (Selected() is not { } income)
        {
            return;
        }

        if (income.Status != IncomeStatus.Planned)
        {
            Notify("Solo los ingresos previstos se pueden confirmar.", true);
            return;
        }

        using var dialog = new FormDialog(
            "Confirmar ingreso",
            $"Se registrará «{income.Concept}» por {Format.Currency(income.AmountCents)}.",
            "Confirmar");

        var date = dialog.AddDate("Fecha real", DateTime.Today, maxToday: true);

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        var result = await Context.Services.Incomes.ConfirmAsync(
            new ConfirmIncomeRequest(income.Id, DateOnly.FromDateTime(date.Value)));

        if (Failed(result))
        {
            return;
        }

        Notify("Ingreso confirmado.");
        await DataChangedAsync(DataChangeKind.Incomes);
    }

    private async Task CancelAsync()
    {
        if (Selected() is not { } income)
        {
            return;
        }

        if (income.Status == IncomeStatus.Cancelled)
        {
            Notify("Este ingreso ya está anulado.", true);
            return;
        }

        using var dialog = new FormDialog(
            "Anular ingreso",
            $"«{income.Concept}» por {Format.Currency(income.AmountCents)}. El motivo es obligatorio y queda en el historial.",
            "Anular ingreso");

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

        var result = await Context.Services.Incomes.CancelAsync(
            new CancelIncomeRequest(income.Id, reason.Text.Trim()));

        if (Failed(result))
        {
            return;
        }

        Notify("Ingreso anulado.");
        await DataChangedAsync(DataChangeKind.Incomes);
    }

    private async Task CorrectionsAsync()
    {
        if (Selected() is not { } income)
        {
            return;
        }

        var result = await Context.Services.Incomes.GetCorrectionsAsync(income.Id);

        if (Failed(result))
        {
            return;
        }

        TableView.Show(
            FindForm(),
            "Correcciones",
            $"{income.Concept} · historial de cambios",
            new[]
            {
                ("Fecha", 20f),
                ("Concepto anterior", 22f),
                ("Concepto nuevo", 22f),
                ("Importe anterior", 18f),
                ("Importe nuevo", 18f)
            },
            result.Value.Select(correction => new[]
            {
                Format.Stamp(correction.ModifiedAt),
                correction.PreviousConcept ?? "—",
                correction.NewConcept ?? "—",
                correction.PreviousAmountCents is { } previous ? Format.Money(previous) : "—",
                correction.NewAmountCents is { } current ? Format.Money(current) : "—"
            }));
    }

    private async Task NewTemplateAsync()
    {
        using var dialog = new FormDialog(
            "Nueva plantilla",
            "La plantilla genera ingresos previstos individuales; tú los confirmas cuando los recibes.",
            "Crear plantilla");

        var concept = dialog.AddText("Concepto");
        var amount = dialog.AddMoney("Importe");
        var frequency = dialog.AddCombo("Frecuencia", TemplateTypes, IncomeType.Monthly);
        var day = dialog.AddNumber("Día de cobro", DateTime.Today.Day, 1, 31);
        var start = dialog.AddDate("Inicio", DateTime.Today);

        dialog.AddNote("En frecuencia quincenal el día debe ser 15 o 31 (31 = último día del mes).");

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        var type = FormDialog.Value<IncomeType>(frequency);
        var anchor = (int)day.Value;

        if (type == IncomeType.Biweekly && anchor != 15 && anchor != 31)
        {
            Notify("La plantilla quincenal solo admite día 15 o 31.", true);
            return;
        }

        var result = await Context.Services.Incomes.CreateTemplateAsync(new CreateIncomeTemplateRequest(
            concept.Text.Trim(),
            Format.Cents(amount.Value),
            type,
            anchor,
            DateOnly.FromDateTime(start.Value),
            Guid.NewGuid()));

        if (Failed(result))
        {
            return;
        }

        Notify($"Plantilla creada: {result.Value.Concept}.");
        await DataChangedAsync(DataChangeKind.Incomes);
    }

    private async Task GenerateTemplateAsync()
    {
        if (Ui.Selected<RecurringIncomeTemplate>(_templatesGrid) is not { } template)
        {
            Notify("Selecciona una plantilla.", true);
            return;
        }

        var today = DateTime.Today;
        var suggested = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

        if (suggested < today)
        {
            suggested = today;
        }

        using var dialog = new FormDialog(
            "Generar previstos",
            $"Se crearán ingresos previstos de «{template.Concept}» hasta la fecha indicada.",
            "Generar");

        var until = dialog.AddDate("Generar hasta", suggested);
        until.MaxDate = today.AddMonths(12);

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        var result = await Context.Services.Incomes.GeneratePlannedAsync(
            new GeneratePlannedIncomesRequest(template.Id, DateOnly.FromDateTime(until.Value)));

        if (Failed(result))
        {
            return;
        }

        Notify(result.Value.Count == 0
            ? "No había ingresos nuevos por generar."
            : $"{result.Value.Count} ingresos previstos generados.");

        await DataChangedAsync(DataChangeKind.Incomes);
    }

    private Income? Selected()
    {
        var income = Ui.Selected<Income>(_grid);

        if (income is null)
        {
            Notify("Selecciona un ingreso.", true);
        }

        return income;
    }

    private IncomeStatus? SelectedStatus() =>
        _status.SelectedItem is ComboItem<IncomeStatus?> item ? item.Value : null;
}
