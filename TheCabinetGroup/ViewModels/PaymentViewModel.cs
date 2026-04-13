using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ShadUI;

using TheCabinetGroup.Models;
using TheCabinetGroup.Services;

namespace TheCabinetGroup.ViewModels;

public partial class PaymentViewModel : ViewModelBase
{
    // ── Dependencies ──────────────────────────────────────────────────────────
    private readonly IAppwriteService _appwrite;

    // ── Current user set from the shell after login ───────────────────────────
    private AppUser _currentUser = new();

    // ── Section switcher (Overview | Submit | History) ────────────────────────
    // CHANGED: Added ActiveSection observable, mirrors UserProfileViewModel pattern.
    [ObservableProperty] private string _activeSection = "Overview";

    // ── Overview tab data ─────────────────────────────────────────────────────
    [ObservableProperty] private double _totalContributed;
    [ObservableProperty] private double _outstandingPenalties;
    [ObservableProperty] private DateTime _nextDueDate = DateTime.Today.AddDays(30);
    [ObservableProperty] private double _monthlyContributionAmount;
    [ObservableProperty] private string _groupName = string.Empty;
    [ObservableProperty] private int _monthsRemaining;

    // CHANGED: Pending payments list bound to the overview card.
    [ObservableProperty] private ObservableCollection<Payment> _pendingPayments = [];

    // ── Submit Payment tab ────────────────────────────────────────────────────
    // CHANGED: Amount is kept as string so the TextBox can bind two-way cleanly.
    [ObservableProperty] private string _paymentAmount = string.Empty;
    [ObservableProperty] private string _paymentNotes = string.Empty;

    // Set after SubmitPaymentCommand succeeds; used to offer proof upload.
    [ObservableProperty] private Payment? _lastSubmittedPayment;

    // Proof upload state
    [ObservableProperty] private string _selectedFileName = string.Empty;
    [ObservableProperty] private bool _hasSelectedFile;
    private Stream? _selectedFileStream;
    private string? _selectedFileNameRaw;

    // ── History tab data ──────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<Payment> _paymentHistory = [];

    // ── File picker provider (injected by the View so the VM stays testable) ─
    // CHANGED: StorageProvider is set by ContributionPage.axaml.cs after init.
    public IStorageProvider? StorageProvider { get; set; }

    // ── Constructors ──────────────────────────────────────────────────────────

    /// <summary>Runtime constructor — called via DI / MainViewModel.</summary>
    public PaymentViewModel(IAppwriteService appwrite, ToastManager toast) : base(toast)
    {
        _appwrite = appwrite;
    }

    /// <summary>Design-time constructor.</summary>
    public PaymentViewModel() : this(null!, null!) { }

    /// <summary>
    /// Called by MainViewModel immediately after the user logs in.
    /// Stores the user reference and kicks off the initial data load.
    /// </summary>
    // CHANGED: Added SetUser to mirror UserProfileViewModel pattern.
    public void SetUser(AppUser user)
    {
        _currentUser = user;
        _ = LoadAsync();
    }

    // ── Section commands ──────────────────────────────────────────────────────

    // CHANGED: Three-section switcher commands added.
    [RelayCommand]
    private void ShowOverview() => ActiveSection = "Overview";

    [RelayCommand]
    private void ShowSubmit()
    {
        // Reset the submit form each time the tab is opened.
        PaymentAmount = string.Empty;
        PaymentNotes = string.Empty;
        LastSubmittedPayment = null;
        ClearFileSelection();
        ActiveSection = "Submit";
    }

    [RelayCommand]
    private void ShowHistory() => ActiveSection = "History";

    // ── Data loading ──────────────────────────────────────────────────────────

    /// <summary>Loads (or reloads) all data for the current member.</summary>
    // CHANGED: LoadAsync added — populates all observable properties from Appwrite.
    [RelayCommand]
    private async Task LoadAsync()
    {
        if (string.IsNullOrEmpty(_currentUser.Id)) return;

        await RunSafeAsync(async () =>
        {
            // Parallel fetch for speed.
            var summaryTask = _appwrite.GetDashboardSummaryAsync(_currentUser.Id);
            var historyTask = _appwrite.GetPaymentHistoryAsync(_currentUser.Id);
            var settingsTask = _appwrite.GetStokvelSettingsAsync();

            await Task.WhenAll(summaryTask, historyTask, settingsTask);

            var summary = summaryTask.Result;
            var history = historyTask.Result;
            var settings = settingsTask.Result;

            // ── Overview ──────────────────────────────────────────────────────
            TotalContributed = summary.TotalContributed;
            OutstandingPenalties = summary.OutstandingPenalties;
            NextDueDate = summary.NextDueDate;
            GroupName = summary.GroupName;
            MonthlyContributionAmount = settings?.MonthlyContribution ?? 0;
            MonthsRemaining = summary.MonthsRemaining;

            // CHANGED: Filter pending payments for the overview card.
            PendingPayments = new ObservableCollection<Payment>(
                history.Where(p => p.Status == "pending")
                    .OrderByDescending(p => p.PaymentDate));

            // ── History ───────────────────────────────────────────────────────
            PaymentHistory = new ObservableCollection<Payment>(
                history.OrderByDescending(p => p.PaymentDate));
        });
    }

    // ── Submit payment ────────────────────────────────────────────────────────

    /// <summary>
    /// Validates the amount field and calls IAppwriteService.SubmitPaymentAsync.
    /// On success the member can optionally upload proof of payment.
    /// </summary>
    // CHANGED: SubmitPaymentCommand added — core of the Contributions page.
    [RelayCommand]
    private async Task SubmitPaymentAsync()
    {
        if (!double.TryParse(PaymentAmount, out var amount) || amount <= 0)
        {
            ShowError("Please enter a valid payment amount.");
            return;
        }

        await RunSafeAsync(async () =>
        {
            // CHANGED: If the user has selected a proof file, upload it first so the
            // fileId can be included when the payment row is created — rather than
            // requiring a separate upload step after submission.
            string? proofFileId = null;
            if (_selectedFileStream is not null && !string.IsNullOrEmpty(_selectedFileNameRaw))
            {
                proofFileId = await _appwrite.UploadFileOnlyAsync(
                    fileStream: _selectedFileStream,
                    userId:  _currentUser.Id,
                    fileName: _selectedFileNameRaw);

                ClearFileSelection();
            }

            var payment = await _appwrite.SubmitPaymentAsync(
                userId: _currentUser.Id,
                amount: amount,
                proofFileId: proofFileId,
                period: null,
                notes: PaymentNotes.Trim(), isPenaltyPayment: false);

            LastSubmittedPayment = payment;

            // Optimistically add to history and pending lists.
            PaymentHistory.Insert(0, payment);
            PendingPayments.Insert(0, payment);

            ShowSuccess($"Payment of R {amount:N2} submitted — awaiting approval.");
        });
    }
    // ── File picker / proof upload ────────────────────────────────────────────

    /// <summary>Opens the OS file picker so the member can select proof of payment.</summary>
    // CHANGED: PickFileCommand added — uses Avalonia IStorageProvider injected from View.
    [RelayCommand]
    private async Task PickFileAsync()
    {
        if (StorageProvider is null)
        {
            ShowError("File picker is not available.");
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Proof of Payment",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Images & PDFs") { Patterns = ["*.jpg", "*.jpeg", "*.png", "*.pdf"] }
            ]
        });

        if (files.Count == 0) return;

        var file = files[0];
        _selectedFileNameRaw = file.Name;
        _selectedFileStream = await file.OpenReadAsync();

        SelectedFileName = file.Name;
        HasSelectedFile = true;
    }

    /// <summary>Uploads the selected file as proof for the last submitted payment.</summary>
    // CHANGED: UploadProofCommand added.
    [RelayCommand]
    private async Task UploadProofAsync()
    {
        if (LastSubmittedPayment is null)
        {
            ShowError("No payment to attach proof to. Please submit a payment first.");
            return;
        }

        if (_selectedFileStream is null || string.IsNullOrEmpty(_selectedFileNameRaw))
        {
            ShowError("Please select a file first.");
            return;
        }

        await RunSafeAsync(async () =>
        {
            await _appwrite.UploadProofOfPaymentAsync(
                paymentId: LastSubmittedPayment.Id,
                userId: _currentUser.Id,
                fileStream: _selectedFileStream,
                fileName: _selectedFileNameRaw);

            ShowSuccess("Proof of payment uploaded successfully.");
            ClearFileSelection();
            LastSubmittedPayment = null;
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // CHANGED: ClearFileSelection helper extracted to avoid duplication.
    private void ClearFileSelection()
    {
        _selectedFileStream?.Dispose();
        _selectedFileStream = null;
        _selectedFileNameRaw = null;
        SelectedFileName = string.Empty;
        HasSelectedFile = false;
    }
}
