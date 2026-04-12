using System;
using System.Runtime.InteropServices.JavaScript;

namespace TheCabinetGroup.Models;

public class Payment
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public double Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? ProofFileId { get; set; }
    public string? Period { get; set; } = DateTime.Now.ToString("MMMM yyyy");
    public string Status { get; set; } = "pending";
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? Notes { get; set; }

    public string StatusDisplay => Status switch
    {
        "pending" => "⏳ Pending",
        "approved" => "✅ Approved",
        "rejected" => "❌ Rejected",
        _ => Status
    };

    public bool HasProof => !string.IsNullOrEmpty(ProofFileId);
    public bool IsPenaltyPayment { get; set; } = false;
}

public class AppUser
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string Role { get; set; } = "member";
    public string? BankAccount { get; set; }
    public string? BankName { get; set; }
    public string? BankBranchCode { get; set; }
    public bool IsAdmin => Role == "admin";
}

public class UserProfile
{
    public string UserId { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string Role { get; set; } = "member";
    public string? BankAccount { get; set; }
    public string? BankName { get; set; }
    public string? BankBranchCode { get; set; }
}

public class Penalty
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public double Amount { get; set; }
    public DateTime PenaltyDate { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }
    public string StatusDisplay => IsPaid ? "✅ Paid" : "❗ Outstanding";
}

public class StokvelSettings
{
    public string Id { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public double MonthlyContribution { get; set; }
    public int ContributionDay { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalMembers { get; set; }
    public double PenaltyAmount { get; set; }
    public int GracePeriodDays { get; set; }

    public DateTime NextDueDate
    {
        get
        {
            var today = DateTime.Today;
            var nextDue = new DateTime(today.Year, today.Month, ContributionDay);
            if (today.Day >= ContributionDay) nextDue = nextDue.AddMonths(1);
            return nextDue;
        }
    }

    public int MonthsRemaining =>
        Math.Max(0, ((EndDate.Year - DateTime.Today.Year) * 12) + EndDate.Month - DateTime.Today.Month);
}

public class DashboardSummary
{
    public double TotalContributed { get; set; }
    public double TotalPenalties { get; set; }
    public double OutstandingPenalties { get; set; }
    public int TotalPayments { get; set; }
    public int PendingPayments { get; set; }
    public DateTime NextDueDate { get; set; }
    public double ProjectedFinalAmount { get; set; }
    public double LastPaymentAmount { get; set; }
    public double AveragePaymentAmount { get; set; }
    public int MonthsRemaining { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public double NetContributed => TotalContributed - TotalPenalties;
}

/// <summary>
/// Strongly-typed representation of the "Appwrite" section in appsettings.json.
/// Sensitive values (ProjectId, DatabaseId, BucketId) are populated at runtime
/// from User Secrets — they should never appear in appsettings.json itself.
/// </summary>
public class AppwriteConfig
{
    public string Endpoint { get; set; } = "https://fra.cloud.appwrite.io/v1";
    public string ProjectId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string DevKey { get; set; } = string.Empty;
    public string DatabaseId { get; set; } = string.Empty;
    public string BucketId { get; set; } = string.Empty;
    public CollectionIds Collections { get; set; } = new();
}

/// <summary>
/// Maps to the "Appwrite:Collections" subsection in appsettings.json.
/// These are the Appwrite Database collection IDs — not sensitive,
/// so they live in appsettings.json and not in User Secrets.
/// </summary>
public class CollectionIds
{
    public string Profiles { get; set; } = "profiles";
    public string Payments { get; set; } = "payments";
    public string Penalties { get; set; } = "penalties";
    public string Settings { get; set; } = "stokvel_settings";
}
