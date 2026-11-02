using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Infrastructure.Persistence;
using SubTrack.WinForms.Theme;
using SubTrack.WinForms.UI;

namespace SubTrack.WinForms.Views;

internal sealed class SettingsView : ViewBase
{
    private readonly DataGridView _categoriesGrid = Ui.Grid(("Categoría", 70f), ("Estado", 30f));
    private readonly DataGridView _historyGrid = Ui.Grid(("Fecha", 34f), ("Anterior", 33f), ("Nuevo", 33f));
    private readonly Panel _scrollHost = new() { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Palette.Canvas };
    private readonly TableLayoutPanel _rootLayout = new()
    {
        ColumnCount = 2,
        RowCount = 1,
        BackColor = Palette.Canvas,
        Margin = Padding.Empty,
        Padding = Padding.Empty
    };
    private FlatButton? _clearOpeningButton;
    private Control? _categoriesPanel;
    private Control? _leftPanel;
    private Control? _sidePanel;
    private Control? _maintenancePanel;

    private readonly Label _categoriesSummary = new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleRight,
        Font = Palette.Font(8.5f),
        ForeColor = Palette.Muted,
        Margin = new Padding(0)
    };

    private readonly Label _openingValue = new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        Font = Palette.Font(20f, FontStyle.Bold),
        ForeColor = Palette.PrimaryDeep,
        TextAlign = ContentAlignment.MiddleLeft,
        Text = "Sin configurar",
        Margin = new Padding(0)
    };

    private readonly Label _openingDate = new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        Font = Palette.Font(8.5f),
        ForeColor = Palette.Muted,
        TextAlign = ContentAlignment.MiddleLeft,
        Text = "—",
        Margin = new Padding(0)
    };

    private readonly Label _maintenanceStatus = new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        Font = Palette.Font(8.25f),
        ForeColor = Palette.Muted,
        TextAlign = ContentAlignment.MiddleLeft,
        Text = "—",
        Margin = new Padding(0)
    };

    private IReadOnlyList<ExpenseCategory> _categories = Array.Empty<ExpenseCategory>();
    private bool _openingConfigured;
    private long _openingCents;
    private DateOnly? _openingStart;

    public SettingsView(ViewContext context)
        : base(context)
    {
    }

    public override string Title => "Configuración";

    public override string Subtitle => "Categorías, saldo inicial y respaldo local de tus datos.";

    protected override void Build()
    {
        _categoriesPanel = BuildCategories();
        _maintenancePanel = BuildMaintenance();
        _leftPanel = BuildLeft();
        _sidePanel = BuildSide();

        _rootLayout.Controls.Add(_leftPanel, 0, 0);
        _rootLayout.Controls.Add(_sidePanel, 1, 0);
        _scrollHost.Controls.Add(_rootLayout);
        _scrollHost.SizeChanged += (_, _) => UpdateResponsiveLayout();

        Controls.Add(_scrollHost);
        UpdateResponsiveLayout();
    }

    protected override async Task LoadAsync()
    {
        await LoadCategoriesAsync();
        await LoadOpeningAsync();
        await LoadMaintenanceAsync();
    }

    private Control BuildCategories()
    {
        var card = new Card
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 8, 18, 10),
            Margin = new Padding(0, 0, 14, 0)
        };

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 8, 0, 0),
            Margin = new Padding(0)
        };

        actions.Controls.Add(Ui.Compact(Ui.Action("Nueva", NewCategoryAsync, ButtonKind.Primary)));
        actions.Controls.Add(Ui.Compact(Ui.Action("Renombrar", RenameCategoryAsync)));
        actions.Controls.Add(Ui.Compact(Ui.Action("Desactivar", DeactivateCategoryAsync)));
        actions.Controls.Add(Ui.Compact(Ui.Action("Eliminar", DeleteCategoryAsync, ButtonKind.Danger)));

        card.Controls.Add(_categoriesGrid);
        card.Controls.Add(actions);
        card.Controls.Add(Ui.Header("Categorías de gasto", _categoriesSummary));

        _categoriesGrid.CellDoubleClick += async (_, args) =>
        {
            if (args.RowIndex >= 0)
            {
                await RenameCategoryAsync();
            }
        };

        Ui.Align(_categoriesGrid, "Estado");

        return card;
    }

    private Control BuildSide()
    {
        var side = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Palette.Canvas,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };

        var scale = DeviceDpi / 96f;
        side.RowStyles.Add(new RowStyle(SizeType.Absolute, 180 * scale));
        side.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        side.Controls.Add(BuildOpening(), 0, 0);
        side.Controls.Add(BuildHistory(), 0, 1);

        return side;
    }

    private Control BuildLeft()
    {
        var left = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Palette.Canvas,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        left.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
        left.Controls.Add(_categoriesPanel!, 0, 0);
        left.Controls.Add(_maintenancePanel!, 0, 1);
        _categoriesPanel!.Margin = new Padding(0, 0, 0, 14);
        _maintenancePanel!.Margin = Padding.Empty;

        return left;
    }

    private void UpdateResponsiveLayout()
    {
        if (_leftPanel is null || _sidePanel is null)
        {
            return;
        }

        var viewportWidth = _scrollHost.ClientSize.Width;
        var viewportHeight = _scrollHost.ClientSize.Height;
        var isWide = viewportWidth >= 960;
        var scale = _scrollHost.DeviceDpi / 96f;
        var sideMinimumHeight = (int)Math.Ceiling(490 * scale);
        var categoryHeight = Math.Max(
            (int)(320 * scale),
            Math.Min((int)(420 * scale), (int)(viewportHeight * 0.55)));
        var rootHeight = isWide
            ? Math.Max(viewportHeight, sideMinimumHeight)
            : categoryHeight + sideMinimumHeight;
        var needsVerticalScroll = rootHeight > viewportHeight;
        var rootWidth = Math.Max(
            0,
            viewportWidth - (needsVerticalScroll ? SystemInformation.VerticalScrollBarWidth : 0));

        _rootLayout.SuspendLayout();
        _scrollHost.SuspendLayout();

        _rootLayout.ColumnCount = isWide ? 2 : 1;
        _rootLayout.RowCount = isWide ? 1 : 2;
        _rootLayout.ColumnStyles.Clear();
        _rootLayout.RowStyles.Clear();

        if (isWide)
        {
            _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            _rootLayout.SetCellPosition(_sidePanel, new TableLayoutPanelCellPosition(1, 0));
        }
        else
        {
            _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, categoryHeight + (int)(190 * scale)));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, sideMinimumHeight));
            _rootLayout.SetCellPosition(_sidePanel, new TableLayoutPanelCellPosition(0, 1));
        }

        _rootLayout.SetCellPosition(_leftPanel, new TableLayoutPanelCellPosition(0, 0));
        _rootLayout.Location = Point.Empty;
        _rootLayout.Size = new Size(rootWidth, rootHeight);
        _scrollHost.AutoScrollMinSize = new Size(rootWidth, rootHeight);

        _scrollHost.ResumeLayout(true);
        _rootLayout.ResumeLayout(true);
    }

    private Control BuildOpening()
    {
        var card = new Card
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 14, 20, 14),
            Margin = new Padding(0, 0, 0, 14)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Palette.Surface,
            Margin = new Padding(0)
        };

        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var buttonRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Padding = new Padding(0, 8, 0, 0),
            Margin = new Padding(0)
        };

        buttonRow.Controls.Add(Ui.Compact(Ui.Action("Configurar / corregir", ConfigureOpeningAsync)));
        _clearOpeningButton = Ui.Compact(Ui.Action(
            "Limpiar saldo inicial",
            ClearOpeningAsync,
            ButtonKind.Danger));
        _clearOpeningButton.Enabled = false;
        buttonRow.Controls.Add(_clearOpeningButton);

        layout.Controls.Add(Ui.Caption("SALDO INICIAL"), 0, 0);
        layout.Controls.Add(_openingValue, 0, 1);
        layout.Controls.Add(_openingDate, 0, 2);
        layout.Controls.Add(buttonRow, 0, 3);

        card.Controls.Add(layout);

        return card;
    }

    private Control BuildHistory()
    {
        var card = new Card
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 8, 18, 10),
            Margin = new Padding(0, 0, 0, 14)
        };

        card.Controls.Add(_historyGrid);
        card.Controls.Add(Ui.Header("Historial del saldo inicial"));

        Ui.Align(_historyGrid, "Anterior", "Nuevo");

        return card;
    }

    private Control BuildMaintenance()
    {
        var card = new Card
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 8, 18, 10),
            Margin = new Padding(0)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Palette.Surface,
            Margin = new Padding(0)
        };

        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = true,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };

        actions.Controls.Add(Ui.Compact(Ui.Action("Crear respaldo", CreateBackupAsync)));
        actions.Controls.Add(Ui.Compact(Ui.Action("Restaurar respaldo", RestoreBackupAsync)));
        actions.Controls.Add(Ui.Compact(Ui.Action("Verificar integridad", VerifyAsync)));
        actions.Controls.Add(Ui.Compact(Ui.Action("Optimizar", OptimizeAsync)));

        var hint = new Label
        {
            Text = "El respaldo es una copia consistente de la base SQLite en tu equipo. Verificar y optimizar no modifican tus datos.",
            Dock = DockStyle.Fill,
            Font = Palette.Font(8.25f),
            ForeColor = Palette.Muted,
            Margin = new Padding(0, 8, 0, 4)
        };

        layout.Controls.Add(actions, 0, 0);
        layout.Controls.Add(hint, 0, 1);
        layout.Controls.Add(_maintenanceStatus, 0, 2);

        card.Controls.Add(layout);
        card.Controls.Add(Ui.Header(
            "Sistema y mantenimiento",
            Ui.Compact(Ui.Action("Acerca de SubTrack", ShowAboutAsync))));

        return card;
    }

    private Task ShowAboutAsync()
    {
        using var form = new AboutForm();
        form.ShowDialog(FindForm());
        return Task.CompletedTask;
    }

    private async Task LoadCategoriesAsync()
    {
        var result = await Context.Services.Categories.GetAllAsync(includeInactive: true);

        if (Failed(result))
        {
            return;
        }

        _categories = result.Value;
        _categoriesGrid.Rows.Clear();

        foreach (var category in result.Value)
        {
            var index = _categoriesGrid.Rows.Add(
                category.Name,
                category.IsActive ? "● Activa" : "● Inactiva");

            var row = _categoriesGrid.Rows[index];
            row.Tag = category;

            Ui.Tint(row, "Estado", category.IsActive ? Palette.PrimaryDeep : Palette.Muted);

            if (!category.IsActive)
            {
                row.Cells["Categoría"].Style.ForeColor = Palette.Muted;
            }
        }

        _categoriesSummary.Text = $"{result.Value.Count(category => category.IsActive)} activas · {result.Value.Count} en total";
    }

    private async Task LoadOpeningAsync()
    {
        var result = await Context.Services.OpeningBalance.GetAsync();

        if (result.IsSuccess)
        {
            _openingConfigured = true;
            _openingCents = result.Value.AmountCents;
            _openingStart = result.Value.StartDate;
            _openingValue.Text = Format.Currency(result.Value.AmountCents);
            _openingDate.Text = $"Vigente desde {Format.Date(result.Value.StartDate)}";
        }
        else
        {
            _openingConfigured = false;
            _openingCents = 0;
            _openingStart = null;
            _openingValue.Text = "Sin configurar";
            _openingDate.Text = "Configura tu punto de partida para que el saldo sea exacto.";
        }

        if (_clearOpeningButton is not null)
        {
            _clearOpeningButton.Enabled = _openingConfigured;
        }

        var history = await Context.Services.OpeningBalance.GetCorrectionsAsync();

        _historyGrid.Rows.Clear();

        if (!history.IsSuccess)
        {
            return;
        }

        foreach (var correction in history.Value)
        {
            _historyGrid.Rows.Add(
                Format.Stamp(correction.ModifiedAt),
                Format.Money(correction.PreviousAmountCents),
                Format.Money(correction.NewAmountCents));
        }
    }

    private async Task LoadMaintenanceAsync()
    {
        try
        {
            var service = new SqliteBackupService(Context.Options);
            var backups = await service.ListBackupsAsync();

            _maintenanceStatus.Text = backups.Count == 0
                ? "Sin respaldos todavía"
                : $"Último respaldo: {backups[0].CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm} · {backups[0].FileName}";
        }
        catch (Exception exception)
        {
            _maintenanceStatus.Text = $"No se pudieron leer los respaldos: {exception.Message}";
        }
    }

    private async Task NewCategoryAsync()
    {
        using var dialog = new FormDialog("Nueva categoría", "Agrupa tus gastos para entender en qué se va el dinero.", "Crear categoría");
        var name = dialog.AddText("Nombre");

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        var result = await Context.Services.Categories.CreateAsync(
            new CreateExpenseCategoryRequest(name.Text.Trim()));

        if (Failed(result))
        {
            return;
        }

        Notify($"Categoría creada: {result.Value.Name}.");
        await DataChangedAsync(DataChangeKind.Categories);
    }

    private async Task RenameCategoryAsync()
    {
        if (SelectedCategory() is not { } category)
        {
            return;
        }

        using var dialog = new FormDialog("Renombrar categoría", "El historial de gastos conserva la categoría anterior.", "Guardar nombre");
        var name = dialog.AddText("Nombre", category.Name);

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        var result = await Context.Services.Categories.UpdateAsync(
            new UpdateExpenseCategoryRequest(category.Id, name.Text.Trim()));

        if (Failed(result))
        {
            return;
        }

        Notify("Categoría renombrada.");
        await DataChangedAsync(DataChangeKind.Categories);
    }

    private async Task DeactivateCategoryAsync()
    {
        if (SelectedCategory() is not { } category)
        {
            return;
        }

        var confirmed = Confirm.Ask(
            FindForm(),
            "Desactivar categoría",
            $"¿Desactivar «{category.Name}»? Dejará de aparecer al registrar gastos nuevos; el historial se conserva.",
            "Desactivar");

        if (!confirmed)
        {
            return;
        }

        var result = await Context.Services.Categories.DeactivateAsync(category.Id);

        if (Failed(result))
        {
            return;
        }

        Notify("Categoría desactivada.");
        await DataChangedAsync(DataChangeKind.Categories);
    }

    private async Task DeleteCategoryAsync()
    {
        if (SelectedCategory() is not { } category)
        {
            return;
        }

        var confirmed = Confirm.Ask(
            FindForm(),
            "Eliminar categoría",
            $"¿Eliminar «{category.Name}»? Esta acción no se puede deshacer y fallará si tiene gastos asociados.",
            "Eliminar",
            danger: true);

        if (!confirmed)
        {
            return;
        }

        var result = await Context.Services.Categories.DeleteAsync(category.Id);

        if (Failed(result))
        {
            return;
        }

        Notify("Categoría eliminada.");
        await DataChangedAsync(DataChangeKind.Categories);
    }

    private async Task ConfigureOpeningAsync()
    {
        using var dialog = new FormDialog(
            _openingConfigured ? "Corregir saldo inicial" : "Configurar saldo inicial",
            _openingConfigured
                ? "El importe reemplazará el saldo inicial vigente. Los movimientos registrados se conservan y el cambio queda en el historial."
                : "Es el punto de partida de tu saldo: no se registra como ingreso.",
            "Guardar saldo");

        var amount = dialog.AddMoney("Saldo", _openingCents / 100m);
        var date = dialog.AddDate(
            "Vigente desde",
            _openingStart?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today);

        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
        {
            return;
        }

        var cents = Format.Cents(amount.Value);
        var startDate = DateOnly.FromDateTime(date.Value);

        var result = _openingConfigured
            ? await Context.Services.OpeningBalance.CorrectAsync(new CorrectOpeningBalanceRequest(cents, startDate))
            : await Context.Services.OpeningBalance.SetAsync(new SetOpeningBalanceRequest(cents, startDate));

        if (Failed(result))
        {
            return;
        }

        Notify("Saldo inicial guardado.");
        await DataChangedAsync(DataChangeKind.OpeningBalance);
    }

    private async Task ClearOpeningAsync()
    {
        var confirmed = Confirm.Ask(
            FindForm(),
            "Limpiar saldo inicial",
            "El saldo inicial vigente se establecerá en $0.00. Los ingresos, gastos, pagos y demás movimientos se conservarán.",
            "Limpiar saldo",
            danger: true);

        if (!confirmed)
        {
            return;
        }

        var current = await Context.Services.OpeningBalance.GetAsync();
        if (Failed(current))
        {
            return;
        }

        var result = await Context.Services.OpeningBalance.CorrectAsync(
            new CorrectOpeningBalanceRequest(0, current.Value.StartDate));

        if (Failed(result))
        {
            return;
        }

        Notify("Saldo inicial establecido en $0.00.");
        await DataChangedAsync(DataChangeKind.OpeningBalance);
    }

    private async Task CreateBackupAsync()
    {
        using (BeginOperation())
        {
            try
            {
                var service = new SqliteBackupService(Context.Options);
                var result = await service.CreateBackupAsync();

                Notify($"Respaldo creado: {Path.GetFileName(result.BackupPath)}.");
                await LoadMaintenanceAsync();
            }
            catch (Exception exception)
            {
                Notify($"No se pudo crear el respaldo: {exception.Message}", true);
            }
        }
    }

    private async Task RestoreBackupAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Seleccionar copia de seguridad para restaurar",
            Filter = "Base de datos SQLite (*.db)|*.db|Todos los archivos (*.*)|*.*",
            InitialDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SubTrack",
                "backups")
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        var confirmation = MessageBox.Show(
            $"Esta acción reemplazará la base de datos actual con la copia seleccionada:{Environment.NewLine}{Path.GetFileName(dialog.FileName)}{Environment.NewLine}{Environment.NewLine}Se creará una copia de seguridad preventiva antes de restaurar.{Environment.NewLine}¿Deseas continuar?",
            "Confirmar restauración de datos",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirmation != DialogResult.Yes)
        {
            return;
        }

        using (BeginOperation())
        {
            try
            {
                var service = new SqliteBackupService(Context.Options);
                var verification = await service.VerifyBackupAsync(dialog.FileName);

                if (!verification.IsValid)
                {
                    Notify($"El archivo no es una copia válida de SubTrack: {verification.FailureReason}", true);
                    return;
                }

                var result = await service.RestoreBackupAsync(dialog.FileName, keepSafetyBackup: true);

                Notify($"Base de datos restaurada correctamente desde {Path.GetFileName(result.SourceBackupPath)}.");
                await DataChangedAsync(
                    DataChangeKind.Subscriptions
                    | DataChangeKind.Charges
                    | DataChangeKind.Payments
                    | DataChangeKind.Expenses
                    | DataChangeKind.Incomes
                    | DataChangeKind.Categories
                    | DataChangeKind.OpeningBalance);
            }
            catch (Exception exception)
            {
                Notify($"Error al restaurar el respaldo: {exception.Message}", true);
            }
        }
    }
    private async Task VerifyAsync()
    {
        try
        {
            var service = new SqliteMaintenanceService(Context.Options);
            var report = await service.CheckIntegrityAsync();

            if (report.IsHealthy)
            {
                Notify("Integridad verificada: la base está sana.");
            }
            else
            {
                Notify(
                    $"Se encontraron {report.ForeignKeyViolations.Count} problemas de integridad. Revisa el detalle en los registros.",
                    true);
            }
        }
        catch (Exception exception)
        {
            Notify($"No se pudo verificar la base: {exception.Message}", true);
        }
    }

    private async Task OptimizeAsync()
    {
        try
        {
            var service = new SqliteMaintenanceService(Context.Options);
            await service.OptimizeAsync();

            Notify("Base optimizada.");
        }
        catch (Exception exception)
        {
            Notify($"No se pudo optimizar la base: {exception.Message}", true);
        }
    }

    private ExpenseCategory? SelectedCategory()
    {
        var category = Ui.Selected<ExpenseCategory>(_categoriesGrid);

        if (category is null)
        {
            Notify("Selecciona una categoría.", true);
        }

        return category;
    }
}
