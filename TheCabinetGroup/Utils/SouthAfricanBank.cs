using TheCabinetGroup.Helpers;

namespace TheCabinetGroup.Utils;

public enum SouthAfricanBank
{
    // ── Major / retail banks ─────────────────────────────────────────────────

    [BankInfo("ABSA Bank Limited", "ABSA", 632005)]
    Absa = 1,

    [BankInfo("Capitec Bank Limited", "Capitec", 470010)]
    Capitec = 2,

    [BankInfo("First National Bank", "FNB", 250655)]
    Fnb = 3,

    [BankInfo("Nedbank Limited", "Nedbank", 198765)]
    Nedbank = 4,

    [BankInfo("Standard Bank of South Africa", "STD", 051001)]
    StandardBank = 5,

    [BankInfo("Investec Bank Limited", "Investec", 580105)]
    Investec = 6,

    // ── Digital / challenger banks ───────────────────────────────────────────

    [BankInfo("Discovery Bank Limited", "Discovery", 679000)]
    DiscoveryBank = 7,

    [BankInfo("TymeBank Limited", "Tyme", 678910)]
    TymeBank = 8,

    [BankInfo("Bank Zero Mutual Bank", "BankZero", 888000)]
    BankZero = 9,

    [BankInfo("African Bank Limited", "AFB", 430000)]
    AfricanBank = 10,

    [BankInfo("Bidvest Bank Limited", "Bidvest", 462005)]
    BidvestBank = 11,

    // ── Specialist / niche banks ─────────────────────────────────────────────

    [BankInfo("Grindrod Bank Limited", "Grindrod", 584000)]
    GrindrodBank = 12,

    [BankInfo("Mercantile Bank Limited", "Mercantile", 450105)]
    MercantileBank = 13,

    [BankInfo("Sasfin Bank Limited", "Sasfin", 683000)]
    SasfinBank = 14,

    [BankInfo("South African Postbank SOC Limited", "PostBank", 460005)]
    SouthAfricanPostBank = 15,

    [BankInfo("Ubank Limited", "Ubank", 431010)]
    Ubank = 16
}
