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

    [ObservableProperty] private Member? _currentMember;
    [ObservableProperty] private bool _isAuthenticated;

    private const string LoginDialogId = "LoginDialogHost";
    private const string MainDialogId = "MainDialogHost";

    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _errorMessage = string.Empty;


    // ── MEMBER ─────────────────────────────────────────────────────────
    public string MemberName => _currentMember.FullName;
    public string MemberRole => _currentMember.Role;
    public bool IsAdmin => _currentMember.Role == "admin";

    public event Action? LogoutRequested;

    public MainViewModel(
        IAppwriteService appwrite,
        ILoginCacheService cache,
        AuthViewModel authVm,
        ToastManager toastManager)
    {
        _appwrite     = appwrite;
        _cache        = cache;
        _authVm       = authVm;
        _toastManager = toastManager;

        _authVm.LoginSucceeded += OnLoginSucceeded;
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
                cached!.UserId, cached.SessionSecret);

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
    public void OnMemberAuthenticated(Member member)
    {
        CurrentMember = member;
        IsAuthenticated = true;
    }

    // ── LoginSucceeded handler ─────────────────────────────────────────────
    private void OnLoginSucceeded(Member member)
    {
        CurrentMember   = member;
        IsAuthenticated = true;

        if (DialogHost.IsDialogOpen(LoginDialogId))
            DialogHost.Close(LoginDialogId, member);
    }

    // ── Shell commands ─────────────────────────────────────────────────────
    [RelayCommand]
    private void ShowToast()
    {
        ToastManager?.CreateToast($"Hello, {CurrentMember?.FullName ?? "there"}!")
                    .Show(Notification.Info);
    }

    [RelayCommand]
    private async Task ShowConfirmDialog()
    {
        var vm   = new ConfirmDialogViewModel("Confirm Action", "Are you sure you want to proceed?");
        var view = new ConfirmDialogView { DataContext = vm };

        var result = await DialogHost.Show(view, MainDialogId);

        if (result is true)
            ToastManager?.CreateToast("Confirmed").WithContent("Action was confirmed.").ShowSuccess();
        else
            ToastManager?.CreateToast("Cancelled").WithContent("Action was cancelled.").ShowWarning();
    }

}
