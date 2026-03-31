using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ShadUI;

using TheCabinetGroup.Helpers;
using TheCabinetGroup.Models;
using TheCabinetGroup.Services;
using TheCabinetGroup.Utils;

namespace TheCabinetGroup.ViewModels;

public partial class UserProfileViewModel : ViewModelBase
{
    private readonly IAppwriteService _appwrite;
    // ── Current user reference set from the shell ─────────────────────────
    private AppUser _currentUser = new();

    // ── Editable profile fields ───────────────────────────────────────────
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private string _idNumber = string.Empty;
    [ObservableProperty] private string _bankAccount = string.Empty;

    // ── Bank dropdown ─────────────────────────────────────────────────────
    // Populated once from the SouthAfricanBank enum so the View can bind to it.
    [ObservableProperty] private ObservableCollection<BankModel> _availableBanks = [];
    [ObservableProperty] private BankModel? _selectedBank;

    // ── Password change fields ────────────────────────────────────────────
    [ObservableProperty] private string _currentPassword = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _confirmPassword = string.Empty;

    // ── State ─────────────────────────────────────────────────────────────

    [ObservableProperty] private bool _isEditMode;
    [ObservableProperty] private string _activeSection = "Details"; // Details | Security

    public event Action? LogoutRequested;

    public UserProfileViewModel(IAppwriteService appwrite, ToastManager toast) : base(toast)
    {
        _appwrite = appwrite;
        LoadBankList();
    }

    // ── Called by the shell after authentication ──────────────────────────
    public void SetUser(AppUser user)
    {
        _currentUser = user;
        PopulateFields(user);
    }

    // ── Bank list helper ──────────────────────────────────────────────────
    private void LoadBankList()
    {
        AvailableBanks = new ObservableCollection<BankModel>(
            Enum.GetValues<SouthAfricanBank>()
                .Select(b =>
                {
                    var info = b.GetBankInfo();
                    return new BankModel
                    {
                        BankName = info.BankName,
                        Abbreviation = info.Abbreviation,
                        NationalBranchCode = info.NationalBranchCode
                    };
                })
                .OrderBy(b => b.BankName)
                .ToList());
    }

    // ── Populate editable fields from AppUser ─────────────────────────────
    private void PopulateFields(AppUser user)
    {
        FullName = user.FullName;
        Email = user.Email;
        Phone = user.Phone;
        IdNumber = user.IdNumber;
        BankAccount = user.BankAccount ?? string.Empty;
        SelectedBank =
            AvailableBanks.FirstOrDefault(b => b.BankName == user.BankName || b.Abbreviation == user.BankName);
    }

    // ── Commands ──────────────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleEditMode()
    {
        if (!IsEditMode) PopulateFields(_currentUser);
        IsEditMode = !IsEditMode;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        PopulateFields(_currentUser);
        IsEditMode = false;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (string.IsNullOrWhiteSpace(FullName))
        {
            ShowError("Full name is required.");
            return;
        }

        await RunSafeAsync(async () =>
        {
            // Build the profile object to upsert in the Profiles collection
            var profile = new UserProfile
            {
                UserId = _currentUser.Id,
                IdNumber = IdNumber.Trim(),
                Role = _currentUser.Role, // Role is not user-editable
                BankAccount = BankAccount.Trim(),
                BankName = SelectedBank?.BankName ?? string.Empty,
                BankBranchCode = SelectedBank?.NationalBranchCode.ToString() ?? string.Empty
            };

            await _appwrite.SaveUserProfileAsync(profile);

            // Sync the in-memory user so the shell headers update immediately
            _currentUser.FullName = FullName.Trim();
            _currentUser.IdNumber = profile.IdNumber;
            _currentUser.BankAccount = profile.BankAccount;
            _currentUser.BankName = profile.BankName;

            IsEditMode = false;
            ShowSuccess("Profile updated successfully.");
        });
    }

    [RelayCommand]
    private void ShowDetails() => ActiveSection = "Details";

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await RunSafeAsync(async () =>
        {
            await _appwrite.LogoutAsync();
            ShowInfo("You have been logged out.");
            LogoutRequested?.Invoke();
        });
    }
    [RelayCommand]
    private void ShowSecurity() => ActiveSection = "Security";

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        // Basic client-side validation before hitting the API
        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            ShowError("Current password is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 8)
        {
            ShowError("New password must be at least 8 characters.");
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            ShowError("New passwords do not match.");
            return;
        }

        await RunSafeAsync(async () =>
        {
            await _appwrite.ChangePasswordAsync(CurrentPassword, NewPassword);
            CurrentPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmPassword = string.Empty;
            ShowSuccess("Password changed successfully.");
        });
    }
}
