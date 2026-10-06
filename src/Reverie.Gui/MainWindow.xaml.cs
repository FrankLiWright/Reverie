using System.Windows;
using Reverie.Core.Devices;
using Reverie.Gui.ViewModels;

namespace Reverie.Gui;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(new WasapiDeviceEnumerator());
        Closing += OnClosing;
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // 记住窗口尺寸（布局/开关已由 VM 持久化）
        if (DataContext is MainViewModel vm)
        {
            // 通过 UserSettings 静态路径写入
            var s = Core.IO.UserSettings.Load();
            if (Width > 200 && Height > 200)
            {
                s.WindowWidth = Width;
                s.WindowHeight = Height;
            }
            s.Save();
        }
    }
}
