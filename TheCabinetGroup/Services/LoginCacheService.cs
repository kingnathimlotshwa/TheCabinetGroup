using System;
using System.IO;
using Newtonsoft.Json;

namespace TheCabinetGroup.Services;

/// <summary>
/// File-backed session cache.
///
/// Resolves a safe writable path on every platform:
///   Windows : %LOCALAPPDATA%\TheCabinetGroup\session.json
///   Linux   : ~/.local/share/TheCabinetGroup/session.json
///   macOS   : ~/Library/Application Support/TheCabinetGroup/session.json
///   Android : /data/data/{package}/files/TheCabinetGroup/session.json
///             (via Environment.SpecialFolder.Personal, which maps correctly)
///   iOS     : {NSDocumentDirectory}/TheCabinetGroup/session.json
///
/// The session is only persisted when the user ticks "Remember me".
/// It expires after <see cref="CacheDuration"/>.
/// </summary>
public sealed class LoginCacheService : ILoginCacheService
{
    public static readonly TimeSpan CacheDuration = TimeSpan.FromHours(2);

    private static readonly string CacheDir  = GetCacheDir();
    private static readonly string CachePath = Path.Combine(CacheDir, "session.json");

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        DateFormatHandling   = DateFormatHandling.IsoDateFormat
    };

    /// <summary>
    /// Returns a writable directory on all platforms including Android.
    /// On Android, SpecialFolder.Personal maps to the app's internal files directory,
    /// which is always writable without any permissions.
    /// </summary>
    private static string GetCacheDir()
    {
        // SpecialFolder.Personal works on Android (internal files dir),
        // iOS (Documents), and all desktop platforms.
        // LocalApplicationData is preferred on desktop but returns empty on Android.
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (string.IsNullOrEmpty(baseDir))
            baseDir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

        // Final fallback — should never be needed but prevents a null path crash
        if (string.IsNullOrEmpty(baseDir))
            baseDir = AppContext.BaseDirectory;

        return Path.Combine(baseDir, "TheCabinetGroup");
    }

    public SessionCache? Load()
    {
        try
        {
            if (!File.Exists(CachePath)) return null;
            var json = File.ReadAllText(CachePath);
            return JsonConvert.DeserializeObject<SessionCache>(json, JsonSettings);
        }
        catch
        {
            return null;
        }
    }

    public void Save(SessionCache cache)
    {
        try
        {
            Directory.CreateDirectory(CacheDir);
            File.WriteAllText(
                CachePath,
                JsonConvert.SerializeObject(cache, Formatting.Indented, JsonSettings));
        }
        catch
        {
            // Non-fatal — user will be prompted to log in again next launch.
        }
    }

    public void Clear()
    {
        try
        {
            if (File.Exists(CachePath))
                File.Delete(CachePath);
        }
        catch { }
    }

    public bool IsValid(SessionCache? cache) =>
        cache is { RememberMe: true } && DateTime.UtcNow < cache.ExpiresAt;
}
