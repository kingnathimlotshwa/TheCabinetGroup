using System;

namespace TheCabinetGroup.Helpers;

/// <summary>
/// Decorates a <see cref="SouthAfricanBank"/> enum member with its
/// display name, abbreviation and SARB universal branch code.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public class BankInfoAttribute : Attribute
{
    /// <summary>Full registered bank name.</summary>
    public string BankName { get; }

    /// <summary>Common abbreviation / short name.</summary>
    public string Abbreviation { get; }

    /// <summary>SARB universal (national) branch code.</summary>
    public int NationalBranchCode { get; }

    public BankInfoAttribute(string bankName, string abbreviation, int nationalBranchCode)
    {
        BankName = bankName;
        Abbreviation = abbreviation;
        NationalBranchCode = nationalBranchCode;
    }
}
