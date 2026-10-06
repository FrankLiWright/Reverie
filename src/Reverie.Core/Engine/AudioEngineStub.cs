using Reverie.Core.Models;

namespace Reverie.Core.Engine;

/// <summary>配置容器 + 路由校验骨架（不真正播放）。</summary>
public sealed class AudioEngineStub
{
    private readonly List<ChannelRoute> _routes = new();
    private readonly List<SpeakerPosition> _speakers = new();

    public string Status { get; private set; } = "引擎骨架已就绪（未启动播放）";

    public IReadOnlyList<ChannelRoute> Routes => _routes;
    public IReadOnlyList<SpeakerPosition> Speakers => _speakers;

    public void LoadPreset(LayoutPreset preset)
    {
        _routes.Clear();
        _routes.AddRange(preset.Routes.Select(r => r.Clone()));
        _speakers.Clear();
        _speakers.AddRange(preset.Speakers.Select(s => new SpeakerPosition
        {
            Id = s.Id,
            X = s.X,
            Y = s.Y,
            HeightTier = s.HeightTier,
        }));
        Status = $"已加载预设「{preset.Name}」 · {_routes.Count} 路路由 · 未启动播放";
    }

    public void UpsertRoute(ChannelRoute route)
    {
        var existing = _routes.FirstOrDefault(r =>
            r.Channel == route.Channel &&
            r.DeviceId == route.DeviceId &&
            r.DeviceChannel == route.DeviceChannel);
        if (existing is null)
            _routes.Add(route.Clone());
        else
        {
            existing.GainDb = route.GainDb;
            existing.DelayMs = route.DelayMs;
            existing.Polarity = route.Polarity;
            existing.Mute = route.Mute;
            existing.DeviceName = route.DeviceName;
        }
        Status = $"路由已更新 · 共 {_routes.Count} 路";
    }

    public void RemoveRoute(ChannelId channel, string deviceId, int deviceChannel)
    {
        _routes.RemoveAll(r =>
            r.Channel == channel &&
            r.DeviceId == deviceId &&
            r.DeviceChannel == deviceChannel);
        Status = $"已删除路由 · 剩余 {_routes.Count} 路";
    }

    /// <summary>校验：未分配设备、声道越界、延时差过大。</summary>
    public IReadOnlyList<string> Validate(IReadOnlyList<AudioDeviceInfo> devices)
    {
        var warnings = new List<string>();
        var byId = devices.ToDictionary(d => d.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var route in _routes)
        {
            if (string.IsNullOrEmpty(route.DeviceId))
            {
                warnings.Add($"{route.Channel}：未分配设备");
                continue;
            }

            if (!byId.TryGetValue(route.DeviceId, out var dev))
            {
                warnings.Add($"{route.Channel}：设备「{route.DeviceName}」已不存在");
                continue;
            }

            if (route.DeviceChannel < 1 || route.DeviceChannel > dev.ChannelCount)
            {
                warnings.Add($"{route.Channel}：目标声道 {route.DeviceChannel} 超出设备「{dev.Name}」的 {dev.ChannelCount}ch");
            }
        }

        if (_routes.Count > 0)
        {
            var maxDelay = _routes.Max(r => r.DelayMs);
            var minDelay = _routes.Min(r => r.DelayMs);
            if (maxDelay - minDelay > 80)
            {
                warnings.Add($"路由延时差 {maxDelay - minDelay:F0}ms 偏大，多设备可能出现可感知不同步");
            }
        }

        return warnings;
    }
}
