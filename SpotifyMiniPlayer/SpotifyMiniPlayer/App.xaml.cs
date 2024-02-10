using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace SpotifyMiniPlayer;

public partial class App : Application
{
    public App() : base()
    {
        Dispatcher.UnhandledException += OnUnhandledException;
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(e.Exception.StackTrace, e.Exception.Message);
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        var executableDirectory = Path.GetDirectoryName(Environment.ProcessPath);

        Directory.SetCurrentDirectory(executableDirectory!);
    }
}
