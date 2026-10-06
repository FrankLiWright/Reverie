using System.Text.Json;

namespace Reverie.Core.IO;

/// <summary>界面与引擎偏好的本地持久化（%AppData%/Reverie/settings.json）。</summary>
public sealed class UserSettings
{
    public string LayoutId { get; set; } = "2.0";
    public bool FreeMove { get; set; }
    public bool EnableUpmix { get; set; } = true;
    public bool ShowAdvanced { get; set; }
    public double WindowWidth { get; set; } = 1180;
    public double WindowHeight { get; set; } = 740;

    public static string DefaultPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Reverie", "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static UserSettings Load(string? path = null)
    {
        try
        {
            path ??= DefaultPath;
            if (!File.Exists(path))
                return new UserSettings();
            return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(path), JsonOptions)
                   ?? new UserSettings();
        }
        catch
        {
            return new UserSettings();
        }
    }

    public void Save(string? path = null)
    {
        try
        {
            path ??= DefaultPath;
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
            // 设置写入失败不影响主流程
        }
    }
}
