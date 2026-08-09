using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace HearthstoneDeckTracker
{
    public partial class WidgetWindow : Window
    {
        private DispatcherTimer positionTimer;
        private Action onCloseRequested;

        public WidgetWindow(Action onClose)
        {
            InitializeComponent();
            onCloseRequested = onClose;

            positionTimer = new DispatcherTimer();
            positionTimer.Interval = TimeSpan.FromMilliseconds(200);
            positionTimer.Tick += PositionTimer_Tick;
            positionTimer.Start();

            UpdatePosition();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            positionTimer.Stop();
            onCloseRequested?.Invoke();
            this.Close();
        }

        private void PositionTimer_Tick(object sender, EventArgs e)
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            try
            {
                var processes = Process.GetProcessesByName("Hearthstone");
                if (processes.Length > 0)
                {
                    var hsProcess = processes[0];
                    IntPtr handle = hsProcess.MainWindowHandle;

                    if (handle != IntPtr.Zero && Win32.GetWindowRect(handle, out Win32.RECT rect))
                    {
                        IntPtr activeHandle = Win32.GetForegroundWindow();
                        bool isHearthstoneActive = (activeHandle == handle);

                        if (isHearthstoneActive)
                        {
                            double scaleX = 1.0;
                            double scaleY = 1.0;
                            var source = PresentationSource.FromVisual(this);
                            if (source != null && source.CompositionTarget != null)
                            {
                                scaleX = source.CompositionTarget.TransformToDevice.M11;
                                scaleY = source.CompositionTarget.TransformToDevice.M22;
                            }

                            double hsLeft = rect.Left / scaleX;
                            double hsTop = rect.Top / scaleY;

                            // Position widget in top-left corner
                            this.Left = hsLeft + 15;
                            this.Top = hsTop + 60;

                            if (this.Visibility != Visibility.Visible)
                            {
                                this.Visibility = Visibility.Visible;
                            }
                        }
                        else
                        {
                            if (this.Visibility == Visibility.Visible)
                            {
                                this.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                    else
                    {
                        if (this.Visibility == Visibility.Visible)
                        {
                            this.Visibility = Visibility.Collapsed;
                        }
                    }
                }
                else
                {
                    if (this.Visibility == Visibility.Visible)
                    {
                        this.Visibility = Visibility.Collapsed;
                    }
                }
            }
            catch
            {
                // Prevent crash during process lookup
            }
        }
    }
}
