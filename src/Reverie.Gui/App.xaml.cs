using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Reverie.Gui;

public partial class App : Application
{
    private static readonly string LogPath =
        Path.Combine(AppContext.BaseDirectory, "audiomeet-crash.log");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        File.AppendAllText(LogPath, $"[UI] {DateTime.Now:O}\n{e.Exception}\n\n");
        MessageBox.Show(e.Exception.Message, "AudioMeet 错误", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        File.AppendAllText(LogPath, $"[Domain] {DateTime.Now:O}\n{e.ExceptionObject}\n\n");
    }
}
