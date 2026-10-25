using SubTrack.Infrastructure;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.WinForms;

static class Program
{
    /// <summary>
    /// F9 — Arranque: preparar SQLite, migrar y abrir la ventana principal.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) =>
            MessageBox.Show(
                $"Error inesperado:{Environment.NewLine}{args.Exception.Message}",
                "SubTrack",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

        try
        {
            var options = SubTrackDatabaseOptions.CreateDefault();

            new SqliteDatabaseInitializer(options)
                .InitializeAsync()
                .GetAwaiter()
                .GetResult();

            var migration = new SqliteDatabaseMigrator(options)
                .MigrateAsync()
                .GetAwaiter()
                .GetResult();

            if (!migration.Success)
            {
                MessageBox.Show(
                    "No se pudo preparar la base de datos.",
                    "SubTrack",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            var services = new SubTrackServices(options);

            Application.Run(new MainForm(services, options));
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"No se pudo iniciar SubTrack:{Environment.NewLine}{exception.Message}",
                "SubTrack",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
