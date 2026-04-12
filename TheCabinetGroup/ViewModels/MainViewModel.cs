using System;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ShadUI;

using TheCabinetGroup.DialogControls.DialogModels;
using TheCabinetGroup.DialogControls.Dialogs;
using TheCabinetGroup.Models;
using TheCabinetGroup.Services;

using DialogHost = DialogHostAvalonia.DialogHost;

namespace TheCabinetGroup.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IAppwriteService _appwrite;
    private readonly ILoginCacheService _cache;
    private readonly AuthViewModel _authVm;

    [ObservableProperty] ToastManager? _toastManager;

    [ObservableProperty] private AppUser? _currentMember;
    [ObservableProperty] private bool _isAuthenticated;

    private const string LoginDialogId = "LoginDialogHost";
    private const string MainDialogId = "MainDialogHost";

    // ── MEMBER ─────────────────────────────────────────────────────────
    [ObservableProperty] public string _memberName = string.Empty;
    [ObservableProperty] public string _memberRole = string.Empty;
    [ObservableProperty] public bool _isAdmin = false;

    // ── PAGES ─────────────────────────────────────────────────────────
    [ObservableProperty] private UserProfileViewModel _profileVm = null!;
    [ObservableProperty] private PaymentViewModel _paymentVm = null!;

    public event Action? LogoutRequested;

    public MainViewModel(
        IAppwriteService appwrite,
        ILoginCacheService cache,
        AuthViewModel authVm,
        ToastManager toastManager): base(toastManager)
    {
        _appwrite = appwrite;
        _cache = cache;
        _authVm = authVm;
        _toastManager = toastManager;
        _authVm.LoginSucceeded += OnLoginSucceeded;

        // NEW: initialise the ProfileViewModel (shares the same ToastManager)
        ProfileVm = new UserProfileViewModel(appwrite, toastManager);
        ProfileVm.LogoutRequested += OnLogoutRequested;


        // CHANGED: Initialise ContributionViewModel so the Contributions tab is ready
        PaymentVm = new PaymentViewModel(appwrite, toastManager);
    }

    // Design-time constructor
    public MainViewModel() : this(null!, null!, null!, null!)
    {
    }


    // ── Triggered by MainWindow.axaml Opened event ─────────────────────────

    [RelayCommand]
    private async Task CheckAuthAsync()
    {
        // Step 1: try to silently restore a cached session
        var cached = _cache.Load();
        if (_cache.IsValid(cached))
        {
            var member = await _appwrite.TryRestoreSessionAsync(
                cached!.Email, cached.Password);

            if (member is not null)
            {
                OnLoginSucceeded(member);
                return;
            }

            // Expired server-side — discard stale cache
            _cache.Clear();
        }

        // Step 2: show the blocking login dialog
        var loginPage = new LoginPage { DataContext = _authVm };
        await DialogHost.Show(loginPage, LoginDialogId);
    }

    // ── Called by MainWindow once login succeeds ───────────────────────────
    /// <summary>
    /// Receives the authenticated <see cref="Member"/> from MainWindow and
    /// makes the main shell aware that the user is now logged in.
    /// </summary>
    public void OnMemberAuthenticated(AppUser user)
    {
        CurrentMember = user;
        IsAuthenticated = true;
    }

    // ── LoginSucceeded handler ─────────────────────────────────────────────
    private void OnLoginSucceeded(AppUser user)
    {
        CurrentMember = user;
        IsAuthenticated = true;

        MemberName = user.FullName;
        MemberRole = user.Role;
        IsAdmin = user.IsAdmin;

        // NEW: populate the Profile tab with the authenticated user's data
        // CHANGED: Guard against ProfileVm being null in edge cases
        if (ProfileVm is not null)
            ProfileVm.SetUser(user);

        // CHANGED: Populate the Contributions tab with the authenticated user's data
        if (PaymentVm is not null)
            PaymentVm.SetUser(user);

        if (DialogHost.IsDialogOpen(LoginDialogId))
            DialogHost.Close(LoginDialogId, user);
    }

    // ── Logout handler ────────────────────────────────────────────────────
    private void OnLogoutRequested()
    {
        CurrentMember = null;
        IsAuthenticated = false;
        MemberName = string.Empty;
        MemberRole = string.Empty;
        IsAdmin = false;

        // CHANGED: Re-create ProfileVm instead of nulling it, so OnLoginSucceeded
        // can safely call ProfileVm.SetUser() after the next login.
        // Also re-subscribe the logout handler on the new instance.
        ProfileVm = new UserProfileViewModel(_appwrite, _toastManager);
        ProfileVm.LogoutRequested += OnLogoutRequested;

        // CHANGED: Re-create ContributionVm on logout so stale data is cleared
        PaymentVm = new PaymentViewModel(_appwrite, _toastManager);

        var loginPage = new LoginPage { DataContext = _authVm };
        _ = DialogHost.Show(loginPage, LoginDialogId);
    }
}
