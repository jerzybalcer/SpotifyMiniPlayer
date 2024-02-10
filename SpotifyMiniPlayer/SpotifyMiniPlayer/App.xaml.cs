using Microsoft.Win32;
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
        MessageBox.Show(e.Exception.Message + '\n' + e.Exception.StackTrace, "Application Unexpected Error");
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        SetWorkingDirectory();
        SetLaunchOnSystemStartup();
    }

    private static void SetWorkingDirectory()
    {
        var executableDirectory = Path.GetDirectoryName(Environment.ProcessPath);

        Directory.SetCurrentDirectory(executableDirectory!);
    }

    private static void SetLaunchOnSystemStartup()
    {
        RegistryKey? registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        registryKey!.SetValue("SpotifyMiniPlayer", Environment.ProcessPath!);
    }
}
