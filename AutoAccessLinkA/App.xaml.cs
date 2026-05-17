using Microsoft.Extensions.DependencyInjection;

namespace AutoAccessLinkA
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
            {
                Serilog.Log.Fatal(error.ExceptionObject as Exception, "Ứng dụng bị crash (UnhandledException).");
                Serilog.Log.CloseAndFlush();
            };

            TaskScheduler.UnobservedTaskException += (sender, error) =>
            {
                Serilog.Log.Error(error.Exception, "Lỗi Task chạy ngầm (UnobservedTaskException).");
            };
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
            window.Title = "AutoAccessLinkA";

#if WINDOWS
            window.Created += (s, e) =>
            {
                // CreateTrayIcon();
            };
#endif
            return window;
        }

#if WINDOWS
        private H.NotifyIcon.TaskbarIcon trayIcon;

        private void CreateTrayIcon()
        {
            trayIcon = new H.NotifyIcon.TaskbarIcon
            {
                ToolTipText = "AutoAccessLinkA Đang chạy ngầm",
                IconSource = "dotnet_bot.png"
            };

            var menu = new Microsoft.Maui.Controls.MenuFlyout();
            
            var showItem = new Microsoft.Maui.Controls.MenuFlyoutItem { Text = "Mở cửa sổ" };
            showItem.Clicked += ShowWindow_Clicked;
            
            var exitItem = new Microsoft.Maui.Controls.MenuFlyoutItem { Text = "Thoát" };
            exitItem.Clicked += Exit_Clicked;

            menu.Add(showItem);
            menu.Add(exitItem);

            Microsoft.Maui.Controls.FlyoutBase.SetContextFlyout(trayIcon, menu);
            trayIcon.ForceCreate();
        }
#endif

        private void ShowWindow_Clicked(object sender, EventArgs e)
        {
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window != null)
            {
#if WINDOWS
                var nativeWindow = window.Handler.PlatformView as Microsoft.UI.Xaml.Window;
                nativeWindow?.Activate();
#endif
            }
        }

        private void Exit_Clicked(object sender, EventArgs e)
        {
            Application.Current?.Quit();
        }
    }
}