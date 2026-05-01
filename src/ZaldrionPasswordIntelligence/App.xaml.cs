using System;
using System.Windows;
using System.Windows.Threading;

namespace ZaldrionPasswordIntelligence;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            MessageBox.Show(args.ExceptionObject.ToString(), "Error fatal");
        };

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.ToString(), "Error al abrir la app");
            args.Handled = true;
        };

        base.OnStartup(e);

        try
        {
            MainWindow window = new();
            window.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Error iniciando ventana");
            Shutdown();
        }
    }
}