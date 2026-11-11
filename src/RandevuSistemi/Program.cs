using RandevuSistemi.Arayuz;

namespace RandevuSistemi;

/// <summary>
/// Uygulamanın giriş noktası. Windows Forms ana penceresini açar.
/// </summary>
internal static class Program
{
    [STAThread] // Windows Forms uygulamaları tek iş parçacıklı (STA) modda çalışmalıdır
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.SystemAware);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Application.Run(new AnaForm());
    }
}
