using System;
using System.Threading;
using System.Windows;

namespace MultiScreenDock.App;

public static class Program
{
    private static Mutex? _singleInstanceMutex;

    [STAThread]
    public static void Main()
    {
        _singleInstanceMutex = new Mutex(true, "Local\\MultiScreenDock_Windows_Singleton", out bool createdNew);
        if (!createdNew)
        {
            return;
        }

        var app = new Application
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown
        };
        app.Startup += (_, _) => MultiScreenController.Shared.Start();
        app.Exit += (_, _) =>
        {
            MultiScreenController.Shared.Dispose();
            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();
        };
        app.Run();
    }
}
