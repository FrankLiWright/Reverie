using System.Text.Json.Serialization;

namespace Reverie.Core.Models;

/// <summary>布局图中一只音箱的归一化坐标（0..1，原点左上）。</summary>
public sealed class SpeakerPosition
{
    public ChannelId Id { get; set; }

    /// <summary>水平位置，0 = 左墙，1 = 右墙。</summary>
    public double X { get; set; }

    /// <summary>垂直位置，0 = 前墙，1 = 后墙。</summary>
    public double Y { get; set; }

    /// <summary>高度层级：0 水平，1 顶置。</summary>
    public int HeightTier { get; set; }

    [JsonIgnore]
    public string Label => Id.ToDisplay();
}
