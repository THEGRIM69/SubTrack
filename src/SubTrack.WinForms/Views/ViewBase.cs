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

internal sealed record ViewContext(
    SubTrackServices Services,
    SubTrackDatabaseOptions Options,
    Action<string, bool> Notify);

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
