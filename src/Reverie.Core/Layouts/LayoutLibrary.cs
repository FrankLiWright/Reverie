using Reverie.Core.Models;

namespace Reverie.Core.Layouts;

/// <summary>内置布局库。</summary>
public static class LayoutLibrary
{
    public sealed record LayoutInfo(
        string Id,
        string Name,
        string Description,
        IReadOnlyList<SpeakerPosition> Speakers,
        IReadOnlyList<(ChannelId Ch, DeviceRole Role, int DeviceChannel, double GainDb)> Plan);

    private static SpeakerPosition P(ChannelId id, double x, double y, int height = 0) =>
        new() { Id = id, X = x, Y = y, HeightTier = height };

    public static IReadOnlyList<LayoutInfo> All { get; } =
    [
        new("2.0", "2.0 立体声", "左右两声道",
        [
            P(ChannelId.FL, 0.32, 0.18), P(ChannelId.FR, 0.68, 0.18),
        ],
        [
            (ChannelId.FL, DeviceRole.Front, 1, 0),
            (ChannelId.FR, DeviceRole.Front, 2, 0),
        ]),

        new("2.1", "2.1 立体声 + 低音", "左右 + 独立低音",
        [
            P(ChannelId.FL, 0.32, 0.18), P(ChannelId.FR, 0.68, 0.18),
            P(ChannelId.LFE, 0.50, 0.28),
        ],
        [
            (ChannelId.FL, DeviceRole.Front, 1, 0),
            (ChannelId.FR, DeviceRole.Front, 2, 0),
            (ChannelId.LFE, DeviceRole.Sub, 1, 0),
        ]),

        new("3.1", "3.1 前场 + 低音", "前左 / 前右 / 中置 + 低音",
        [
            P(ChannelId.FL, 0.30, 0.18), P(ChannelId.FR, 0.70, 0.18),
            P(ChannelId.FC, 0.50, 0.12),
            P(ChannelId.LFE, 0.50, 0.28),
        ],
        [
            (ChannelId.FL, DeviceRole.Front, 1, 0),
            (ChannelId.FR, DeviceRole.Front, 2, 0),
            (ChannelId.FC, DeviceRole.Front, 3, 0),
            (ChannelId.LFE, DeviceRole.Sub, 1, 0),
        ]),

        new("4.0", "4.0 四方", "前左右 + 后左右",
        [
            P(ChannelId.FL, 0.30, 0.18), P(ChannelId.FR, 0.70, 0.18),
            P(ChannelId.RL, 0.28, 0.82), P(ChannelId.RR, 0.72, 0.82),
        ],
        [
            (ChannelId.FL, DeviceRole.Front, 1, 0),
            (ChannelId.FR, DeviceRole.Front, 2, 0),
            (ChannelId.RL, DeviceRole.Surround, 1, 0),
            (ChannelId.RR, DeviceRole.Surround, 2, 0),
        ]),

        new("5.1", "5.1 环绕", "标准 5.1",
        [
            P(ChannelId.FL, 0.30, 0.18), P(ChannelId.FR, 0.70, 0.18),
            P(ChannelId.FC, 0.50, 0.12), P(ChannelId.LFE, 0.50, 0.28),
            P(ChannelId.RL, 0.28, 0.82), P(ChannelId.RR, 0.72, 0.82),
        ],
        [
            (ChannelId.FL, DeviceRole.Front, 1, 0),
            (ChannelId.FR, DeviceRole.Front, 2, 0),
            (ChannelId.FC, DeviceRole.Front, 3, 0),
            (ChannelId.LFE, DeviceRole.Front, 4, 0),
            (ChannelId.RL, DeviceRole.Surround, 1, 0),
            (ChannelId.RR, DeviceRole.Surround, 2, 0),
        ]),

        new("5.1.2", "5.1.2 含高度", "5.1 + 两只顶置",
        [
            P(ChannelId.FL, 0.28, 0.18), P(ChannelId.FR, 0.72, 0.18),
            P(ChannelId.FC, 0.50, 0.12), P(ChannelId.LFE, 0.50, 0.28),
            P(ChannelId.RL, 0.28, 0.82), P(ChannelId.RR, 0.72, 0.82),
            P(ChannelId.TFL, 0.32, 0.32, 1), P(ChannelId.TFR, 0.68, 0.32, 1),
        ],
        [
            (ChannelId.FL, DeviceRole.Front, 1, 0),
            (ChannelId.FR, DeviceRole.Front, 2, 0),
            (ChannelId.FC, DeviceRole.Front, 3, 0),
            (ChannelId.LFE, DeviceRole.Front, 4, 0),
            (ChannelId.RL, DeviceRole.Surround, 1, 0),
            (ChannelId.RR, DeviceRole.Surround, 2, 0),
            (ChannelId.TFL, DeviceRole.Height, 1, 0),
            (ChannelId.TFR, DeviceRole.Height, 2, 0),
        ]),

        new("5.1.4", "5.1.4 四顶置", "5.1 + 四只顶置",
        [
            P(ChannelId.FL, 0.28, 0.18), P(ChannelId.FR, 0.72, 0.18),
            P(ChannelId.FC, 0.50, 0.12), P(ChannelId.LFE, 0.50, 0.28),
            P(ChannelId.RL, 0.28, 0.82), P(ChannelId.RR, 0.72, 0.82),
            P(ChannelId.TFL, 0.30, 0.30, 1), P(ChannelId.TFR, 0.70, 0.30, 1),
            P(ChannelId.TRL, 0.30, 0.68, 1), P(ChannelId.TRR, 0.70, 0.68, 1),
        ],
        [
            (ChannelId.FL, DeviceRole.Front, 1, 0),
            (ChannelId.FR, DeviceRole.Front, 2, 0),
            (ChannelId.FC, DeviceRole.Front, 3, 0),
            (ChannelId.LFE, DeviceRole.Front, 4, 0),
            (ChannelId.RL, DeviceRole.Surround, 1, 0),
            (ChannelId.RR, DeviceRole.Surround, 2, 0),
            (ChannelId.TFL, DeviceRole.Height, 1, 0),
            (ChannelId.TFR, DeviceRole.Height, 2, 0),
            (ChannelId.TRL, DeviceRole.Height, 3, 0),
            (ChannelId.TRR, DeviceRole.Height, 4, 0),
        ]),

        new("7.1", "7.1 环绕", "前 / 侧 / 后",
        [
            P(ChannelId.FL, 0.28, 0.16), P(ChannelId.FR, 0.72, 0.16),
            P(ChannelId.FC, 0.50, 0.10), P(ChannelId.LFE, 0.50, 0.26),
            P(ChannelId.SL, 0.14, 0.50), P(ChannelId.SR, 0.86, 0.50),
            P(ChannelId.RL, 0.28, 0.84), P(ChannelId.RR, 0.72, 0.84),
        ],
        [
            (ChannelId.FL, DeviceRole.Front, 1, 0),
            (ChannelId.FR, DeviceRole.Front, 2, 0),
            (ChannelId.FC, DeviceRole.Front, 3, 0),
            (ChannelId.LFE, DeviceRole.Front, 4, 0),
            (ChannelId.SL, DeviceRole.Surround, 1, 0),
            (ChannelId.SR, DeviceRole.Surround, 2, 0),
            (ChannelId.RL, DeviceRole.Surround, 3, 0),
            (ChannelId.RR, DeviceRole.Surround, 4, 0),
        ]),

        new("7.1.2", "7.1.2 含高度", "7.1 + 两只顶置",
        [
            P(ChannelId.FL, 0.28, 0.16), P(ChannelId.FR, 0.72, 0.16),
            P(ChannelId.FC, 0.50, 0.10), P(ChannelId.LFE, 0.50, 0.26),
            P(ChannelId.SL, 0.14, 0.50), P(ChannelId.SR, 0.86, 0.50),
            P(ChannelId.RL, 0.28, 0.84), P(ChannelId.RR, 0.72, 0.84),
            P(ChannelId.TFL, 0.32, 0.30, 1), P(ChannelId.TFR, 0.68, 0.30, 1),
        ],
        [
            (ChannelId.FL, DeviceRole.Front, 1, 0),
            (ChannelId.FR, DeviceRole.Front, 2, 0),
            (ChannelId.FC, DeviceRole.Front, 3, 0),
            (ChannelId.LFE, DeviceRole.Front, 4, 0),
            (ChannelId.SL, DeviceRole.Surround, 1, 0),
            (ChannelId.SR, DeviceRole.Surround, 2, 0),
            (ChannelId.RL, DeviceRole.Surround, 3, 0),
            (ChannelId.RR, DeviceRole.Surround, 4, 0),
            (ChannelId.TFL, DeviceRole.Height, 1, 0),
            (ChannelId.TFR, DeviceRole.Height, 2, 0),
        ]),

        new("7.1.4", "7.1.4 四顶置", "7.1 + 四只顶置",
        [
            P(ChannelId.FL, 0.28, 0.16), P(ChannelId.FR, 0.72, 0.16),
            P(ChannelId.FC, 0.50, 0.10), P(ChannelId.LFE, 0.50, 0.26),
            P(ChannelId.SL, 0.14, 0.50), P(ChannelId.SR, 0.86, 0.50),
            P(ChannelId.RL, 0.28, 0.84), P(ChannelId.RR, 0.72, 0.84),
            P(ChannelId.TFL, 0.30, 0.28, 1), P(ChannelId.TFR, 0.70, 0.28, 1),
            P(ChannelId.TRL, 0.30, 0.70, 1), P(ChannelId.TRR, 0.70, 0.70, 1),
        ],
        [
            (ChannelId.FL, DeviceRole.Front, 1, 0),
            (ChannelId.FR, DeviceRole.Front, 2, 0),
            (ChannelId.FC, DeviceRole.Front, 3, 0),
            (ChannelId.LFE, DeviceRole.Front, 4, 0),
            (ChannelId.SL, DeviceRole.Surround, 1, 0),
            (ChannelId.SR, DeviceRole.Surround, 2, 0),
            (ChannelId.RL, DeviceRole.Surround, 3, 0),
            (ChannelId.RR, DeviceRole.Surround, 4, 0),
            (ChannelId.TFL, DeviceRole.Height, 1, 0),
            (ChannelId.TFR, DeviceRole.Height, 2, 0),
            (ChannelId.TRL, DeviceRole.Height, 3, 0),
            (ChannelId.TRR, DeviceRole.Height, 4, 0),
        ]),

        new("7.2.1", "7.2.1 双低音 + 顶置", "7 声道 + 双低音 + 1 顶置",
        [
            P(ChannelId.FL, 0.28, 0.16), P(ChannelId.FR, 0.72, 0.16),
            P(ChannelId.FC, 0.50, 0.10),
            P(ChannelId.LFE, 0.38, 0.28), P(ChannelId.LFE2, 0.62, 0.28),
            P(ChannelId.SL, 0.14, 0.50), P(ChannelId.SR, 0.86, 0.50),
            P(ChannelId.RL, 0.28, 0.84), P(ChannelId.RR, 0.72, 0.84),
            P(ChannelId.TFC, 0.50, 0.28, 1),
        ],
        [
            (ChannelId.FL, DeviceRole.Front, 1, 0),
            (ChannelId.FR, DeviceRole.Front, 2, 0),
            (ChannelId.FC, DeviceRole.Front, 3, 0),
            (ChannelId.LFE, DeviceRole.Sub, 1, 0),
            (ChannelId.LFE2, DeviceRole.Sub, 2, 0),
            (ChannelId.SL, DeviceRole.Surround, 1, 0),
            (ChannelId.SR, DeviceRole.Surround, 2, 0),
            (ChannelId.RL, DeviceRole.Surround, 3, 0),
            (ChannelId.RR, DeviceRole.Surround, 4, 0),
            (ChannelId.TFC, DeviceRole.Height, 1, 0),
        ]),
    ];

    public static LayoutInfo? Find(string id) =>
        All.FirstOrDefault(l => string.Equals(l.Id, id, StringComparison.OrdinalIgnoreCase));

    public static LayoutPreset ToPreset(LayoutInfo info)
    {
        var preset = new LayoutPreset
        {
            Name = info.Name,
            Description = info.Description,
            LayoutTag = info.Id,
            Speakers = info.Speakers.Select(s => new SpeakerPosition
            {
                Id = s.Id,
                X = s.X,
                Y = s.Y,
                HeightTier = s.HeightTier,
            }).ToList(),
        };

        foreach (var (ch, role, devCh, gain) in info.Plan)
        {
            preset.Routes.Add(new ChannelRoute
            {
                Channel = ch,
                DeviceChannel = devCh,
                GainDb = gain,
            });
        }

        return preset;
    }
}
