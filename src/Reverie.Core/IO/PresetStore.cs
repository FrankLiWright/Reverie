using Reverie.Core.Models;

namespace Reverie.Core.IO;

/// <summary>预设 JSON 读写。</summary>
public static class PresetStore
{
    public static void Save(LayoutPreset preset, string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(path, preset.ToJson());
    }

    public static LayoutPreset Load(string path)
    {
        var json = File.ReadAllText(path);
        var preset = LayoutPreset.FromJson(json)
            ?? throw new InvalidDataException("无法解析预设文件");
        return preset;
    }
}
