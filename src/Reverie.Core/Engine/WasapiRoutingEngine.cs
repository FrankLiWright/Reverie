using Reverie.Core.Dsp;
using Reverie.Core.Models;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Reverie.Core.Engine;

/// <summary>
/// WASAPI 路由引擎：源 → 矩阵上混/直通 → 每路延时/增益/极性 → 多设备渲染。
/// </summary>
public sealed class WasapiRoutingEngine : IDisposable
{
    private readonly object _gate = new();
    private readonly MatrixUpmixer _upmixer = new();

    private readonly Dictionary<RouteKey, DelayLine> _delays = new();
    private readonly Dictionary<string, DeviceOutput> _outputs = new(StringComparer.OrdinalIgnoreCase);

    private WasapiCapture? _capture;
    private WasapiLoopbackCapture? _loopback;
    private IWaveIn? _waveIn;

    private volatile bool _running;
    private volatile string _status = "已停止";

    public string Status
    {
        get => _status;
        private set => _status = value;
    }

    public bool IsRunning => _running;
    public MatrixUpmixer Upmix => _upmixer;

    /// <summary>立体声上混开关（仅对 1/2 声道源有效）。多声道源始终直通。</summary>
    public bool EnableUpmix { get; set; } = true;

    public void ConfigureUpmix(IReadOnlyList<ChannelId> outputs) => _upmixer.Configure(outputs);

    public event Action<string>? StatusChanged;
    public event Action<float[]>? LevelsChanged;

    private List<ChannelRoute>? _pendingRoutes;

    public void Start(string sourceId, bool sourceIsLoopback, IReadOnlyList<AudioDeviceInfo> renderDevices, IReadOnlyList<ChannelRoute> routes)
    {
        Stop();

        IWaveIn? waveIn = null;
        WaveFormat format = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
        var newOutputs = new Dictionary<string, DeviceOutput>(StringComparer.OrdinalIgnoreCase);
        var newDelays = new Dictionary<RouteKey, DelayLine>();
        List<ChannelRoute> pending;

        var enumerator = new MMDeviceEnumerator();
        try
        {
            if (sourceIsLoopback)
            {
                var device = FindDevice(enumerator, DataFlow.Render, sourceId);
                var loopback = new WasapiLoopbackCapture(device);
                _loopback = loopback;
                waveIn = loopback;
                format = loopback.WaveFormat;
            }
            else
            {
                var device = FindDevice(enumerator, DataFlow.Capture, sourceId);
                var capture = new WasapiCapture(device);
                _capture = capture;
                waveIn = capture;
                format = capture.WaveFormat;
            }

            var usedDeviceIds = routes
                .Where(r => !string.IsNullOrEmpty(r.DeviceId))
                .Select(r => r.DeviceId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var id in usedDeviceIds)
            {
                var info = renderDevices.FirstOrDefault(d => string.Equals(d.Id, id, StringComparison.OrdinalIgnoreCase));
                if (info is null)
                    continue;

                var mm = FindDevice(enumerator, DataFlow.Render, id);
                var outDev = new DeviceOutput(mm, info.ChannelCount, format.SampleRate);
                newOutputs[id] = outDev;
            }

            pending = routes.Select(r => r.Clone()).ToList();
            foreach (var route in pending)
            {
                if (string.IsNullOrEmpty(route.DeviceId) || !newOutputs.ContainsKey(route.DeviceId))
                    continue;
                var key = new RouteKey(route.Channel, route.DeviceId, route.DeviceChannel);
                newDelays[key] = new DelayLine { DelaySamples = MsToSamples(route.DelayMs, format.SampleRate) };
            }
        }
        catch
        {
            foreach (var kv in newOutputs)
            {
                try { kv.Value.Dispose(); } catch { /* ignore */ }
            }
            if (waveIn is not null)
            {
                try { waveIn.Dispose(); } catch { /* ignore */ }
            }
            throw;
        }

        lock (_gate)
        {
            _waveIn = waveIn;
            foreach (var kv in newOutputs)
                _outputs[kv.Key] = kv.Value;
            foreach (var kv in newDelays)
                _delays[kv.Key] = kv.Value;
            _pendingRoutes = pending;
        }

        foreach (var kv in newOutputs)
            kv.Value.Start();

        waveIn!.DataAvailable += OnDataAvailable;
        waveIn.RecordingStopped += OnRecordingStopped;
        waveIn.StartRecording();

        _running = true;
        SetStatus($"运行中 · 源 {format.SampleRate}Hz/{format.Channels}ch · 输出 {newOutputs.Count} 设备 · 路由 {routes.Count}");
    }

    public void Stop()
    {
        _running = false;

        IWaveIn? waveIn;
        List<DeviceOutput> outputs;
        lock (_gate)
        {
            waveIn = _waveIn;
            outputs = _outputs.Values.ToList();
            _waveIn = null;
            _capture = null;
            _loopback = null;
            _outputs.Clear();
            _delays.Clear();
            _pendingRoutes = null;
        }

        if (waveIn is not null)
        {
            try
            {
                waveIn.DataAvailable -= OnDataAvailable;
                waveIn.RecordingStopped -= OnRecordingStopped;
                waveIn.StopRecording();
            }
            catch
            {
                // ignore
            }

            try { waveIn.Dispose(); } catch { /* ignore */ }
        }

        foreach (var o in outputs)
        {
            try { o.Dispose(); } catch { /* ignore */ }
        }

        SetStatus("已停止");
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    public void UpdateRoutes(IReadOnlyList<ChannelRoute> routes)
    {
        lock (_gate)
        {
            if (!_running || _waveIn is null)
                return;

            int rate = _waveIn.WaveFormat.SampleRate;
            foreach (var route in routes)
            {
                if (string.IsNullOrEmpty(route.DeviceId))
                    continue;
                var key = new RouteKey(route.Channel, route.DeviceId, route.DeviceChannel);
                if (_delays.TryGetValue(key, out var line))
                    line.DelaySamples = MsToSamples(route.DelayMs, rate);
                else
                    _delays[key] = new DelayLine { DelaySamples = MsToSamples(route.DelayMs, rate) };
            }

            var live = routes
                .Where(r => !string.IsNullOrEmpty(r.DeviceId))
                .Select(r => new RouteKey(r.Channel, r.DeviceId, r.DeviceChannel))
                .ToHashSet();
            foreach (var key in _delays.Keys.ToList())
            {
                if (!live.Contains(key))
                    _delays.Remove(key);
            }

            _pendingRoutes = routes.Select(r => r.Clone()).ToList();
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (!_running)
            return;

        float pkL = 0f, pkR = 0f;
        try
        {
            lock (_gate)
            {
                if (!_running || _waveIn is null)
                    return;

                var fmt = _waveIn.WaveFormat;
                if (fmt.Encoding != WaveFormatEncoding.IeeeFloat)
                    return;

                int bytesPerSample = 4;
                int inputChannels = fmt.Channels;
                int frames = e.BytesRecorded / (bytesPerSample * Math.Max(1, inputChannels));
                if (frames <= 0)
                    return;

                var input = new float[frames * inputChannels];
                Buffer.BlockCopy(e.Buffer, 0, input, 0, frames * inputChannels * bytesPerSample);

                int slots = ChannelMapper.SlotCount;
                var planar = new float[slots][];
                for (int c = 0; c < slots; c++)
                    planar[c] = new float[frames];

                if (inputChannels > 2)
                    ChannelMapper.DirectMap(input, frames, inputChannels, planar);
                else if (EnableUpmix)
                    _upmixer.Process(input, frames, inputChannels, planar);
                else
                    ChannelMapper.StereoPassthrough(input, frames, inputChannels, planar);

                Peak(planar[(int)ChannelId.FL], out pkL);
                Peak(planar[(int)ChannelId.FR], out pkR);

                foreach (var kv in _outputs)
                    kv.Value.BeginFrame(frames);

                var routes = _pendingRoutes;
                if (routes is null)
                    return;

                foreach (var route in routes)
                {
                    if (route.Mute || string.IsNullOrEmpty(route.DeviceId))
                        continue;
                    if (!_outputs.TryGetValue(route.DeviceId, out var dev))
                        continue;

                    int logical = (int)route.Channel;
                    if (logical < 0 || logical >= slots)
                        continue;

                    var key = new RouteKey(route.Channel, route.DeviceId, route.DeviceChannel);
                    if (!_delays.TryGetValue(key, out var delay))
                    {
                        delay = new DelayLine();
                        _delays[key] = delay;
                    }

                    float gain = Gains.FromDb(route.GainDb) * (route.Polarity ? 1f : -1f);
                    int chIndex = route.DeviceChannel - 1;
                    if (chIndex < 0 || chIndex >= dev.ChannelCount)
                        continue;

                    var src = planar[logical];
                    var dst = dev.GetChannelBuffer(chIndex, frames);
                    for (int i = 0; i < frames; i++)
                    {
                        float y = delay.Process(src[i]) * gain;
                        dst[i] += y;
                    }
                }

                foreach (var kv in _outputs)
                    kv.Value.Push();
            }
        }
        catch (Exception ex)
        {
            SetStatus($"音频处理错误：{ex.Message}");
        }

        if (_running)
            LevelsChanged?.Invoke([pkL, pkR]);
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception is not null)
            SetStatus($"捕获已停止：{e.Exception.Message}");
    }

    private void SetStatus(string text)
    {
        Status = text;
        StatusChanged?.Invoke(text);
    }

    private static MMDevice FindDevice(MMDeviceEnumerator enumerator, DataFlow flow, string id)
    {
        foreach (var d in enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active))
        {
            if (string.Equals(d.ID, id, StringComparison.OrdinalIgnoreCase))
                return d;
        }
        throw new InvalidOperationException($"找不到设备：{id}");
    }

    private static int MsToSamples(double ms, int sampleRate) =>
        (int)Math.Clamp(Math.Round(ms / 1000.0 * sampleRate), 0, sampleRate * 2);

    private static void Peak(ReadOnlySpan<float> buf, out float peak)
    {
        float p = 0f;
        for (int i = 0; i < buf.Length; i++)
        {
            float a = Math.Abs(buf[i]);
            if (a > p) p = a;
        }
        peak = p;
    }

    private readonly record struct RouteKey(ChannelId Channel, string DeviceId, int DeviceChannel);

    private sealed class DeviceOutput : IDisposable
    {
        private readonly WasapiOut _out;
        private readonly BufferedWaveProvider _buffer;
        private readonly float[][] _channelBuf = new float[16][];
        private readonly int _channels;
        private float[] _interleaved = Array.Empty<float>();
        private int _frames;
        private bool _pushedAny;

        public int ChannelCount => _channels;

        public DeviceOutput(MMDevice device, int channels, int sampleRate)
        {
            _channels = Math.Clamp(channels, 1, 16);
            for (int c = 0; c < _channels; c++)
                _channelBuf[c] = new float[8192];

            var format = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, _channels);
            _buffer = new BufferedWaveProvider(format)
            {
                BufferDuration = TimeSpan.FromMilliseconds(400),
                DiscardOnBufferOverflow = true,
            };
            _out = new WasapiOut(device, AudioClientShareMode.Shared, false, 50);
            _out.Init(_buffer);
        }

        public void Start() => _out.Play();

        public void BeginFrame(int frames)
        {
            _frames = frames;
            if (_interleaved.Length < frames * _channels)
                _interleaved = new float[frames * _channels];
            Array.Clear(_interleaved, 0, frames * _channels);

            for (int c = 0; c < _channels; c++)
            {
                if (_channelBuf[c].Length < frames)
                    _channelBuf[c] = new float[frames];
                Array.Clear(_channelBuf[c], 0, frames);
            }
            _pushedAny = true;
        }

        public float[] GetChannelBuffer(int channel, int frames) => _channelBuf[channel];

        public void Push()
        {
            if (!_pushedAny || _frames <= 0)
                return;

            for (int i = 0; i < _frames; i++)
            {
                for (int c = 0; c < _channels; c++)
                    _interleaved[i * _channels + c] = _channelBuf[c][i];
            }

            var bytes = new byte[_frames * _channels * 4];
            Buffer.BlockCopy(_interleaved, 0, bytes, 0, bytes.Length);
            _buffer.AddSamples(bytes, 0, bytes.Length);
            _pushedAny = false;
        }

        public void Dispose()
        {
            try
            {
                _out.Stop();
                _out.Dispose();
            }
            catch
            {
                // ignore
            }
        }
    }
}
