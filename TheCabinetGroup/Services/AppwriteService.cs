using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Appwrite;
using Appwrite.Extensions;
using Appwrite.Models;
using Appwrite.Services;
using Newtonsoft.Json;
using TheCabinetGroup.Models;
using File = Appwrite.Models.File;

namespace TheCabinetGroup.Services;

public class AppwriteService : IAppwriteService
{
    // ── SDK clients ──────────────────────────────────────────────────────────
    private readonly Client _client;
    private readonly Account _account;
    private readonly TablesDB _databases;
    private readonly Storage _storage;

    // ── Config ───────────────────────────────────────────────────────────────
    private readonly AppwriteConfig _config;
    public string? CurrentUserId { get; private set; }
    public string? CurrentSessionSecret { get; private set; }

    // ── Json Settings ───────────────────────────────────────────────────────────────
    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        DateParseHandling = DateParseHandling.DateTime,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc
    };

    public AppwriteService(AppwriteConfig config)
    {
        _config = config;
        _client = new Client()
                  .SetEndpoint(_config.Endpoint)
                  .SetProject(_config.ProjectId);
            // .SetKey(_config.ApiKey)
            // .SetSelfSigned(true); // set true only for self-hosted with no valid cert

        _account = new Account(_client);
        _databases = new TablesDB(_client);
        _storage = new Storage(_client);
    }

    // ── Deserializer ──────────────────────────────────────────────────────────
    // Serialize Document.Data (Dictionary<string,object>) → JSON → domain model.
    // Document.$id is always set separately since it lives outside Data.

    private static T FromRow<T>(Row doc) where T : class
    {
        var json = JsonConvert.SerializeObject(doc.Data, _jsonSettings);
        var model = JsonConvert.DeserializeObject<T>(json, _jsonSettings)
                    ?? throw new InvalidOperationException($"Failed to deserialize {typeof(T).Name}.");

        // Assign $id via reflection so the caller doesn't need to do it
        var idProp = typeof(T).GetProperty("Id");
        idProp?.SetValue(model, doc.Id);

        return model;
    }

    private static List<T> FromRowList<T>(RowList docs) where T : class =>
        docs.Rows.Select(doc => FromRow<T>(doc)).ToList();

    // ───────────────── AUTHENTICATION ─────────────────

    public async Task<string> RegisterAsync(string email, string password, string name)
    {
        var user = await _account.Create(
            userId: ID.Unique(),
            email: email,
            password: password,
            name: name
        );

        return user.Id;
    }

    public async Task LoginWithEmailAsync(string email, string password)
    {
        var session = await _account.CreateEmailPasswordSession(email, password);
        CurrentUserId = session.UserId;
        CurrentSessionSecret = session.Secret;
        _client.SetSession(session.Secret);
    }

    public async Task<string> RequestPhoneOtpAsync(string phone)
    {
        var token = await _account.CreatePhoneToken(
            userId: ID.Unique(),
            phone: phone);
        return token.UserId;
    }

    public async Task ConfirmPhoneOtpAsync(string userId, string secret)
    {
        var session = await _account.CreateSession(userId: userId, secret: secret);
        CurrentUserId = session.UserId;
        CurrentSessionSecret = session.Secret;
        _client.SetSession(session.Secret);
    }

    public async Task ForgotPasswordAsync(string email, string redirectUrl)
        => await _account.CreateRecovery(email: email, url: redirectUrl);

    public async Task ResetPasswordAsync(string userId, string secret, string password, string confirmPassword)
        => await _account.UpdateRecovery(userId: userId, secret: secret, password: password);

    public async Task ChangePasswordAsync(string oldPassword, string newPassword)
        => await _account.UpdatePassword(password: newPassword, oldPassword: oldPassword);

    public async Task SendEmailVerificationAsync(string redirectUrl)
        => await _account.CreateEmailVerification(url: redirectUrl);

    public async Task ConfirmEmailVerificationAsync(string userId, string secret)
        => await _account.UpdateEmailVerification(userId: userId, secret: secret);

    public async Task SendPhoneVerificationAsync()
        => await _account.CreatePhoneVerification();

    public async Task ConfirmPhoneVerificationAsync(string userId, string secret)
        => await _account.UpdatePhoneVerification(userId, secret: secret);

    public async Task LogoutAsync()
    {
        await _account.DeleteSession(sessionId: "current");
        CurrentUserId = null;
        CurrentSessionSecret = null;
        _client.SetSession(string.Empty);
    }


    /// <inheritdoc />
    public async Task<Member?> TryRestoreSessionAsync(string userId, string sessionSecret)
    {
        try
        {
            _client.SetSession(sessionSecret);
            CurrentUserId = userId;
            CurrentSessionSecret = sessionSecret;

            // Verify the session is still alive on the Appwrite server.
            // _account.Get() throws AppwriteException (401) if expired.
            await _account.Get();

            return await GetMemberByUserIdAsync(userId);
        }
        catch
        {
            // Session is expired or network error – reset state cleanly.
            CurrentUserId = null;
            CurrentSessionSecret = null;
            _client.SetSession(string.Empty);
            return null;
        }
    }

    // ───────────────── MEMBER PROFILE ─────────────────

    public async Task<Member> CreateMemberProfileAsync(Member member)
    {
        var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                       JsonConvert.SerializeObject(member, _jsonSettings), _jsonSettings)
                   ?? throw new InvalidOperationException("Failed to serialize member.");

        var doc = await _databases.CreateRow(
            databaseId: _config.DatabaseId,
            tableId: _config.Collections.Members,
            rowId: ID.Unique(),
            data: data,
            permissions: new List<string>
            {
                Permission.Read(Role.User(member.UserId)),
                Permission.Update(Role.User(member.UserId)),
                Permission.Read(Role.Team("admins")),
                Permission.Update(Role.Team("admins"))
            });

        return FromRow<Member>(doc);
    }

    public async Task<Member?> GetMemberByUserIdAsync(string userId)
    {
        var result = await _databases.ListRows(
            databaseId: _config.DatabaseId,
            tableId: _config.Collections.Members,
            queries: [Query.Equal("userId", userId)]);

        return result.Rows.Count > 0 ? FromRow<Member>(result.Rows[0]) : null;
    }

    public async Task<Member> GetMemberByIdAsync(string memberId)
    {
        var doc = await _databases.GetRow(
            databaseId: _config.DatabaseId,
            tableId: _config.Collections.Members,
            rowId: memberId);

        return FromRow<Member>(doc);
    }

    public async Task<List<Member>> GetAllMembersAsync()
    {
        var result = await _databases.ListRows(
            databaseId: _config.DatabaseId,
            tableId: _config.Collections.Members,
            queries: [Query.OrderAsc("fullName")]);

        return FromRowList<Member>(result);
    }

    public async Task UpdateMemberAsync(Member member)
    {
        var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                       JsonConvert.SerializeObject(member, _jsonSettings), _jsonSettings)
                   ?? throw new InvalidOperationException("Failed to serialize member.");

        await _databases.UpdateRow(
            databaseId: _config.DatabaseId,
            tableId: _config.Collections.Members,
            rowId: member.Id,
            data: data);
    }

    // ───────────────── CONTRIBUTIONS ─────────────────

    public async Task<List<Contribution>> GetMyContributionsAsync(string memberId)
    {
        var result = await _databases.ListRows(
            databaseId: _config.DatabaseId,
            tableId: _config.Collections.Contributions,
            queries: [Query.Equal("memberId", memberId), Query.OrderDesc("dueDate")]);

        return FromRowList<Contribution>(result);
    }

    public async Task<double> GetTotalContributedAsync(string memberId)
    {
        var contributions = await GetMyContributionsAsync(memberId);
        return contributions.Where(c => c.Status == "approved").Sum(c => c.Amount);
    }

    // ───────────────── PAYMENTS ─────────────────

    public async Task<Payment> SubmitPaymentAsync(
        string memberId,
        double amount,
        string? contributionId = null,
        string? notes = null)
    {
        var doc = await _databases.CreateRow(
            databaseId: _config.DatabaseId,
            tableId: _config.Collections.Payments,
            rowId: ID.Unique(),
            data: new Dictionary<string, object>
            {
                ["memberId"] = memberId,
                ["contributionId"] = contributionId ?? string.Empty,
                ["amount"] = amount,
                ["paymentDate"] = DateTime.UtcNow.ToString("o"),
                ["status"] = "pending",
                ["notes"] = notes ?? string.Empty
            },
            permissions: new List<string>
            {
                Permission.Read(Role.User(CurrentUserId!)),
                Permission.Read(Role.Team("admins")),
                Permission.Update(Role.Team("admins"))
            });

        return FromRow<Payment>(doc);
    }

    public async Task<string> UploadProofOfPaymentAsync(string paymentId, string memberId,
        Stream fileStream, string fileName)
    {
        var file = await _storage.CreateFile(
            bucketId:    _config.BucketId,
            fileId:      ID.Unique(),
            file:        InputFile.FromStream(fileStream, fileName, fileName.GetMimeType()),
            permissions: new List<string>
            {
                Permission.Read(Role.User(CurrentUserId!)),
                Permission.Read(Role.Team("admins"))
            });

        await _databases.UpdateRow(
            databaseId: _config.DatabaseId,
            tableId:    _config.Collections.Payments,
            rowId:      paymentId,
            data:       new Dictionary<string, object> { ["proofFileId"] = file.Id });

        return file.Id;
    }

    public Task<string> GetProofOfPaymentUrlAsync(string fileId)
    {
        var url = $"{_config.Endpoint}/storage/buckets/{_config.BucketId}" +
                  $"/files/{fileId}/view?project={_config.ProjectId}";
        return Task.FromResult(url);
    }

    public async Task<List<Payment>> GetMyPaymentsAsync(string memberId)
    {
        var result = await _databases.ListRows(
            databaseId: _config.DatabaseId,
            tableId: _config.Collections.Payments,
            queries: [Query.Equal("memberId", memberId), Query.OrderDesc("paymentDate")]);

        return FromRowList<Payment>(result);
    }

    public async Task<List<Payment>> GetPaymentsByStatusAsync(string memberId, string status)
    {
        var result = await _databases.ListRows(
            databaseId: _config.DatabaseId,
            tableId: _config.Collections.Payments,
            queries: [Query.Equal("memberId", memberId), Query.Equal("status", status)]);

        return FromRowList<Payment>(result);
    }

    // ───────────────── PENALTIES ─────────────────

    public async Task<List<Penalty>> GetMyPenaltiesAsync(string memberId)
    {
        var result = await _databases.ListRows(
            databaseId: _config.DatabaseId,
            tableId: _config.Collections.Penalties,
            queries: [Query.Equal("memberId", memberId), Query.OrderDesc("penaltyDate")]);

        return FromRowList<Penalty>(result);
    }

    public async Task<double> GetOutstandingPenaltiesAsync(string memberId)
    {
        var penalties = await GetMyPenaltiesAsync(memberId);
        return penalties.Where(p => !p.IsPaid).Sum(p => p.Amount);
    }

    // ───────────────── SETTINGS ─────────────────

    public async Task<StokvelSettings?> GetStokvelSettingsAsync()
    {
        var result = await _databases.ListRows(
            databaseId: _config.DatabaseId,
            tableId: _config.Collections.Settings,
            queries: [Query.Limit(1)]);

        return result.Rows.Count > 0 ? FromRow<StokvelSettings>(result.Rows[0]) : null;
    }

    // ───────────────── DASHBOARD SUMMARY ─────────────────

    public async Task<DashboardSummary> GetDashboardSummaryAsync(string memberId)
    {
        var paymentsTask  = GetMyPaymentsAsync(memberId);
        var penaltiesTask = GetMyPenaltiesAsync(memberId);
        var settingsTask  = GetStokvelSettingsAsync();

        await Task.WhenAll(paymentsTask, penaltiesTask, settingsTask);

        var payments  = paymentsTask.Result;
        var penalties = penaltiesTask.Result;
        var settings  = settingsTask.Result;

        var approved        = payments.Where(p => p.Status == "approved").ToList();
        var totalContrib    = approved.Sum(p => p.Amount);
        var totalPenalties  = penalties.Sum(p => p.Amount);
        var outstanding     = penalties.Where(p => !p.IsPaid).Sum(p => p.Amount);
        var lastAmount      = approved.OrderByDescending(p => p.PaymentDate).FirstOrDefault()?.Amount ?? 0;
        var avgAmount       = approved.Count > 0 ? approved.Average(p => p.Amount) : 0;
        var monthsRemaining = settings?.MonthsRemaining ?? 0;

        return new DashboardSummary
        {
            TotalContributed     = totalContrib,
            TotalPenalties       = totalPenalties,
            OutstandingPenalties = outstanding,
            TotalPayments        = payments.Count,
            PendingPayments      = payments.Count(p => p.Status == "pending"),
            NextDueDate          = settings?.NextDueDate ?? DateTime.Today.AddDays(30),
            ProjectedFinalAmount = totalContrib + (avgAmount * monthsRemaining),
            LastPaymentAmount    = lastAmount,
            AveragePaymentAmount = avgAmount,
            MonthsRemaining      = monthsRemaining,
            GroupName            = settings?.GroupName ?? "Stokvel"
        };
    }

    // ───────────────── PAYMENT HISTORY ─────────────────
    public async Task<List<Payment>> GetPaymentHistoryAsync(string memberId,
        DateTime? from = null, DateTime? to = null)
    {
        var queries = new List<string>
        {
            Query.Equal("memberId", memberId),
            Query.OrderDesc("paymentDate")
        };

        if (from.HasValue)
            queries.Add(Query.GreaterThanEqual("paymentDate", from.Value.ToString("o")));
        if (to.HasValue)
            queries.Add(Query.LessThanEqual("paymentDate", to.Value.ToString("o")));

        var result = await _databases.ListRows(
            databaseId: _config.DatabaseId,
            tableId:    _config.Collections.Payments,
            queries:    queries);

        return FromRowList<Payment>(result);
    }
}
