using SubTrack.Core.Results;
using SubTrack.Infrastructure;
using SubTrack.Infrastructure.Persistence;
using SubTrack.WinForms.Theme;

namespace SubTrack.WinForms.Views;

internal interface IAppView
{
    string Title { get; }

    string Subtitle { get; }

    Task RefreshAsync();
}

[Flags]
internal enum DataChangeKind
{
    None = 0,
    Subscriptions = 1,
    Charges = 2,
    Payments = 4,
    Expenses = 8,
    Incomes = 16,
    Categories = 32,
    OpeningBalance = 64,
    Dashboard = 128
}

internal sealed record ViewContext(
    SubTrackServices Services,
    SubTrackDatabaseOptions Options,
    Action<string, bool> Notify,
    Func<IDisposable>? BeginOperation = null,
    Func<DataChangeKind, Task>? DataChanged = null);

/// <summary>
/// Base de las vistas: ciclo de vida Build/Load y manejo uniforme de errores.
/// La UI no calcula dinero ni habla con SQLite.
/// </summary>
internal abstract class ViewBase : UserControl, IAppView
{
    protected ViewBase(ViewContext context)
    {
        Context = context;
        Dock = DockStyle.Fill;
        BackColor = Palette.Canvas;
        DoubleBuffered = true;
        AutoScaleMode = AutoScaleMode.None;

        Build();
    }

    protected ViewContext Context { get; }

    protected IDisposable BeginOperation() =>
        Context.BeginOperation?.Invoke() ?? NullDisposable.Instance;

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();
        public void Dispose() { }
    }

    public abstract string Title { get; }

    public abstract string Subtitle { get; }

    public async Task RefreshAsync()
    {
        try
        {
            await LoadAsync();
        }
        catch (Exception exception)
        {
            Notify($"No se pudieron cargar los datos: {exception.Message}", true);
        }
    }

    protected abstract void Build();

    protected abstract Task LoadAsync();

    protected void Notify(string message, bool error = false) => Context.Notify(message, error);

    protected Task DataChangedAsync(DataChangeKind change) =>
        Context.DataChanged?.Invoke(change) ?? Task.CompletedTask;

    protected bool Failed(Result result)
    {
        if (result.IsSuccess)
        {
            return false;
        }

        Notify(Describe(result.Errors), true);
        return true;
    }

    protected bool Failed<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return false;
        }

        Notify(Describe(result.Errors), true);
        return true;
    }

    protected bool Failed<T>(Result<T> result, out T value)
    {
        if (result.IsSuccess)
        {
            value = result.Value;
            return false;
        }

        Notify(Describe(result.Errors), true);
        value = default!;
        return true;
    }

    protected static string Describe(IReadOnlyList<DomainError> errors) =>
        errors.Count == 0
            ? "No se pudo completar la operación."
            : string.Join(" · ", errors.Select(error => error.Message));
}
