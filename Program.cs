namespace HushLittlePC;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "HushLittlePC_SingleInstance", out bool isNew);
        if (!isNew) return;

        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => AppLog.Error("Unhandled UI-thread exception.", e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var exception = e.ExceptionObject as Exception
                ?? new Exception(e.ExceptionObject?.ToString() ?? "Unknown unhandled exception.");
            AppLog.Error("Unhandled application exception.", exception);
        };

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApp());
    }
}
