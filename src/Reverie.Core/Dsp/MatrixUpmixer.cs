using Reverie.Core.Models;

namespace Reverie.Core.Dsp;

/// <summary>立体声 → N 声道矩阵上混。输出声道随布局配置。</summary>
public sealed class MatrixUpmixer
{
    private readonly object _gate = new();
    private ChannelId[] _outputs = [ChannelId.FL, ChannelId.FR];
    private float[] _gL = [0.707f, 0f];
    private float[] _gR = [0f, 0.707f];

    public int ChannelCount
    {
        get
        {
            lock (_gate)
                return _outputs.Length;
        }
    }

    public ChannelId[] OutputChannels
    {
        get
        {
            lock (_gate)
                return (ChannelId[])_outputs.Clone();
        }
    }

    public void Configure(IReadOnlyList<ChannelId> outputs)
    {
        if (outputs.Count == 0)
            return;

        lock (_gate)
        {
            _outputs = outputs.ToArray();
            _gL = new float[_outputs.Length];
            _gR = new float[_outputs.Length];
            for (int i = 0; i < _outputs.Length; i++)
                SetDefaults(i, _outputs[i]);
        }
    }

    private void SetDefaults(int index, ChannelId ch)
    {
        var (l, r) = DefaultGains(ch);
        _gL[index] = l;
        _gR[index] = r;
    }

    public static (float L, float R) DefaultGains(ChannelId ch) => ch switch
    {
        ChannelId.FL => (0.707f, 0f),
        ChannelId.FR => (0f, 0.707f),
        ChannelId.FC => (0.5f, 0.5f),
        ChannelId.LFE or ChannelId.LFE2 => (0.3f, 0.3f),
        ChannelId.SL or ChannelId.RL or ChannelId.TFL or ChannelId.TRL => (0.5f, 0f),
        ChannelId.SR or ChannelId.RR or ChannelId.TFR or ChannelId.TRR => (0f, 0.5f),
        ChannelId.TFC => (0.35f, 0.35f),
        _ => (0.5f, 0.5f),
    };

    public void SetGains(int index, float l, float r)
    {
        lock (_gate)
        {
            if (index < 0 || index >= _gL.Length)
                return;
            _gL[index] = l;
            _gR[index] = r;
        }
    }

    public (float L, float R) GetGains(int index)
    {
        lock (_gate)
        {
            if (index < 0 || index >= _gL.Length)
                return (0, 0);
            return (_gL[index], _gR[index]);
        }
    }

    public void ResetToDefaults()
    {
        lock (_gate)
        {
            for (int i = 0; i < _outputs.Length; i++)
                SetDefaults(i, _outputs[i]);
        }
    }

    /// <summary>输入交错立体声 → 写入 outSlots[(int)ChannelId]。</summary>
    public void Process(ReadOnlySpan<float> interleavedIn, int frames, int inputChannels, float[][] outSlots)
    {
        float[] gL, gR;
        ChannelId[] outputs;

        lock (_gate)
        {
            outputs = _outputs;
            gL = _gL;
            gR = _gR;
        }

        foreach (var ch in outputs)
            Array.Clear(outSlots[(int)ch], 0, frames);

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

            for (int k = 0; k < outputs.Length; k++)
            {
                int slot = (int)outputs[k];
                outSlots[slot][i] = l * gL[k] + r * gR[k];
            }
        }
    }
}
