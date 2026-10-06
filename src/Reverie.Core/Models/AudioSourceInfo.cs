namespace Reverie.Core.Models;

/// <summary>音频源：捕获设备或某渲染设备的系统回环。</summary>
public sealed record AudioSourceInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required bool IsLoopback { get; init; }
    public int ChannelCount { get; init; } = 2;
    public int SampleRate { get; init; } = 48000;
    public string Kind { get; init; } = "Capture";

    public string DisplayLine =>
        $"{Name}  ·  {(IsLoopback ? "回环" : "捕获")}  ·  {ChannelCount}ch";
}
