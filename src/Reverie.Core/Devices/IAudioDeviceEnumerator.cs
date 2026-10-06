using Reverie.Core.Models;

namespace Reverie.Core.Devices;

public interface IAudioDeviceEnumerator
{
    /// <summary>枚举当前活跃的渲染设备。</summary>
    IReadOnlyList<AudioDeviceInfo> GetRenderDevices();

    /// <summary>枚举音频源：捕获设备 + 渲染设备回环。</summary>
    IReadOnlyList<AudioSourceInfo> GetAudioSources();
}
