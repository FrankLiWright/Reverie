using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reverie.Core.Models;

/// <summary>通用布局预设。</summary>
public sealed class LayoutPreset
{
    public string Name { get; set; } = "5.1";
    public string Description { get; set; } = string.Empty;
    public string LayoutTag { get; set; } = "5.1";
    public List<SpeakerPosition> Speakers { get; set; } = new();
    public List<ChannelRoute> Routes { get; set; } = new();

    public static LayoutPreset CreateDefault51() => new()
    {
        Name = "5.1",
        Description = string.Empty,
        LayoutTag = "5.1",
        Speakers =
        [
            new SpeakerPosition { Id = ChannelId.FL, X = 0.30, Y = 0.18 },
            new SpeakerPosition { Id = ChannelId.FR, X = 0.70, Y = 0.18 },
            new SpeakerPosition { Id = ChannelId.FC, X = 0.50, Y = 0.12 },
            new SpeakerPosition { Id = ChannelId.LFE, X = 0.50, Y = 0.28 },
            new SpeakerPosition { Id = ChannelId.RL, X = 0.28, Y = 0.82 },
            new SpeakerPosition { Id = ChannelId.RR, X = 0.72, Y = 0.82 },
        ],
        Routes =
        [
            new ChannelRoute { Channel = ChannelId.FL, DeviceChannel = 1 },
            new ChannelRoute { Channel = ChannelId.FR, DeviceChannel = 2 },
            new ChannelRoute { Channel = ChannelId.FC, DeviceChannel = 3 },
            new ChannelRoute { Channel = ChannelId.LFE, DeviceChannel = 4 },
            new ChannelRoute { Channel = ChannelId.RL, DeviceChannel = 1 },
            new ChannelRoute { Channel = ChannelId.RR, DeviceChannel = 2 },
        ],
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static LayoutPreset? FromJson(string json) =>
        JsonSerializer.Deserialize<LayoutPreset>(json, JsonOptions);

    public LayoutPreset Clone() => FromJson(ToJson()) ?? CreateDefault51();
}
