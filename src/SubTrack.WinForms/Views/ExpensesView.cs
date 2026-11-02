using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Domain.Enums;
using SubTrack.WinForms.Theme;
using SubTrack.WinForms.UI;

namespace SubTrack.WinForms.Views;

internal sealed class ExpensesView : ViewBase
{
    private static readonly (ExpenseStatus? Value, string Text)[] Statuses =
    {
        (null, "Todos los estados"),
        (ExpenseStatus.Planned, "Previstos"),
        (ExpenseStatus.Confirmed, "Confirmados"),
        (ExpenseStatus.Cancelled, "Anulados")
    };

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

    private readonly ComboBox _category = Combo.Create(
        new List<(Guid? Value, string Text)> { (null, "Todas las categorías") },
        null,
        190);

    private readonly ComboBox _status = Combo.Create(Statuses, null, 160);

    private readonly DataGridView _grid = Ui.Grid(
        ("Fecha", 14f),
        ("Concepto", 38f),
        ("Categoría", 20f),
        ("Importe", 14f),
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

    private IReadOnlyList<ExpenseCategory> _categories = Array.Empty<ExpenseCategory>();
    private Dictionary<Guid, string> _categoryNames = new();
    private int _page = 1;
    private int _totalPages = 1;
    private bool _suppress;

    public ExpensesView(ViewContext context)
        : base(context)
    {
    }

    public override string Title => "Gastos";

    public override string Subtitle => "Gastos previstos, confirmados y su historial de correcciones.";

    protected override void Build()
    {
        var today = DateTime.Today;

        _from.Value = new DateTime(today.Year, today.Month, 1);
        _to.Value = today;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Palette.Canvas,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbar = Ui.Toolbar(
            Array.Empty<Control>(),
            new Control[]
            {
                Ui.Action("Nuevo gasto", NewAsync, ButtonKind.Primary),
                Ui.Action("Editar", EditAsync),
                Ui.Action("Confirmar", ConfirmAsync),
                Ui.Action("Anular", CancelAsync),
                Ui.Menu("Más", ("Ver correcciones", CorrectionsAsync))
            });

        var filters = new Card
        {
            Dock = DockStyle.Top,
            Height = 64,
            Padding = new Padding(16, 8, 16, 8),
            Margin = new Padding(0, 0, 0, 14)
        };

        var filterFlow = new FlowLayoutPanel
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

        filterFlow.Controls.Add(_useRange);
        filterFlow.Controls.Add(_from);
        filterFlow.Controls.Add(dash);
        filterFlow.Controls.Add(_to);
        filterFlow.Controls.Add(_category);
        filterFlow.Controls.Add(_status);
        filterFlow.Controls.Add(clear);

        filters.Controls.Add(filterFlow);

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
        card.Controls.Add(Ui.Header("Movimientos", _summary));

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(filters, 0, 1);
        root.Controls.Add(card, 0, 2);

        Ui.Align(_grid, "Importe");

        _useRange.CheckedChanged += OnFilterChanged;
        _from.ValueChanged += OnFilterChanged;
        _to.ValueChanged += OnFilterChanged;
        _category.SelectedIndexChanged += OnFilterChanged;
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

        Controls.Add(root);
    }

    protected override async Task LoadAsync()
    {
        var categories = await Context.Services.Categories.GetAllAsync(includeInactive: true);

        if (Failed(categories))
        {
            return;
        }

        _categories = categories.Value;
        _categoryNames = categories.Value.ToDictionary(category => category.Id, category => category.Name);

        RebuildCategoryFilter();

        await LoadPageAsync();
    }

    private async Task LoadPageAsync()
    {
        while (true)
        {
            var request = new ExpenseHistoryRequest(
                new PageRequest(_page, 50),
                _useRange.Checked ? DateOnly.FromDateTime(_from.Value) : null,
                _useRange.Checked ? DateOnly.FromDateTime(_to.Value) : null,
                SelectedCategory(),
                SelectedStatus());

            var result = await Context.Services.Expenses.GetHistoryAsync(request);

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

            foreach (var expense in result.Value.Items)
            {
                var name = _categoryNames.TryGetValue(expense.CategoryId, out var value) ? value : "—";
                var (text, color) = Ui.ExpenseBadge(expense.Status);

                var index = _grid.Rows.Add(
                    Format.Date(expense.ActualDate ?? expense.PlannedDate),
                    expense.Concept,
                    name,
                    Format.Money(expense.AmountCents),
                    text);

                var row = _grid.Rows[index];
                row.Tag = expense;

                Ui.Tint(row, "Estado", color);

                if (expense.Status == ExpenseStatus.Cancelled)
                {
                    row.Cells["Concepto"].Style.ForeColor = Palette.Muted;
                }
            }

            _summary.Text = $"{result.Value.TotalCount} movimientos";
            _pager.SetPage(result.Value.Page, result.Value.TotalPages);
            return;
        }
    }

    private void RebuildCategoryFilter()
    {
        _suppress = true;

        var selected = SelectedCategory();

        _category.Items.Clear();
        _category.Items.Add(new ComboItem<Guid?>(null, "Todas las categorías"));

        foreach (var category in _categories.OrderBy(item => item.Name))
        {
            _category.Items.Add(new ComboItem<Guid?>(category.Id, category.Name));
        }

        _category.SelectedIndex = 0;

        for (var index = 0; index < _category.Items.Count; index++)
        {
            if (_category.Items[index] is ComboItem<Guid?> item && item.Value == selected)
            {
                _category.SelectedIndex = index;
                break;
            }
        }

        _suppress = false;
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
        _category.SelectedIndex = 0;
        _status.SelectedIndex = 0;
        _from.Enabled = false;
        _to.Enabled = false;
        _suppress = false;

        _page = 1;
        await RefreshAsync();
    }

    private async Task NewAsync()
    {
        var active = _categories.Where(category => category.IsActive).ToList();

        if (active.Count == 0)
        {
            Notify("Crea al menos una categoría activa en Configuración.", true);
            return;
        }

        using var dialog = new FormDialog(
            "Nuevo gasto",
            "Si marcas «Ya realizado» el gasto queda confirmado y afecta tu saldo.",
            "Guardar gasto");

        var concept = dialog.AddText("Concepto");
        var amount = dialog.AddMoney("Importe");
        var category = dialog.AddCombo(
            "Categoría",
            active.Select(item => (item.Id, item.Name)).ToList(),
            active[0].Id);

        var planned = dialog.AddDate("Fecha prevista", DateTime.Today);
        var done = dialog.AddCheck("Ya realizado");

        var actual = dialog.AddDate("Fecha real", DateTime.Today, maxToday: true);
        actual.Enabled = false;
        done.CheckedChanged += (_, _) => actual.Enabled = done.Checked;

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        var result = await Context.Services.Expenses.CreateAsync(new CreateExpenseRequest(
            concept.Text.Trim(),
            FormDialog.Value<Guid>(category),
            Format.Cents(amount.Value),
            DateOnly.FromDateTime(planned.Value),
            done.Checked ? DateOnly.FromDateTime(actual.Value) : null,
            Guid.NewGuid()));

        if (Failed(result))
        {
            return;
        }

        Notify($"Gasto registrado: {result.Value.Concept}.");
        await DataChangedAsync(DataChangeKind.Expenses);
    }

    private async Task EditAsync()
    {
        if (Selected() is not { } expense)
        {
            return;
        }

        if (expense.Status == ExpenseStatus.Cancelled)
        {
            Notify("Un gasto anulado ya no se puede editar.", true);
            return;
        }

        var active = _categories.Where(category => category.IsActive).ToList();

        if (active.All(category => category.Id != expense.CategoryId)
            && _categoryNames.TryGetValue(expense.CategoryId, out var currentName))
        {
            active.Add(new ExpenseCategory
            {
                Id = expense.CategoryId,
                Name = currentName,
                IsActive = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        using var dialog = new FormDialog(
            "Editar gasto",
            "La edición guarda el valor anterior en el historial de correcciones.",
            "Guardar cambios");

        var concept = dialog.AddText("Concepto", expense.Concept);
        var amount = dialog.AddMoney("Importe", expense.AmountCents / 100m);
        var category = dialog.AddCombo(
            "Categoría",
            active.Select(item => (item.Id, item.Name)).ToList(),
            expense.CategoryId);

        var planned = dialog.AddDate("Fecha prevista", expense.PlannedDate.ToDateTime(TimeOnly.MinValue));

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        var result = await Context.Services.Expenses.UpdateAsync(new UpdateExpenseRequest(
            expense.Id,
            concept.Text.Trim(),
            FormDialog.Value<Guid>(category),
            Format.Cents(amount.Value),
            DateOnly.FromDateTime(planned.Value)));

        if (Failed(result))
        {
            return;
        }

        Notify("Gasto actualizado.");
        await DataChangedAsync(DataChangeKind.Expenses);
    }

    private async Task ConfirmAsync()
    {
        if (Selected() is not { } expense)
        {
            return;
        }

        if (expense.Status != ExpenseStatus.Planned)
        {
            Notify("Solo los gastos previstos se pueden confirmar.", true);
            return;
        }

        using var dialog = new FormDialog(
            "Confirmar gasto",
            $"Se registrará «{expense.Concept}» por {Format.Currency(expense.AmountCents)}.",
            "Confirmar");

        var date = dialog.AddDate("Fecha real", DateTime.Today, maxToday: true);

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        var result = await Context.Services.Expenses.ConfirmAsync(
            new ConfirmExpenseRequest(expense.Id, DateOnly.FromDateTime(date.Value)));

        if (Failed(result))
        {
            return;
        }

        Notify("Gasto confirmado.");
        await DataChangedAsync(DataChangeKind.Expenses);
    }

    private async Task CancelAsync()
    {
        if (Selected() is not { } expense)
        {
            return;
        }

        if (expense.Status == ExpenseStatus.Cancelled)
        {
            Notify("Este gasto ya está anulado.", true);
            return;
        }

        using var dialog = new FormDialog(
            "Anular gasto",
            $"«{expense.Concept}» por {Format.Currency(expense.AmountCents)}. El motivo es obligatorio y queda en el historial.",
            "Anular gasto");

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

        var result = await Context.Services.Expenses.CancelAsync(
            new CancelExpenseRequest(expense.Id, reason.Text.Trim()));

        if (Failed(result))
        {
            return;
        }

        Notify("Gasto anulado.");
        await DataChangedAsync(DataChangeKind.Expenses);
    }

    private async Task CorrectionsAsync()
    {
        if (Selected() is not { } expense)
        {
            return;
        }

        var result = await Context.Services.Expenses.GetCorrectionsAsync(expense.Id);

        if (Failed(result))
        {
            return;
        }

        TableView.Show(
            FindForm(),
            "Correcciones",
            $"{expense.Concept} · historial de cambios",
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

    private Expense? Selected()
    {
        var expense = Ui.Selected<Expense>(_grid);

        if (expense is null)
        {
            Notify("Selecciona un gasto.", true);
        }

        return expense;
    }

    private Guid? SelectedCategory() =>
        _category.SelectedItem is ComboItem<Guid?> item ? item.Value : null;

    private ExpenseStatus? SelectedStatus() =>
        _status.SelectedItem is ComboItem<ExpenseStatus?> item ? item.Value : null;
}
