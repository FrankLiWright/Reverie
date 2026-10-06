using Reverie.Core.Models;

namespace Reverie.Core.Dsp;

/// <summary>多声道输入 → ChannelId 槽位映射（直通，不参与上混）。</summary>
public static class ChannelMapper
{
    public static int SlotCount => Enum.GetValues<ChannelId>().Length;

    public static void Clear(float[][] outSlots, int frames)
    {
        for (int c = 0; c < outSlots.Length; c++)
            Array.Clear(outSlots[c], 0, frames);
    }

    /// <summary>多声道（≥3）直通：按常见 WASAPI / WAV 顺序映射。</summary>
    public static void DirectMap(ReadOnlySpan<float> interleavedIn, int frames, int inputChannels, float[][] outSlots)
    {
        Clear(outSlots, frames);

        Span<ChannelId> map = stackalloc ChannelId[Math.Min(inputChannels, 8)];

        switch (inputChannels)
        {
            case 3:
                map[0] = ChannelId.FL; map[1] = ChannelId.FR; map[2] = ChannelId.FC;
                break;
            case 4:
                map[0] = ChannelId.FL; map[1] = ChannelId.FR;
                map[2] = ChannelId.SL; map[3] = ChannelId.SR;
                break;
            case 5:
                map[0] = ChannelId.FL; map[1] = ChannelId.FR; map[2] = ChannelId.FC;
                map[3] = ChannelId.SL; map[4] = ChannelId.SR;
                break;
            default:
                map[0] = ChannelId.FL; map[1] = ChannelId.FR; map[2] = ChannelId.FC;
                map[3] = ChannelId.LFE;
                map[4] = ChannelId.SL; map[5] = ChannelId.SR;
                if (inputChannels >= 7) map[6] = ChannelId.RL;
                if (inputChannels >= 8) map[7] = ChannelId.RR;
                break;
        }

        int n = Math.Min(inputChannels, map.Length);
        for (int i = 0; i < frames; i++)
        {
            int b = i * inputChannels;
            for (int c = 0; c < n; c++)
            {
                int slot = (int)map[c];
                if (slot >= 0 && slot < outSlots.Length)
                    outSlots[slot][i] += interleavedIn[b + c];
            }
        }
    }

    /// <summary>立体声直通：仅 FL/FR。</summary>
    public static void StereoPassthrough(ReadOnlySpan<float> interleavedIn, int frames, int inputChannels, float[][] outSlots)
    {
        Clear(outSlots, frames);

        for (int i = 0; i < frames; i++)
        {
            float l, r;
            if (inputChannels <= 1)
            {
                l = r = interleavedIn[i];
            }
            else
            {
                int b = i * inputChannels;
                l = interleavedIn[b];
                r = interleavedIn[b + 1];
            }
            outSlots[(int)ChannelId.FL][i] = l;
            outSlots[(int)ChannelId.FR][i] = r;
        }
    }
}
