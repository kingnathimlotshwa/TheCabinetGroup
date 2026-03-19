using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TheCabinetGroup.Models;

namespace TheCabinetGroup.Services;

public interface IAppwriteService
{
    string? CurrentUserId { get; }

    /// <summary>
    /// The raw session secret (JWT) obtained after the last successful login.
    /// Stored here so the cache layer can persist it without exposing Appwrite
    /// SDK types outside of <see cref="AppwriteService"/>.
    /// </summary>
    string? CurrentSessionSecret { get; }
    
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
    /// Re-hydrates an Appwrite client session from a previously-cached secret
    /// and verifies it is still active on the server.
    /// Returns the <see cref="Member"/> profile on success, or null if the
    /// session has expired / is otherwise invalid.
    /// </summary>
    Task<Member?> TryRestoreSessionAsync(string userId, string sessionSecret);

    // Members
    Task<Member> CreateMemberProfileAsync(Member member);
    Task<Member?> GetMemberByUserIdAsync(string userId);
    Task<Member> GetMemberByIdAsync(string memberId);
    Task<List<Member>> GetAllMembersAsync();
    Task UpdateMemberAsync(Member member);

    // Contributions
    Task<List<Contribution>> GetMyContributionsAsync(string memberId);
    Task<double> GetTotalContributedAsync(string memberId);

    // Payments
    Task<Payment> SubmitPaymentAsync(string memberId, double amount,
        string? contributionId = null, string? notes = null);
    Task<string> UploadProofOfPaymentAsync(string paymentId, string memberId,
        Stream fileStream, string fileName);
    Task<string> GetProofOfPaymentUrlAsync(string fileId);
    Task<List<Payment>> GetMyPaymentsAsync(string memberId);
    Task<List<Payment>> GetPaymentsByStatusAsync(string memberId, string status);

    // Penalties
    Task<List<Penalty>> GetMyPenaltiesAsync(string memberId);
    Task<double> GetOutstandingPenaltiesAsync(string memberId);

    // Settings
    Task<StokvelSettings?> GetStokvelSettingsAsync();

    // Dashboard
    Task<DashboardSummary> GetDashboardSummaryAsync(string memberId);

    // History
    Task<List<Payment>> GetPaymentHistoryAsync(string memberId,
        DateTime? from = null, DateTime? to = null);
}