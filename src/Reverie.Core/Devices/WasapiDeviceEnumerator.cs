using Reverie.Core.Models;
using NAudio.CoreAudioApi;

namespace Reverie.Core.Devices;

/// <summary>基于 WASAPI (NAudio) 的设备与源枚举。</summary>
public sealed class WasapiDeviceEnumerator : IAudioDeviceEnumerator
{
    public IReadOnlyList<AudioDeviceInfo> GetRenderDevices()
    {
        var result = new List<AudioDeviceInfo>();
        using var enumerator = new MMDeviceEnumerator();

        string? defaultId = null;
        try
        {
            defaultId = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;
        }
        catch
        {
            // 系统可能没有默认渲染设备
        }

        foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            using (device)
            {
                result.Add(FromDevice(device, defaultId));
            }
        }

        return result;
    }

    public IReadOnlyList<AudioSourceInfo> GetAudioSources()
    {
        var result = new List<AudioSourceInfo>();
        using var enumerator = new MMDeviceEnumerator();

        foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
        {
            using (device)
            {
                result.Add(ToSource(device, isLoopback: false));
            }
        }

        foreach (var device in enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active))
        {
            using (device)
            {
                result.Add(ToSource(device, isLoopback: true));
            }
        }

        return result;
    }

    private static AudioDeviceInfo FromDevice(MMDevice device, string? defaultId)
    {
        int channels = 2;
        int sampleRate = 48000;
        int bitDepth = 32;
        string type = "Unknown";

        try
        {
            var mix = device.AudioClient.MixFormat;
            channels = mix.Channels;
            sampleRate = mix.SampleRate;
            bitDepth = mix.BitsPerSample;
        }
        catch
        {
            // 某些虚拟设备 MixFormat 可能取不到
        }

        try
        {
            type = Classify(device.ID, device.FriendlyName);
        }
        catch
        {
            // ignore
        }

        return new AudioDeviceInfo
        {
            Id = device.ID,
            Name = device.FriendlyName,
            ChannelCount = channels,
            SampleRate = sampleRate,
            BitDepth = bitDepth,
            IsDefault = string.Equals(device.ID, defaultId, StringComparison.OrdinalIgnoreCase),
            DeviceType = type,
        };
    }

    private static AudioSourceInfo ToSource(MMDevice device, bool isLoopback)
    {
        int channels = 2;
        int rate = 48000;
        try
        {
            var mix = device.AudioClient.MixFormat;
            channels = mix.Channels;
            rate = mix.SampleRate;
        }
        catch
        {
            // ignore
        }

        string name = isLoopback ? $"回环：{device.FriendlyName}" : device.FriendlyName;

        return new AudioSourceInfo
        {
            Id = device.ID,
            Name = name,
            IsLoopback = isLoopback,
            ChannelCount = channels,
            SampleRate = rate,
            Kind = isLoopback ? "Loopback" : Classify(device.ID, device.FriendlyName),
        };
    }

    private static string Classify(string id, string name)
    {
        var text = $"{id} {name}".ToLowerInvariant();
        if (text.Contains("bluetooth") || text.Contains("a2dp") || text.Contains("蓝牙"))
            return "Bluetooth";
        if (text.Contains("hdmi") || text.Contains("displayport") || text.Contains("nvidia") || text.Contains("amd"))
            return "HDMI/DP";
        if (text.Contains("realtek") || text.Contains("speaker") || text.Contains("扬声器") || text.Contains("headphone") || text.Contains("耳机"))
            return "Analog";
        if (text.Contains("usb"))
            return "USB";
        return "Other";
    }
}
