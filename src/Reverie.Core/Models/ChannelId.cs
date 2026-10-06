namespace Reverie.Core.Models;

/// <summary>逻辑声道 ID（与布局图上的音箱标签一致）。</summary>
public enum ChannelId
{
    FL,
    FR,
    FC,
    LFE,
    LFE2,
    SL,
    SR,
    RL,
    RR,
    TFL,
    TFR,
    TFC,
    TRL,
    TRR,
}

public static class ChannelIdExtensions
{
    public static string ToDisplay(this ChannelId id) => id switch
    {
        ChannelId.FL => "FL 前左",
        ChannelId.FR => "FR 前右",
        ChannelId.FC => "FC 中置",
        ChannelId.LFE => "LFE 低音1",
        ChannelId.LFE2 => "LFE2 低音2",
        ChannelId.SL => "SL 侧左",
        ChannelId.SR => "SR 侧右",
        ChannelId.RL => "RL 后左",
        ChannelId.RR => "RR 后右",
        ChannelId.TFL => "TFL 顶前左",
        ChannelId.TFR => "TFR 顶前右",
        ChannelId.TFC => "TFC 顶前中",
        ChannelId.TRL => "TRL 顶后左",
        ChannelId.TRR => "TRR 顶后右",
        _ => id.ToString(),
    };
}
