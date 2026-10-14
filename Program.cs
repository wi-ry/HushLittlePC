namespace HushLittlePC;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, "HushLittlePC_SingleInstance", out bool isNew);
        if (!isNew) return;

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApp());
    }
}
