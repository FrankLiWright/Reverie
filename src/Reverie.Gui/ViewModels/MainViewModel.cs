using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;
using Reverie.Core.Devices;
using Reverie.Core.Dsp;
using Reverie.Core.Engine;
using Reverie.Core.IO;
using Reverie.Core.Layouts;
using Reverie.Core.Models;

namespace Reverie.Gui.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private readonly IAudioDeviceEnumerator _enumerator;
    private readonly AudioEngineStub _stub = new();
    private readonly WasapiRoutingEngine _routing = new();
    private readonly Dispatcher _dispatcher;

    private SpeakerViewModel? _selectedSpeaker;
    private DeviceViewModel? _frontDevice;
    private DeviceViewModel? _surroundDevice;
    private DeviceViewModel? _subDevice;
    private DeviceViewModel? _heightDevice;
    private AudioSourceInfo? _selectedSource;
    private LayoutLibrary.LayoutInfo _layout;
    private string _statusText = "就绪";
    private string _presetName;
    private bool _isRunning;
    private string _engineStatus = "未运行";
    private double _levelL;
    private double _levelR;
    private bool _swapFront;
    private bool _swapSurround;
    private bool _swapHeight;
    private bool _showAdvanced;
    private bool _freeMove;
    private bool _enableUpmix = true;
    private readonly UserSettings _settings;

    public MainViewModel(IAudioDeviceEnumerator enumerator)
    {
        _enumerator = enumerator;
        _dispatcher = Dispatcher.CurrentDispatcher;
        _settings = UserSettings.Load();
        _layout = LayoutLibrary.Find(_settings.LayoutId) ?? LayoutLibrary.All[0];
        _presetName = _layout.Name;
        _freeMove = _settings.FreeMove;
        _enableUpmix = _settings.EnableUpmix;
        _showAdvanced = _settings.ShowAdvanced;
        _routing.EnableUpmix = _settings.EnableUpmix;

        ToggleEngineCommand = new RelayCommand(_ => ToggleEngine());
        RefreshDevicesCommand = new RelayCommand(_ => RefreshDevices());
        SwapRearCommand = new RelayCommand(_ =>
        {
            SwapSurround = !SwapSurround;
            ApplyRoleMapping();
        });
        ToggleSwapFrontCommand = new RelayCommand(_ =>
        {
            SwapFront = !SwapFront;
            ApplyRoleMapping();
            StatusText = SwapFront ? "前置左右已对调" : "前置正常相位";
        });
        ToggleSwapSurroundCommand = new RelayCommand(_ =>
        {
            SwapSurround = !SwapSurround;
            ApplyRoleMapping();
            StatusText = SwapSurround ? "环绕左右已对调" : "环绕正常相位";
        });
        ToggleSwapHeightCommand = new RelayCommand(_ =>
        {
            SwapHeight = !SwapHeight;
            ApplyRoleMapping();
            StatusText = SwapHeight ? "高度左右已对调" : "高度正常相位";
        });
        ResetLayoutCommand = new RelayCommand(_ => ResetLayout());
        SpeakerMovedCommand = new RelayCommand(_ => NotifySpeakerMoved());
        ResetUpmixCommand = new RelayCommand(_ => ResetUpmix());
        SavePresetCommand = new RelayCommand(_ => SavePreset());
        LoadPresetCommand = new RelayCommand(_ => LoadPreset());

        foreach (var info in LayoutLibrary.All)
            Layouts.Add(info);

        ApplyLayout(_layout);
        Raise(nameof(SelectedLayout));

        _routing.StatusChanged += text => _dispatcher.BeginInvoke(() =>
        {
            EngineStatus = text;
            StatusText = text;
        });
        _routing.LevelsChanged += levels => _dispatcher.BeginInvoke(() =>
        {
            if (levels.Length >= 2)
            {
                LevelL = Math.Min(1.0, levels[0]);
                LevelR = Math.Min(1.0, levels[1]);
            }
        });

        RefreshDevices();
    }

    public ObservableCollection<DeviceViewModel> Devices { get; } = new();
    public ObservableCollection<AudioSourceInfo> Sources { get; } = new();
    public ObservableCollection<SpeakerViewModel> Speakers { get; } = new();
    public ObservableCollection<RouteViewModel> Routes { get; } = new();
    public ObservableCollection<UpmixGainViewModel> UpmixGains { get; } = new();
    public ObservableCollection<LayoutLibrary.LayoutInfo> Layouts { get; } = new();

    public RelayCommand ToggleEngineCommand { get; }
    public RelayCommand RefreshDevicesCommand { get; }
    public RelayCommand SwapRearCommand { get; }
    public RelayCommand ToggleSwapFrontCommand { get; }
    public RelayCommand ToggleSwapSurroundCommand { get; }
    public RelayCommand ToggleSwapHeightCommand { get; }
    public RelayCommand ResetLayoutCommand { get; }
    public RelayCommand SpeakerMovedCommand { get; }
    public RelayCommand ResetUpmixCommand { get; }
    public RelayCommand SavePresetCommand { get; }
    public RelayCommand LoadPresetCommand { get; }

    public string PresetName
    {
        get => _presetName;
        set => SetProperty(ref _presetName, value);
    }

    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string EngineStatus
    {
        get => _engineStatus;
        set => SetProperty(ref _engineStatus, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (SetProperty(ref _isRunning, value))
            {
                Raise(nameof(IsStopped));
                Raise(nameof(EngineToggleText));
            }
        }
    }

    public bool IsStopped => !IsRunning;
    public string EngineToggleText => IsRunning ? "停止路由" : "开始路由";

    public double LevelL
    {
        get => _levelL;
        set => SetProperty(ref _levelL, value);
    }

    public double LevelR
    {
        get => _levelR;
        set => SetProperty(ref _levelR, value);
    }

    public bool SwapFront
    {
        get => _swapFront;
        set
        {
            if (SetProperty(ref _swapFront, value))
                Raise(nameof(SwapFrontText));
        }
    }

    public bool SwapSurround
    {
        get => _swapSurround;
        set
        {
            if (SetProperty(ref _swapSurround, value))
                Raise(nameof(SwapSurroundText));
        }
    }

    public bool SwapHeight
    {
        get => _swapHeight;
        set
        {
            if (SetProperty(ref _swapHeight, value))
                Raise(nameof(SwapHeightText));
        }
    }

    public string SwapFrontText => SwapFront ? "左右已对调" : "正常相位";
    public string SwapSurroundText => SwapSurround ? "左右已对调" : "正常相位";
    public string SwapHeightText => SwapHeight ? "左右已对调" : "正常相位";

    public bool FreeMove
    {
        get => _freeMove;
        set
        {
            if (SetProperty(ref _freeMove, value))
            {
                _settings.FreeMove = value;
                _settings.Save();
                StatusText = value ? "自由移动已开启" : "自由移动已锁定，仅可点击选中";
            }
        }
    }

    /// <summary>立体声上混（仅对立体声源生效，多声道始终直通）。</summary>
    public bool EnableUpmix
    {
        get => _enableUpmix;
        set
        {
            if (SetProperty(ref _enableUpmix, value))
            {
                _routing.EnableUpmix = value;
                _settings.EnableUpmix = value;
                _settings.Save();
                StatusText = value ? "立体声上混：开（多声道源仍直通）" : "立体声上混：关";
            }
        }
    }

    public bool ShowAdvanced
    {
        get => _showAdvanced;
        set
        {
            if (SetProperty(ref _showAdvanced, value))
            {
                _settings.ShowAdvanced = value;
                _settings.Save();
            }
        }
    }

    public LayoutLibrary.LayoutInfo SelectedLayout
    {
        get => _layout;
        set
        {
            if (SetProperty(ref _layout, value) && value is not null)
            {
                PresetName = value.Name;
                _settings.LayoutId = value.Id;
                _settings.Save();
                ApplyLayout(value);
            }
        }
    }

    public bool HasSurround => _layout.Speakers.Any(s => s.Id is ChannelId.SL or ChannelId.SR or ChannelId.RL or ChannelId.RR);
    public bool HasSub => _layout.Speakers.Any(s => s.Id is ChannelId.LFE or ChannelId.LFE2);
    public bool HasHeight => _layout.Speakers.Any(s => s.HeightTier > 0);

    public AudioSourceInfo? SelectedSource
    {
        get => _selectedSource;
        set
        {
            if (SetProperty(ref _selectedSource, value) && IsRunning)
                StatusText = "源已更改，请重新开始路由";
        }
    }

    public DeviceViewModel? FrontDevice
    {
        get => _frontDevice;
        set
        {
            if (SetProperty(ref _frontDevice, value))
            {
                ApplyRoleMapping();
                StatusText = value is null ? "前置未分配" : $"前置 → {value.Name}";
            }
        }
    }

    public DeviceViewModel? SurroundDevice
    {
        get => _surroundDevice;
        set
        {
            if (SetProperty(ref _surroundDevice, value))
            {
                ApplyRoleMapping();
                StatusText = value is null ? "环绕未分配" : $"环绕 → {value.Name}";
            }
        }
    }

    public DeviceViewModel? SubDevice
    {
        get => _subDevice;
        set
        {
            if (SetProperty(ref _subDevice, value))
            {
                ApplyRoleMapping();
                StatusText = value is null ? "低音未分配" : $"低音 → {value.Name}";
            }
        }
    }

    public DeviceViewModel? HeightDevice
    {
        get => _heightDevice;
        set
        {
            if (SetProperty(ref _heightDevice, value))
            {
                ApplyRoleMapping();
                StatusText = value is null ? "高度未分配" : $"高度 → {value.Name}";
            }
        }
    }

    public SpeakerViewModel? SelectedSpeaker
    {
        get => _selectedSpeaker;
        set
        {
            if (SetProperty(ref _selectedSpeaker, value))
            {
                foreach (var s in Speakers)
                    s.IsSelected = ReferenceEquals(s, value);
                Raise(nameof(HasSelectedSpeaker));
                Raise(nameof(SelectedRoute));
            }
        }
    }

    public bool HasSelectedSpeaker => SelectedSpeaker is not null;

    public RouteViewModel? SelectedRoute
    {
        get
        {
            if (SelectedSpeaker is null)
                return null;
            return Routes.FirstOrDefault(r => r.Channel == SelectedSpeaker.Id);
        }
    }

    public void NotifySpeakerMoved()
    {
        StatusText = SelectedSpeaker is null
            ? "布局已更新"
            : $"已移动 {SelectedSpeaker.ShortLabel} → {SelectedSpeaker.PixelHint}";
    }

    public void RefreshDevices()
    {
        try
        {
            var devices = _enumerator.GetRenderDevices();
            Devices.Clear();
            foreach (var d in devices)
                Devices.Add(new DeviceViewModel(d));

            var sources = _enumerator.GetAudioSources();
            Sources.Clear();
            foreach (var s in sources)
                Sources.Add(s);

            if (SelectedSource is null && Sources.Count > 0)
            {
                SelectedSource = Sources.FirstOrDefault(s =>
                        !s.IsLoopback &&
                        (s.Name.Contains("CABLE", StringComparison.OrdinalIgnoreCase) ||
                         s.Name.Contains("VB-Audio", StringComparison.OrdinalIgnoreCase)))
                    ?? Sources[0];
            }

            if (FrontDevice is null)
                FrontDevice = Devices.FirstOrDefault(d => d.Info.IsDefault) ?? Devices.FirstOrDefault();
            if (SurroundDevice is null)
                SurroundDevice = Devices.FirstOrDefault(d => !ReferenceEquals(d, FrontDevice)) ?? FrontDevice;
            if (SubDevice is null)
                SubDevice = FrontDevice;
            if (HeightDevice is null)
                HeightDevice = SurroundDevice;

            ApplyRoleMapping();
            StatusText = $"设备 {devices.Count} · 源 {sources.Count}";
        }
        catch (Exception ex)
        {
            StatusText = $"设备枚举失败：{ex.Message}";
        }
    }

    private void ApplyLayout(LayoutLibrary.LayoutInfo info)
    {
        Speakers.Clear();
        foreach (var s in info.Speakers)
            Speakers.Add(new SpeakerViewModel(s));

        SelectedSpeaker = Speakers.FirstOrDefault();
        Raise(nameof(HasSurround));
        Raise(nameof(HasSub));
        Raise(nameof(HasHeight));

        RebuildUpmixGains();
        ApplyRoleMapping();
        StatusText = $"已切换布局 {info.Name}";
    }

    private void RebuildUpmixGains()
    {
        if (_layout is null)
            return;

        var outputs = _layout.Speakers.Select(s => s.Id).ToList();
        _routing.ConfigureUpmix(outputs);

        UpmixGains.Clear();
        for (int i = 0; i < outputs.Count; i++)
            UpmixGains.Add(new UpmixGainViewModel(_routing.Upmix, outputs[i], i, _dispatcher));
    }

    public void ApplyRoleMapping()
    {
        if (_layout is null)
            return;

        foreach (var r in Routes)
            r.PropertyChanged -= OnRoutePropertyChanged;
        Routes.Clear();

        var planByCh = _layout.Plan.ToDictionary(p => p.Ch, p => p);

        foreach (var (ch, role, devCh, gain) in _layout.Plan)
        {
            var device = role switch
            {
                DeviceRole.Front => FrontDevice,
                DeviceRole.Surround => SurroundDevice,
                DeviceRole.Sub => SubDevice,
                DeviceRole.Height => HeightDevice,
                _ => null,
            };
            if (device is null)
                continue;

            int n = device.ChannelCount;
            int targetCh = devCh;

            bool swap = role switch
            {
                DeviceRole.Front => SwapFront,
                DeviceRole.Surround => SwapSurround,
                DeviceRole.Height => SwapHeight,
                _ => false,
            };
            if (swap)
            {
                var partner = LrPartner(ch);
                if (partner is not null && planByCh.TryGetValue(partner.Value, out var p2))
                    targetCh = p2.DeviceChannel;
            }

            if (role == DeviceRole.Front && n == 2 && ch is ChannelId.FC or ChannelId.LFE)
            {
                double g = ch == ChannelId.FC ? -3 : -6;
                AddRoute(ch, device, 1, g);
                AddRoute(ch, device, 2, g);
                continue;
            }

            int target = Math.Clamp(targetCh, 1, Math.Max(1, n));
            AddRoute(ch, device, target, gain);
        }

        Raise(nameof(SelectedRoute));
        PushRoutesToEngine();
    }

    private static ChannelId? LrPartner(ChannelId ch) => ch switch
    {
        ChannelId.FL => ChannelId.FR,
        ChannelId.FR => ChannelId.FL,
        ChannelId.SL => ChannelId.SR,
        ChannelId.SR => ChannelId.SL,
        ChannelId.RL => ChannelId.RR,
        ChannelId.RR => ChannelId.RL,
        ChannelId.TFL => ChannelId.TFR,
        ChannelId.TFR => ChannelId.TFL,
        ChannelId.TRL => ChannelId.TRR,
        ChannelId.TRR => ChannelId.TRL,
        _ => null,
    };

    private void AddRoute(ChannelId channel, DeviceViewModel device, int deviceChannel, double gainDb = 0)
    {
        var route = new RouteViewModel
        {
            Channel = channel,
            DeviceId = device.Id,
            DeviceName = device.Name,
            DeviceChannel = deviceChannel,
            GainDb = gainDb,
        };
        route.PropertyChanged += OnRoutePropertyChanged;
        Routes.Add(route);
    }

    private void ToggleEngine()
    {
        if (IsRunning)
        {
            try
            {
                _routing.Stop();
            }
            finally
            {
                IsRunning = false;
                StatusText = "已停止";
            }
            return;
        }

        if (SelectedSource is null)
        {
            StatusText = "请先选择音频源";
            return;
        }

        try
        {
            ApplyRoleMapping();
            _routing.EnableUpmix = EnableUpmix;
            _routing.ConfigureUpmix(_layout.Speakers.Select(s => s.Id).ToList());
            var deviceInfos = Devices.Select(d => d.Info).ToList();
            var routeModels = Routes.Select(r => r.ToModel()).ToList();
            _routing.Start(SelectedSource.Id, SelectedSource.IsLoopback, deviceInfos, routeModels);
            IsRunning = true;
            StatusText = _routing.Status;
        }
        catch (Exception ex)
        {
            IsRunning = false;
            StatusText = $"启动失败：{ex.Message}";
        }
    }

    private void ResetUpmix()
    {
        _routing.Upmix.ResetToDefaults();
        foreach (var g in UpmixGains)
            g.Reload();
        StatusText = "上混矩阵已恢复默认";
    }

    private void ResetLayout()
    {
        ApplyLayout(_layout);
        StatusText = "布局已重置";
    }

    private void SavePreset()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "保存预设",
            Filter = "预设 (*.json)|*.json|所有文件 (*.*)|*.*",
            FileName = string.IsNullOrWhiteSpace(PresetName) ? "preset" : PresetName,
        };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var preset = BuildCurrentPreset();
            PresetStore.Save(preset, dialog.FileName);
            StatusText = $"已保存：{System.IO.Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            StatusText = $"保存失败：{ex.Message}";
        }
    }

    private void LoadPreset()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "加载预设",
            Filter = "预设 (*.json)|*.json|所有文件 (*.*)|*.*",
        };
        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var preset = PresetStore.Load(dialog.FileName);
            ApplySavedPreset(preset);
            StatusText = $"已加载：{System.IO.Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            StatusText = $"加载失败：{ex.Message}";
        }
    }

    private void ApplySavedPreset(LayoutPreset preset)
    {
        PresetName = string.IsNullOrWhiteSpace(preset.Name) ? "5.1" : preset.Name;

        var match = LayoutLibrary.All.FirstOrDefault(l =>
            string.Equals(l.Id, preset.LayoutTag, StringComparison.OrdinalIgnoreCase));

        if (match is not null && match.Speakers.Count == preset.Speakers.Count)
        {
            _layout = match;
            Raise(nameof(SelectedLayout));
            Speakers.Clear();
            foreach (var s in preset.Speakers)
                Speakers.Add(new SpeakerViewModel(s));
            Raise(nameof(HasSurround));
            Raise(nameof(HasSub));
            Raise(nameof(HasHeight));
        }
        else
        {
            Speakers.Clear();
            foreach (var s in preset.Speakers)
                Speakers.Add(new SpeakerViewModel(s));
            SelectedSpeaker = Speakers.FirstOrDefault();
        }

        foreach (var r in Routes)
            r.PropertyChanged -= OnRoutePropertyChanged;
        Routes.Clear();
        foreach (var r in preset.Routes)
        {
            var vm = RouteViewModel.FromModel(r);
            vm.PropertyChanged += OnRoutePropertyChanged;
            Routes.Add(vm);
        }
        Raise(nameof(SelectedRoute));
        PushRoutesToEngine();
    }

    private LayoutPreset BuildCurrentPreset() => new()
    {
        Name = string.IsNullOrWhiteSpace(PresetName) ? _layout.Id : PresetName,
        LayoutTag = _layout.Id,
        Description = _layout.Description,
        Speakers = Speakers.Select(s => new SpeakerPosition
        {
            Id = s.Id,
            X = s.X,
            Y = s.Y,
            HeightTier = s.HeightTier,
        }).ToList(),
        Routes = Routes.Select(r => r.ToModel()).ToList(),
    };

    private void PushRoutesToEngine()
    {
        if (IsRunning)
            _routing.UpdateRoutes(Routes.Select(r => r.ToModel()).ToList());
    }

    private void OnRoutePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (IsRunning && e.PropertyName is nameof(RouteViewModel.GainDb)
            or nameof(RouteViewModel.DelayMs)
            or nameof(RouteViewModel.Polarity)
            or nameof(RouteViewModel.Mute)
            or nameof(RouteViewModel.DeviceChannel))
        {
            PushRoutesToEngine();
        }
    }
}

public sealed class UpmixGainViewModel : ViewModelBase
{
    private readonly MatrixUpmixer _upmixer;
    private readonly int _index;
    private readonly Dispatcher _dispatcher;
    private double _value;

    public UpmixGainViewModel(MatrixUpmixer upmixer, ChannelId channel, int index, Dispatcher dispatcher)
    {
        _upmixer = upmixer;
        _index = index;
        _dispatcher = dispatcher;
        Channel = channel;
        var (l, r) = upmixer.GetGains(index);
        _value = Math.Max(l, r);
    }

    public ChannelId Channel { get; }
    public string Label => Channel.ToString();
    public string ValueText => _value.ToString("0.00");

    public double Value
    {
        get => _value;
        set
        {
            if (!SetProperty(ref _value, value))
                return;
            Raise(nameof(ValueText));

            float v = (float)value;
            var (l, r) = Channel switch
            {
                ChannelId.FR or ChannelId.SR or ChannelId.RR or ChannelId.TFR or ChannelId.TRR
                    => (0f, v),
                ChannelId.FL or ChannelId.SL or ChannelId.RL or ChannelId.TFL or ChannelId.TRL
                    => (v, 0f),
                _ => (v, v),
            };
            _upmixer.SetGains(_index, l, r);
        }
    }

    public void Reload()
    {
        _dispatcher.BeginInvoke(() =>
        {
            var (l, r) = _upmixer.GetGains(_index);
            Value = Math.Max(l, r);
        });
    }
}
