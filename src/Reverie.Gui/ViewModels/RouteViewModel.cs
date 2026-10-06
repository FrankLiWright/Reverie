using Reverie.Core.Models;

namespace Reverie.Gui.ViewModels;

public sealed class RouteViewModel : ViewModelBase
{
    private string _deviceId = string.Empty;
    private string _deviceName = "未分配";
    private int _deviceChannel = 1;
    private double _gainDb;
    private double _delayMs;
    private bool _polarity = true;
    private bool _mute;

    public ChannelId Channel { get; set; }
    public string ChannelLabel => Channel.ToDisplay();

    public string DeviceId
    {
        get => _deviceId;
        set => SetProperty(ref _deviceId, value);
    }

    public string DeviceName
    {
        get => _deviceName;
        set => SetProperty(ref _deviceName, value);
    }

    public int DeviceChannel
    {
        get => _deviceChannel;
        set
        {
            if (SetProperty(ref _deviceChannel, value))
                Raise(nameof(DeviceChannelIndex));
        }
    }

    /// <summary>ComboBox 用的 0 基索引。</summary>
    public int DeviceChannelIndex
    {
        get => Math.Max(0, _deviceChannel - 1);
        set
        {
            if (value >= 0)
                DeviceChannel = value + 1;
        }
    }

    public double GainDb
    {
        get => _gainDb;
        set
        {
            if (SetProperty(ref _gainDb, value))
                Raise(nameof(GainText));
        }
    }

    public double DelayMs
    {
        get => _delayMs;
        set
        {
            if (SetProperty(ref _delayMs, value))
                Raise(nameof(DelayText));
        }
    }

    public bool Polarity
    {
        get => _polarity;
        set
        {
            if (SetProperty(ref _polarity, value))
            {
                Raise(nameof(PolarityText));
                Raise(nameof(IsInverted));
            }
        }
    }

    /// <summary>UI 用：true = 反相。</summary>
    public bool IsInverted
    {
        get => !_polarity;
        set => Polarity = !value;
    }

    public bool Mute
    {
        get => _mute;
        set => SetProperty(ref _mute, value);
    }

    public string GainText => $"{GainDb:+0.0;-0.0;0.0} dB";
    public string DelayText => $"{DelayMs:F0} ms";
    public string PolarityText => Polarity ? "正相" : "反相";
    public string RouteSummary => $"{Channel} → {DeviceName} ch{DeviceChannel}";

    public ChannelRoute ToModel() => new()
    {
        Channel = Channel,
        DeviceId = DeviceId,
        DeviceName = DeviceName,
        DeviceChannel = DeviceChannel,
        GainDb = GainDb,
        DelayMs = DelayMs,
        Polarity = Polarity,
        Mute = Mute,
    };

    public static RouteViewModel FromModel(ChannelRoute route) => new()
    {
        Channel = route.Channel,
        DeviceId = route.DeviceId,
        DeviceName = route.DeviceName,
        DeviceChannel = route.DeviceChannel,
        GainDb = route.GainDb,
        DelayMs = route.DelayMs,
        Polarity = route.Polarity,
        Mute = route.Mute,
    };
}
