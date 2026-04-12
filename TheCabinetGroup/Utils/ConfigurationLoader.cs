using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using TheCabinetGroup.Models;

namespace TheCabinetGroup.Utils;

public class ConfigurationLoader
{
    private static AppwriteConfig? _cached;

    /// <summary>
    /// Loads Appwrite configuration from appsettings.json.
    ///
    /// Desktop/Server: reads from AppContext.BaseDirectory/Configurations/appsettings.json
    /// Android/iOS   : reads from the embedded assembly resource, because there is no
    ///                 writable file system path available at AppContext.BaseDirectory.
    ///
    /// Sensitive keys (ProjectId, DatabaseId, BucketId) must be present in
    /// appsettings.json directly for mobile builds, or via User Secrets on desktop.
    /// </summary>
    public static AppwriteConfig Load()
    {
        if (_cached != null) return _cached;

        var builder = new ConfigurationBuilder();

        // Try file-based config first (Desktop / CI)
        var filePath = Path.Combine(AppContext.BaseDirectory, "Configurations", "appsettings.json");
        if (File.Exists(filePath))
        {
            builder.AddJsonFile(filePath, optional: false, reloadOnChange: false);
        }
        else
        {
            // Android / iOS: load appsettings.json as an embedded assembly resource.
            // In the .csproj set: <EmbeddedResource Include="Configurations\appsettings.json" />
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "TheCabinetGroup.Configurations.appsettings.json";
            var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream is not null)
                builder.AddJsonStream(stream);
        }

        // User Secrets (Desktop development only — not available on Android)
        var userId = Environment.GetEnvironmentVariable("APPWRITE_PROJECTID", EnvironmentVariableTarget.User);
        var env = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
        if (env.Equals("Development", StringComparison.OrdinalIgnoreCase))
            builder.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);

        var config  = builder.Build();
        var section = config.GetSection("Appwrite");

        _cached = new AppwriteConfig
        {
            Endpoint   = section["Endpoint"]   ?? "https://fra.cloud.appwrite.io/v1",
            ProjectId  = section["ProjectId"]  ?? string.Empty,
            DatabaseId = section["DatabaseId"] ?? string.Empty,
            BucketId   = section["BucketId"]   ?? string.Empty,
            Collections = new CollectionIds
            {
                Profiles       = section["Collections:Profiles"]       ?? "profiles",
                Payments      = section["Collections:Payments"]      ?? "payments",
                Penalties     = section["Collections:Penalties"]     ?? "penalties",
                Settings      = section["Collections:Settings"]      ?? "stokvel_settings"
            }
        };

        return _cached;
    }
}
