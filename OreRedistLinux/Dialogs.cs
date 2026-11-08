using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// The original WPF code base used blocking dialogs (MessageBox.Show, ShowDialog, OpenFileDialog...)
// everywhere, including from background threads. Avalonia dialogs are async only.
// This file provides blocking wrappers with the same shape as the WPF API so the
// view model logic can stay as it was.

namespace SpaceEngineersOreRedistribution
{
    public enum MessageBoxButton { OK, OKCancel, YesNo }
    public enum MessageBoxImage { None, Information, Warning, Error }
    public enum MessageBoxResult { None, OK, Cancel, Yes, No }

    internal static class UiBlocking
    {
        public static Window MainWindow =>
            (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

        /// <summary>
        /// Runs async UI work and blocks the calling thread until it is done.
        /// Works from the UI thread (nested message loop, like WPF modal dialogs)
        /// and from background threads (marshals to the UI thread).
        /// </summary>
        public static T Run<T>(Func<Task<T>> work)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                var task = work();
                if (!task.IsCompleted)
                {
                    using var cts = new CancellationTokenSource();
                    task.ContinueWith(_ => cts.Cancel(), TaskScheduler.Default);
                    Dispatcher.UIThread.MainLoop(cts.Token);
                }
                return task.GetAwaiter().GetResult();
            }
            return Dispatcher.UIThread.InvokeAsync(work).GetAwaiter().GetResult();
        }

        public static void Run(Func<Task> work) =>
            Run(async () => { await work(); return 0; });
    }

    public static class MessageBox
    {
        public static MessageBoxResult Show(string text) =>
            Show(text, "", MessageBoxButton.OK, MessageBoxImage.None);

        public static MessageBoxResult Show(string text, string caption) =>
            Show(text, caption, MessageBoxButton.OK, MessageBoxImage.None);

        public static MessageBoxResult Show(string text, string caption, MessageBoxButton buttons) =>
            Show(text, caption, buttons, MessageBoxImage.None);

        public static MessageBoxResult Show(string text, string caption, MessageBoxButton buttons, MessageBoxImage image)
            => UiBlocking.Run(() => ShowAsync(text, caption, buttons, image));

        static async Task<MessageBoxResult> ShowAsync(string text, string caption, MessageBoxButton buttons, MessageBoxImage image)
        {
            var result = MessageBoxResult.None;
            var win = new Window
            {
                Title = string.IsNullOrEmpty(caption) ? "Message" : caption,
                SizeToContent = SizeToContent.WidthAndHeight,
                MaxWidth = 640,
                CanResize = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ShowInTaskbar = false,
            };
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
            };
            void AddButton(string label, MessageBoxResult res, bool isDefault = false)
            {
                var b = new Button { Content = label, MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center, IsDefault = isDefault };
                b.Click += (_, _) => { result = res; win.Close(); };
                buttonPanel.Children.Add(b);
            }
            switch (buttons)
            {
                case MessageBoxButton.OK:
                    AddButton("OK", MessageBoxResult.OK, true);
                    break;
                case MessageBoxButton.OKCancel:
                    AddButton("OK", MessageBoxResult.OK, true);
                    AddButton("Cancel", MessageBoxResult.Cancel);
                    break;
                case MessageBoxButton.YesNo:
                    AddButton("Yes", MessageBoxResult.Yes, true);
                    AddButton("No", MessageBoxResult.No);
                    break;
            }
            string prefix = image switch
            {
                MessageBoxImage.Warning => "⚠  ",
                MessageBoxImage.Error => "✖  ",
                MessageBoxImage.Information => "ℹ  ",
                _ => "",
            };
            win.Content = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = prefix + text, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    buttonPanel,
                }
            };
            var owner = UiBlocking.MainWindow;
            if (owner != null && owner.IsVisible)
                await win.ShowDialog(owner);
            else
            {
                var tcs = new TaskCompletionSource();
                win.Closed += (_, _) => tcs.TrySetResult();
                win.Show();
                await tcs.Task;
            }
            // Closing with the window X: treat like Cancel / No
            if (result == MessageBoxResult.None)
                result = buttons == MessageBoxButton.YesNo ? MessageBoxResult.No :
                         buttons == MessageBoxButton.OKCancel ? MessageBoxResult.Cancel : MessageBoxResult.OK;
            return result;
        }
    }

    public abstract class FileDialogBase
    {
        /// <summary>WPF style filter, e.g. "PNG Files|*.png" or "A|*.a;*.b|B|*.c"</summary>
        public string Filter { get; set; }
        public string FileName { get; set; }
        public string Title { get; set; }

        protected IReadOnlyList<FilePickerFileType> ParseFilter()
        {
            var list = new List<FilePickerFileType>();
            if (string.IsNullOrEmpty(Filter)) return list;
            var parts = Filter.Split('|');
            for (int i = 0; i + 1 < parts.Length; i += 2)
            {
                var patterns = parts[i + 1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                // Linux file systems are case sensitive. Offer upper case variants as well.
                var all = patterns.Concat(patterns.Select(p => p.ToUpperInvariant())).Distinct().ToList();
                list.Add(new FilePickerFileType(parts[i]) { Patterns = all });
            }
            return list;
        }

        protected static string LocalPath(IStorageItem item) => item?.TryGetLocalPath();
    }

    public class OpenFileDialog : FileDialogBase
    {
        public bool Multiselect { get; set; }
        public bool? ShowDialog() => UiBlocking.Run(ShowAsync);

        async Task<bool?> ShowAsync()
        {
            var owner = UiBlocking.MainWindow;
            if (owner == null) return false;
            var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = Title ?? "Open",
                AllowMultiple = false,
                FileTypeFilter = ParseFilter(),
                SuggestedFileName = FileName,
            });
            var path = files.Count > 0 ? LocalPath(files[0]) : null;
            if (path == null) return false;
            FileName = path;
            return true;
        }
    }

    public class SaveFileDialog : FileDialogBase
    {
        public bool? ShowDialog() => UiBlocking.Run(ShowAsync);

        async Task<bool?> ShowAsync()
        {
            var owner = UiBlocking.MainWindow;
            if (owner == null) return false;
            var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = Title ?? "Save",
                FileTypeChoices = ParseFilter(),
                SuggestedFileName = FileName,
                ShowOverwritePrompt = false, // the callers ask on their own
            });
            var path = LocalPath(file);
            if (path == null) return false;
            FileName = path;
            return true;
        }
    }

    public class FolderDialog
    {
        public string Title { get; set; }
        public string SelectedPath { get; set; }

        /// <summary>For automated UI tests only: skips the native picker.</summary>
        internal static Func<string> TestPathOverride;

        public bool? ShowDialog() => UiBlocking.Run(ShowAsync);

        async Task<bool?> ShowAsync()
        {
            if (TestPathOverride != null)
            {
                SelectedPath = TestPathOverride();
                return SelectedPath != null;
            }
            var owner = UiBlocking.MainWindow;
            if (owner == null) return false;
            var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = Title ?? "Select folder",
                AllowMultiple = false,
            });
            var path = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
            if (path == null) return false;
            SelectedPath = path;
            return true;
        }
    }

    /// <summary>Window that can be shown modal with a blocking call, like WPF ShowDialog().</summary>
    public class DialogWindow : Window
    {
        public bool? DialogResult { get; private set; }

        public DialogWindow()
        {
            // Commands re-evaluate CanExecute after user input (WPF did this via CommandManager)
            AddHandler(PointerReleasedEvent, (_, _) => RelayCommand.RequestRequery(), Avalonia.Interactivity.RoutingStrategies.Tunnel, true);
            AddHandler(KeyUpEvent, (_, _) => RelayCommand.RequestRequery(), Avalonia.Interactivity.RoutingStrategies.Tunnel, true);
        }

        public void CloseWithResult(bool result)
        {
            DialogResult = result;
            Close(result);
        }

        public bool? ShowDialogBlocking(Window owner = null)
        {
            owner ??= UiBlocking.MainWindow;
            var res = UiBlocking.Run(async () =>
            {
                if (owner != null && owner.IsVisible)
                    return await ShowDialog<bool?>(owner);
                var tcs = new TaskCompletionSource<bool?>();
                Closed += (_, _) => tcs.TrySetResult(DialogResult);
                Show();
                return await tcs.Task;
            });
            return res ?? DialogResult;
        }
    }
}
