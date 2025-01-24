using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.IO.Pipes;
using DAIDep.View;

namespace DAIDep
{
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            base.OnStartup(e);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogException(e.ExceptionObject as Exception);
            Environment.Exit(1);
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception);
            e.Handled = true;
            Environment.Exit(1);
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException(e.Exception);
            e.SetObserved();
        }

        private void LogException(Exception ex)
        {
            File.AppendAllText("C:\\SZTSProgramInstaller\\SZTSConfig\\sztsfs_gui_errlog.txt", $"{DateTime.Now}: {ex}\n");
        }
    }
}
