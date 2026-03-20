using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Appwrite;

using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ShadUI;

using TheCabinetGroup.Helpers;
using TheCabinetGroup.Models;
using TheCabinetGroup.Services;
using TheCabinetGroup.Utils;
using TheCabinetGroup.ViewModels;

namespace TheCabinetGroup.DialogControls.DialogModels;

public partial class AuthViewModel : ViewModelBase
{
    private readonly IAppwriteService _appwrite;
    private readonly ILoginCacheService _cache;
    private readonly ToastManager _toast;

    // Events consumed by MainWindow to swap views
    public event Action<AppUser>? LoginSucceeded;

    // Logo Color
    [ObservableProperty] private string _logoColor = ".Black { fill: #54A9FF; }";

    // ── Shared ──
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _currentView = "Login";

    // ── Login ──
    [ObservableProperty] private string _loginEmail = string.Empty;
    [ObservableProperty] private string _loginPhone = string.Empty;
    [ObservableProperty] private string _loginPassword = string.Empty;
    [ObservableProperty] private bool _usePhoneLogin;
    [ObservableProperty] private string _phoneOtp = string.Empty;
    [ObservableProperty] private bool _otpSent;
    [ObservableProperty] private bool _rememberMe;
    private string? _otpUserId;

    // ── Register ──
    [ObservableProperty] private string _regFullName = string.Empty;
    [ObservableProperty] private string _regEmail = string.Empty;
    [ObservableProperty] private string _regPhone = string.Empty;
    [ObservableProperty] private string _regIdNumber = string.Empty;
    [ObservableProperty] private string _regPassword = string.Empty;
    [ObservableProperty] private string _regConfirmPassword = string.Empty;
    [ObservableProperty] private string _regBankAccount = string.Empty;
    [ObservableProperty] private BankModel _regBank = new();
    [ObservableProperty] private List<BankModel> _regBanks = [];

    // ── Forgot Password ──
    [ObservableProperty] private string _forgotEmail = string.Empty;
    [ObservableProperty] private bool _recoveryEmailSent;

    // ── Change Password ──
    [ObservableProperty] private string _oldPassword = string.Empty;
    [ObservableProperty] private string _newPassword = string.Empty;
    [ObservableProperty] private string _confirmNewPassword = string.Empty;


    public AuthViewModel(
        IAppwriteService appwriteService,
        ILoginCacheService loginCacheService,
        ToastManager toastManager)
    {
        _appwrite = appwriteService;
        _cache = loginCacheService;
        _toast = toastManager;
        ChangeLogoColor();
    }

    // Logo Color Helper
    private void ChangeLogoColor()
    {
        App.Current.Styles.TryGetResource("ForegroundColor", App.Current.ActualThemeVariant, out var colorPrimary);
        LogoColor = colorPrimary is Color color
            ? $".Black {{ fill: {color.ToString().Replace("#ff", "#")}; }}"
            : ".Black { fill: #54A9FF; }";
    }

    // Design-time constructor

    public AuthViewModel() : this(null!, null!, null!)
    {
    }

    // ── Login ──────────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task LoginWithEmailAsync()
    {
        if (string.IsNullOrWhiteSpace(LoginEmail) || string.IsNullOrWhiteSpace(LoginPassword))
        {
            ShowError("Please enter email and password.");
            return;
        }

        await RunSafeAsync(async () =>
        {
            await _appwrite.LoginWithEmailAsync(LoginEmail, LoginPassword);
            var member = await _appwrite.GetCurrentUserAsync()
                         ?? throw new Exception("Profile not found. Contact your admin.");
            PersistSessionIfRequired(LoginEmail, LoginPassword);

            _toast.CreateToast($"Welcome back, {member.FullName}!")
                  .WithDelay(2)
                  .ShowSuccess();
            LoginSucceeded?.Invoke(member);
        });
    }

    [RelayCommand]
    private async Task RequestPhoneOtpAsync()
    {
        if (string.IsNullOrWhiteSpace(LoginPhone))
        {
            ErrorMessage = "Enter your phone number (+27XXXXXXXXX).";
            _toast.CreateToast($"{ErrorMessage}")
                  .WithDelay(2)
                  .ShowError();
            return;
        }

        await RunSafeAsync(async () =>
        {
            _otpUserId = await _appwrite.RequestPhoneOtpAsync(LoginPhone);
            OtpSent = true;
            _toast.CreateToast("OTP sent to " + LoginPhone)
                  .ShowInfo();
        });
    }

    [RelayCommand]
    private async Task ConfirmPhoneOtpAsync()
    {
        if (string.IsNullOrWhiteSpace(PhoneOtp))
        {
            ErrorMessage = "Enter the OTP sent to your phone.";
            _toast.CreateToast($"{ErrorMessage}")
                  .WithDelay(2)
                  .ShowError();
            return;
        }

        await RunSafeAsync(async () =>
        {
            await _appwrite.ConfirmPhoneOtpAsync(_otpUserId!, PhoneOtp);
            var user = await _appwrite.GetCurrentUserAsync()
                       ?? throw new Exception("Profile not found.");
            //OTP login has no email/password to cache — remember me not supported for OTP
            _toast.CreateToast("Welcome back, " + user.FullName + "!")
                  .ShowSuccess();
            LoginSucceeded?.Invoke(user);
        });
    }


    // ── Register ───────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(RegFullName))
        {
            ShowError("Full name is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(RegEmail))
        {
            ShowError("Email is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(RegIdNumber))
        {
            ShowError("ID number is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(RegPassword))
        {
            ShowError("Password is required.");
            return;
        }

        if (RegPassword != RegConfirmPassword)
        {
            ShowError("Passwords do not match.");
            return;
        }

        if (RegPassword.Length < 8)
        {
            ShowError("Password must be 8+ characters.");
            return;
        }

        await RunSafeAsync(async () =>
        {
            var userId = await _appwrite.RegisterAsync(RegEmail, RegPassword, RegFullName);
            await _appwrite.LoginWithEmailAsync(RegEmail, RegPassword);
            await _appwrite.SaveUserProfileAsync(new UserProfile
                                                 {
                                                     UserId = _appwrite.CurrentUserId!,
                                                     IdNumber = RegIdNumber,
                                                     Role = "member",
                                                     BankAccount = RegBankAccount,
                                                     BankName = $"{RegBank.BankName}",
                                                     BankBranchCode = $"{RegBank.NationalBranchCode}"
                                                 });

            await _appwrite.SendEmailVerificationAsync("https://yourstokvelapp.com/verify");

            var user = await _appwrite.GetCurrentUserAsync();
            _toast.CreateToast($"Welcome, {user.FullName}! Check your email to verify.")
                  .ShowSuccess();
            LoginSucceeded?.Invoke(user);
        });
    }

    // ── Forgot / Change Password ───────────────────────────────────────────

    [RelayCommand]
    private async Task SendRecoveryEmailAsync()
    {
        if (string.IsNullOrWhiteSpace(ForgotEmail))
        {
            ShowError("Enter your email address.");
            return;
        }

        await RunSafeAsync(async () =>
        {
            await _appwrite.ForgotPasswordAsync(ForgotEmail, "https://localhost/reset-password");
            RecoveryEmailSent = true;
            _toast.CreateToast("Recovery email sent to " + ForgotEmail).WithDelay(3)
                  .ShowInfo();
        });
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        if (NewPassword != ConfirmNewPassword)
        {
            ShowError("New passwords do not match.");
            return;
        }

        await RunSafeAsync(async () =>
        {
            await _appwrite.ChangePasswordAsync(OldPassword, NewPassword);
            OldPassword = string.Empty;
            NewPassword = string.Empty;
            ConfirmNewPassword = string.Empty;
            _toast.CreateToast("Password changed successfully!").WithDelay(3)
                  .ShowSuccess();
        });
    }

    // ── Navigation ─────────────────────────────────────────────────────────

    [RelayCommand]
    private void ShowLogin()
    {
        ErrorMessage = string.Empty;
        CurrentView = "Login";
    }

    [RelayCommand]
    private void ShowRegister()
    {
        ErrorMessage = string.Empty;
        RegFullName = string.Empty;
        RegEmail = string.Empty;
        RegPhone = string.Empty;
        RegIdNumber = string.Empty;
        RegPassword = string.Empty;
        RegConfirmPassword = string.Empty;
        RegBankAccount = string.Empty;
        RegBanks = Enum.GetValues<SouthAfricanBank>().Select(bank => new BankModel
                                                                     {
                                                                         Abbreviation = bank.GetAbbreviation(),
                                                                         BankName = bank.GetBankName(),
                                                                         NationalBranchCode =
                                                                             bank.GetNationalBranchCode()
                                                                     })
                       .ToList();
        RegBank = RegBanks[0];
        CurrentView = "Register";
    }

    [RelayCommand]
    private void ShowForgotPassword()
    {
        ErrorMessage = string.Empty;
        RecoveryEmailSent = false;
        ForgotEmail = string.Empty;
        CurrentView = "ForgotPassword";
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void ShowError(string message)
    {
        ErrorMessage = message;
        _toast.CreateToast(message).WithDelay(2).ShowError();
    }

    /// <summary>
    /// Writes the current session to disk when RememberMe is ticked.
    /// The session remains valid for <see cref="LoginCacheService.CacheDuration"/>.
    /// </summary>
    private void PersistSessionIfRequired(string email, string password)
    {
        if (!RememberMe) return;
        _cache.Save(new SessionCache
                    {
                        Email = email,
                        Password = password,
                        ExpiresAt = DateTime.UtcNow.Add(LoginCacheService.CacheDuration),
                        RememberMe = true
                    });
    }

    private async Task RunSafeAsync(Func<Task> action)
    {
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            await action();
        }
        catch (AppwriteException ex)
        {
            ShowError(ex.Message ?? "An Appwrite error occurred.");
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
