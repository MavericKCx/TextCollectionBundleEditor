namespace TextCollectionBundleEditor;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        using (var splash = new SplashForm())
            Application.Run(splash);

        Application.Run(new MainForm());
    }
}
