using System;
using System.Reflection;

using TheCabinetGroup.Utils;

namespace TheCabinetGroup.Helpers;

public static class SouthAfricanBankExtensions
{
    /// <summary>
    /// Returns the <see cref="BankInfoAttribute"/> attached to the enum member,
    /// or throws <see cref="InvalidOperationException"/> if none is found.
    /// </summary>
    public static BankInfoAttribute GetBankInfo(this SouthAfricanBank bank)
    {
        var memberInfo = typeof(SouthAfricanBank)
                             .GetField(bank.ToString())
                         ?? throw new ArgumentOutOfRangeException(nameof(bank));

        return memberInfo.GetCustomAttribute<BankInfoAttribute>()
               ?? throw new InvalidOperationException(
                   $"No {nameof(BankInfoAttribute)} defined on {bank}.");
    }

    public static string GetBankName(this SouthAfricanBank bank)
        => bank.GetBankInfo().BankName;

    public static string GetAbbreviation(this SouthAfricanBank bank)
        => bank.GetBankInfo().Abbreviation;

    public static int GetNationalBranchCode(this SouthAfricanBank bank)
        => bank.GetBankInfo().NationalBranchCode;
}
