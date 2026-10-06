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
    }
}
