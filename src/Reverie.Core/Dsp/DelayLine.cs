namespace Reverie.Core.Dsp;

/// <summary>每路一个整数采样延时线（环形缓冲）。</summary>
public sealed class DelayLine
{
    private readonly float[] _buffer;
    private int _write;

    public DelayLine(int maxSamples = 48000 * 2)
    {
        _buffer = new float[Math.Max(4, maxSamples)];
    }

    /// <summary>当前延时（采样数）。实时可改。</summary>
    public int DelaySamples { get; set; }

    public float Process(float x)
    {
        _buffer[_write] = x;

        int read = _write - DelaySamples;
        if (read < 0)
            read += _buffer.Length;

        float y = _buffer[read];
        _write++;
        if (_write >= _buffer.Length)
            _write = 0;
        return y;
    }

    public void Reset() => Array.Clear(_buffer, 0, _buffer.Length);
}
