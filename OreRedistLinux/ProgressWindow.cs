using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using System;

namespace SpaceEngineersOreRedistribution
{
    /// <summary>
    /// Modal progress window of a redistribution run: a bar, what is going on, and Cancel.
    /// Closing the window with the X is the same as Cancel while cancelling is still possible.
    /// </summary>
    public class ProgressWindow : Window
    {
        readonly ProgressBar _bar;
        readonly TextBlock _status;
        readonly TextBlock _percent;
        readonly Button _cancel;
        bool _finished;

        /// <summary>Raised when the user asks to cancel.</summary>
        public event Action CancelRequested;

        public ProgressWindow()
        {
            Title = "Redistributing ore";
            Width = 480;
            SizeToContent = SizeToContent.Height;
            CanResize = false;
            ShowInTaskbar = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            _bar = new ProgressBar { Minimum = 0, Maximum = 100, Height = 18, ShowProgressText = false };
            _status = new TextBlock { Text = "Starting...", TextWrapping = Avalonia.Media.TextWrapping.Wrap };
            _percent = new TextBlock { Text = "0%", HorizontalAlignment = HorizontalAlignment.Right };
            _cancel = new Button { Content = "Cancel", Width = 100, HorizontalAlignment = HorizontalAlignment.Right, HorizontalContentAlignment = HorizontalAlignment.Center };
            _cancel.Click += (_, _) => RequestCancel();
            Content = new StackPanel
            {
                Margin = new Thickness(16),
                Spacing = 10,
                Children =
                {
                    _status,
                    _bar,
                    _percent,
                    _cancel,
                }
            };
            Closing += (_, e) =>
            {
                if (_finished) return;
                e.Cancel = true; // only the run itself closes this window
                RequestCancel();
            };
        }

        void RequestCancel()
        {
            if (!_cancel.IsEnabled) return;
            _cancel.IsEnabled = false;
            _cancel.Content = "Cancelling...";
            CancelRequested?.Invoke();
        }

        /// <summary>fraction 0..1. Call on the UI thread.</summary>
        public void Report(double fraction, string status)
        {
            fraction = Math.Clamp(fraction, 0, 1);
            _bar.Value = fraction * 100;
            _percent.Text = $"{fraction * 100:0}%";
            if (!string.IsNullOrEmpty(status)) _status.Text = status;
        }

        /// <summary>Cancel is not possible any more (the files are being written).</summary>
        public void DisableCancel(string status)
        {
            _cancel.IsEnabled = false;
            _cancel.Content = "Cancel";
            if (!string.IsNullOrEmpty(status)) _status.Text = status;
        }

        public void Finish()
        {
            _finished = true;
            Close();
        }
    }
}
