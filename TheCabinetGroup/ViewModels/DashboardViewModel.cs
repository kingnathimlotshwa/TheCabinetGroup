using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShadUI;
using TheCabinetGroup.DialogControls.Dialogs;
using TheCabinetGroup.Models;
using TheCabinetGroup.Services;
using DialogHost = DialogHostAvalonia.DialogHost;
namespace TheCabinetGroup.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IAppwriteService _appwrite;
    private readonly ToastManager _toast;
    private Member _currentMember = new();

    [ObservableProperty] private DashboardSummary? _summary;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _errorMessage = string.Empty;

    [ObservableProperty] private ObservableCollection<Payment> _payments = [];
    [ObservableProperty] private string _paymentStatusFilter = "all";
    [ObservableProperty] private double _newPaymentAmount;

    [ObservableProperty] private ObservableCollection<Penalty> _penalties = [];
    [ObservableProperty] private ObservableCollection<Member> _members = [];
    [ObservableProperty] private string _activeTab = "Dashboard";


    // ── MEMBER ─────────────────────────────────────────────────────────
    public string MemberName => _currentMember.FullName;
    public string MemberRole => _currentMember.Role;
    public bool IsAdmin => _currentMember.Role == "admin";

    [ObservableProperty] private string? _selectedPaymentId;
    [ObservableProperty] private string _uploadStatus = string.Empty;

    [ObservableProperty] private DateTime? _historyFrom;
    [ObservableProperty] private DateTime? _historyTo;
    [ObservableProperty] private ObservableCollection<Payment> _paymentHistory = [];

    public event Action? LogoutRequested;

    public DashboardViewModel(IAppwriteService appwrite, ToastManager toast)
    {
        _appwrite = appwrite;
        _toast = toast;
    }

    public void SetMember(Member member)
    {
        _currentMember = member;
        OnPropertyChanged(nameof(MemberName));
        OnPropertyChanged(nameof(MemberRole));
        OnPropertyChanged(nameof(IsAdmin));
    }

    // ── Initialise ─────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task InitialiseAsync()
    {
        await Task.WhenAll(
            LoadDashboardAsync(),
            LoadPaymentsAsync(),
            LoadPenaltiesAsync(),
            LoadMembersAsync());
    }

    // ── Members ─────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadMembersAsync()
    {
        await RunSafeAsync(async () =>
        {
            var list = await _appwrite.GetAllMembersAsync();
            Members = new ObservableCollection<Member>(list);
        });
    }

    [RelayCommand]
    public async Task ViewProofAsync(string fileId)
    {
        var url = await _appwrite.GetProofOfPaymentUrlAsync(fileId);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = url, UseShellExecute = true
        });
    }
    // ── Payments ────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadPaymentsAsync()
    {
        await RunSafeAsync(async () =>
        {
            var list = PaymentStatusFilter == "all"
                ? await _appwrite.GetMyPaymentsAsync(_currentMember.Id)
                : await _appwrite.GetPaymentsByStatusAsync(_currentMember.Id, PaymentStatusFilter);
            Payments = new ObservableCollection<Payment>(list);
        });
    }
    partial void OnPaymentStatusFilterChanged(string value) => _ = LoadPaymentsAsync();

    [RelayCommand]
    public async Task SubmitPaymentAsync()
    {
        if (NewPaymentAmount <= 0) { ErrorMessage = "Enter a valid amount."; return; }
        await RunSafeAsync(async () =>
        {
            var payment = await _appwrite.SubmitPaymentAsync(_currentMember.Id, NewPaymentAmount);
            Payments.Insert(0, payment);
            SelectedPaymentId = payment.Id;
            NewPaymentAmount  = 0;

            _toast.CreateToast("Payment of R " + payment.Amount.ToString("N2") + " submitted.")
                .ShowSuccess();

            await LoadDashboardAsync();
        });
    }

    [RelayCommand]
    public async Task UploadProofAsync(string fileName)
    {
        Stream fs = new FileStream(fileName, FileMode.Open);
        if (string.IsNullOrEmpty(SelectedPaymentId)) { ErrorMessage = "Select a payment first."; return; }
        await RunSafeAsync(async () =>
        {
            await _appwrite.UploadProofOfPaymentAsync(
                SelectedPaymentId!, _currentMember.Id, fs, fileName);

            _toast.CreateToast("Proof of payment uploaded successfully.")
                .ShowSuccess();

            await LoadPaymentsAsync();
        });
    }
    // ── Penalties ───────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadPenaltiesAsync()
    {
        await RunSafeAsync(async () =>
        {
            var list = await _appwrite.GetMyPenaltiesAsync(_currentMember.Id);
            Penalties = new ObservableCollection<Penalty>(list);
        });
    }

    // ── Dashboard ───────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadDashboardAsync()
    {
        await RunSafeAsync(async () => { Summary = await _appwrite.GetDashboardSummaryAsync(_currentMember.Id); });
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        await RunSafeAsync(async () =>
        {
            await _appwrite.LogoutAsync();
            _toast.CreateToast("You have been logged out.")
                .ShowInfo();
            LogoutRequested?.Invoke();
        });
    }

    // ── History ─────────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task LoadPaymentHistoryAsync()
    {
        await RunSafeAsync(async () =>
        {
            var list = await _appwrite.GetPaymentHistoryAsync(_currentMember.Id, HistoryFrom, HistoryTo);
            PaymentHistory = new ObservableCollection<Payment>(list);
        });
    }


    // ── Helpers ─────────────────────────────────────────────────────────────

    private async Task RunSafeAsync(Func<Task> action)
    {
        ErrorMessage = string.Empty;
        IsBusy = true;
        try
        {
            await action();
        }
        catch (Appwrite.AppwriteException ex)
        {
            ErrorMessage = ex.Message ?? "An Appwrite error occurred.";
            _toast.CreateToast("Appwrite")
                  .WithContent(ErrorMessage)
                  .ShowError();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toast.CreateToast("Exception")
                  .WithContent(ErrorMessage)
                  .ShowError();
        }
        finally
        {
            IsBusy = false;
        }
    }
}
