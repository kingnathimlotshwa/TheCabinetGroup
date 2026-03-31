using CommunityToolkit.Mvvm.Messaging;

using Jab;

using TheCabinetGroup.DialogControls.DialogModels;
using TheCabinetGroup.Injections;
using TheCabinetGroup.Models;
using TheCabinetGroup.Services;
using TheCabinetGroup.ViewModels;

namespace TheCabinetGroup.Utils;

[
    ServiceProvider,
    Singleton(typeof(AppwriteConfig), Factory = nameof(CreateAppwriteConfig)),
    Singleton(typeof(IAppwriteService), typeof(AppwriteService)),
    Singleton(typeof(ILoginCacheService), typeof(LoginCacheService)),
    Transient<AuthViewModel>,
    Singleton<MainViewModel>,
    Import<IUtilitiesModule>,
    Singleton<IMessenger, WeakReferenceMessenger>
]
public partial class ServiceProvider
{
    /// <summary>
    /// Factory method for AppwriteConfig — Jab calls this to create the singleton.
    /// Reads from appsettings.json + User Secrets via ConfigurationLoader.
    /// </summary>
    public static AppwriteConfig CreateAppwriteConfig() => ConfigurationLoader.Load();
}
