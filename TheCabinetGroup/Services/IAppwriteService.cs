using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TheCabinetGroup.Models;

namespace TheCabinetGroup.Services;

public interface IAppwriteService
{
    string? CurrentUserId { get; }
    string? CurrentSessionId { get; }

    // Auth
    Task<string> RegisterAsync(string email, string password, string name);
    Task LoginWithEmailAsync(string email, string password);
    Task<string> RequestPhoneOtpAsync(string phone);
    Task ConfirmPhoneOtpAsync(string userId, string secret);
    Task ForgotPasswordAsync(string email, string redirectUrl);
    Task ResetPasswordAsync(string userId, string secret, string password, string confirmPassword);
    Task ChangePasswordAsync(string oldPassword, string newPassword);
    Task SendEmailVerificationAsync(string redirectUrl);
    Task ConfirmEmailVerificationAsync(string userId, string secret);
    Task SendPhoneVerificationAsync();
    Task ConfirmPhoneVerificationAsync(string userId, string secret);
    Task LogoutAsync();

    /// <summary>
    /// Re-hydrates a session from cache and verifies it is still valid.
    /// Returns the <see cref="AppUser"/> on success, null if expired.
    /// </summary>
    Task<AppUser?> TryRestoreSessionAsync(string email, string password);

    // ── Current user ──────────────────────────────────────────────────────────
    /// <summary>
    /// Returns Auth details + profile fields merged into one <see cref="AppUser"/>.
    /// </summary>
    Task<AppUser> GetCurrentUserAsync();

    /// <summary>
    /// Saves extra profile fields to the profiles collection.
    /// Creates a new document or updates existing one (upsert).
    /// </summary>
    Task SaveUserProfileAsync(UserProfile profile);
    // Contributions
    Task<List<Contribution>> GetMyContributionsAsync(string memberId);
    Task<double> GetTotalContributedAsync(string memberId);

    // ── Payments ──────────────────────────────────────────────────────────────
    Task<Payment> SubmitPaymentAsync(string userId, double amount,
                                     string? contributionId = null, string? notes = null);
    Task<string> UploadProofOfPaymentAsync(string paymentId, string userId,
                                           Stream fileStream, string fileName);
    Task<string> GetProofOfPaymentUrlAsync(string fileId);
    Task<List<Payment>> GetMyPaymentsAsync(string userId);
    Task<List<Payment>> GetPaymentsByStatusAsync(string userId, string status);

    // ── Penalties ─────────────────────────────────────────────────────────────
    Task<List<Penalty>> GetMyPenaltiesAsync(string userId);
    Task<double> GetOutstandingPenaltiesAsync(string userId);

    // ── Settings ──────────────────────────────────────────────────────────────
    Task<StokvelSettings?> GetStokvelSettingsAsync();

    // ── Dashboard ─────────────────────────────────────────────────────────────
    Task<DashboardSummary> GetDashboardSummaryAsync(string userId);

    // ── History ───────────────────────────────────────────────────────────────
    Task<List<Payment>> GetPaymentHistoryAsync(string userId,
                                               DateTime? from = null, DateTime? to = null);
}
