using Reverie.Core.Models;

namespace Reverie.Gui.ViewModels;

public sealed class DeviceViewModel : ViewModelBase
{
    private bool _isSelected;

    public DeviceViewModel(AudioDeviceInfo info)
    {
        Info = info;
    }

    public AudioDeviceInfo Info { get; }
    public string Id => Info.Id;
    public string Name => Info.Name;
    public int ChannelCount => Info.ChannelCount;
    public string DisplayLine => Info.DisplayLine;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
