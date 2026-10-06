namespace Reverie.Core.Dsp;

/// <summary>dB → 线性增益。</summary>
public static class Gains
{
    public static float FromDb(double db) =>
        db <= -60 ? 0f : (float)Math.Pow(10.0, db / 20.0);
}
