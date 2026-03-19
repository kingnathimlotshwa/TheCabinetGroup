using System;

namespace TheCabinetGroup.Services;

/// <summary>
/// Persists a valid Appwrite session to disk so users don't have to
/// re-authenticate on every launch. Cache is considered valid for
/// <see cref="LoginCacheService.CacheDuration"/> (2 hours by default).
/// When RememberMe is false the entry is never written to disk.
/// </summary>
public interface ILoginCacheService
{
    /// <summary>Load the persisted session, or null if none exists.</summary>
    SessionCache? Load();

    /// <summary>Write the session to disk.</summary>
    void Save(SessionCache cache);

    /// <summary>Delete the persisted session (e.g. on logout).</summary>
    void Clear();

    /// <summary>
    /// Returns true when <paramref name="cache"/> is non-null,
    /// has RememberMe set and has not yet expired.
    /// </summary>
    bool IsValid(SessionCache? cache);
}

/// <summary>Value object stored in the on-disk JSON cache file.</summary>
public sealed class SessionCache
{
    public string UserId        { get; set; } = string.Empty;
    public string SessionSecret { get; set; } = string.Empty;
    public DateTime ExpiresAt   { get; set; }
    public bool RememberMe      { get; set; }
}