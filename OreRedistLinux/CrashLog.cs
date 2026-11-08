using System;
using System.IO;
using System.Threading.Tasks;

namespace SpaceEngineersOreRedistribution
{
    /// <summary>
    /// Unhandled errors end up in ~/.config/OreRedistLinux/crash.log and a dialog,
    /// instead of silently closing the program.
    /// </summary>
    public static class CrashLog
    {
        public static string FilePath => Path.Combine(PathHelper.UserConfigDirectory, "crash.log");

        public static void Install()
        {
            Avalonia.Threading.Dispatcher.UIThread.UnhandledException += (s, e) =>
            {
                e.Handled = true; // keep the program (and unsaved setup) alive
                Report(e.Exception, "UI thread");
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Write(e.ExceptionObject as Exception, "AppDomain");
            TaskScheduler.UnobservedTaskException += (s, e) => { Write(e.Exception, "Task"); e.SetObserved(); };
        }

        public static void Write(Exception ex, string source)
        {
            try
            {
                File.AppendAllText(FilePath, $"==== {DateTime.Now:yyyy-MM-dd HH:mm:ss} ({source}) ====\n{ex}\n\n");
            }
            catch { }
        }

        static void Report(Exception ex, string source)
        {
            Write(ex, source);
            try
            {
                MessageBox.Show("Something went wrong:\n\n" + ex.GetType().Name + ": " + ex.Message +
                                "\n\nDetails were saved to\n" + FilePath, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch { }
        }
    }
}
