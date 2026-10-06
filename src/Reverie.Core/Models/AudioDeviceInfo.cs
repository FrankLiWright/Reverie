namespace Reverie.Core.Models;

/// <summary>渲染（播放）设备快照，来自 WASAPI。</summary>
public sealed record AudioDeviceInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required int ChannelCount { get; init; }
    public int SampleRate { get; init; } = 48000;
    public int BitDepth { get; init; } = 32;
    public bool IsDefault { get; init; }
    public string DeviceType { get; init; } = "Unknown";

    public string DisplayLine =>
        $"{Name}  ·  {ChannelCount}ch  ·  {(IsDefault ? "默认" : DeviceType)}";
}
