namespace Reverie.Core.Models;

/// <summary>逻辑声道 → (设备, 设备物理声道) 的一路输出。</summary>
public sealed class ChannelRoute
{
    public ChannelId Channel { get; set; }

    /// <summary>目标设备 Id；空表示未分配。</summary>
    public string DeviceId { get; set; } = string.Empty;

    public string DeviceName { get; set; } = string.Empty;

    /// <summary>设备上的物理声道序号，1 起。</summary>
    public int DeviceChannel { get; set; } = 1;

    /// <summary>增益，单位 dB。</summary>
    public double GainDb { get; set; }

    /// <summary>延时，单位毫秒（用于多设备对齐）。</summary>
    public double DelayMs { get; set; }

    /// <summary>极性；false = 反相。</summary>
    public bool Polarity { get; set; } = true;

    public bool Mute { get; set; }

    public ChannelRoute Clone() => (ChannelRoute)MemberwiseClone();
}
