namespace Snackbox.Api.Dtos;

/// <summary>
/// Connection to the old Snackbox SQL Server database. Matches the fields of its option.ini.
/// </summary>
public class LegacyConnectionDto
{
    public string Server { get; set; } = "localhost";
    public string Database { get; set; } = "Snackboxx";

    /// <summary>SQL Server login. Leave empty to use the API process' Windows account.</summary>
    public string? Username { get; set; }

    public string? Password { get; set; }

    public bool TrustServerCertificate { get; set; } = true;
}

/// <summary>Row counts of the old database, to confirm the connection points at the right one.</summary>
public class LegacyConnectionTestDto
{
    public bool Connected { get; set; }
    public string? Error { get; set; }
    public int Users { get; set; }
    public int Codes { get; set; }
    public int Scans { get; set; }
    public int Payments { get; set; }
    public DateTime? FirstScan { get; set; }
    public DateTime? LastScan { get; set; }
}

public class LegacyImportRequestDto
{
    public LegacyConnectionDto Connection { get; set; } = new();

    /// <summary>
    /// Only import scans and payments at or after this point. Leave empty for everything.
    /// Useful once the initial import is done and only the parallel-run period matters.
    /// </summary>
    public DateTime? From { get; set; }

    /// <summary>
    /// Add a correcting payment per user so the new balance matches the old T_User.rest.
    /// The old database does not reconcile exactly (its history was trimmed over the years),
    /// so without this the imported users start with a wrong balance.
    /// </summary>
    public bool AnchorBalances { get; set; } = true;

    /// <summary>Report what would happen without writing anything.</summary>
    public bool DryRun { get; set; }
}

public class LegacyImportResultDto
{
    public bool DryRun { get; set; }
    public int UsersCreated { get; set; }
    public int UsersMatched { get; set; }
    public int BarcodesCreated { get; set; }
    public int PurchasesCreated { get; set; }
    public int ScansImported { get; set; }
    public int ScansSkippedAlreadyPresent { get; set; }
    public int PaymentsImported { get; set; }
    public int PaymentsSkippedAlreadyPresent { get; set; }
    public int BalanceAnchorsWritten { get; set; }
    public decimal BalanceAnchorTotal { get; set; }
    public long DurationMs { get; set; }
    public List<string> Warnings { get; set; } = new();
}

/// <summary>One user as seen from both sides. <c>Matches</c> is false for anything worth a look.</summary>
public class LegacyUserComparisonDto
{
    public int LegacyUserId { get; set; }
    public string LegacyName { get; set; } = "";
    public int? UserId { get; set; }
    public string? Username { get; set; }

    public int OldScanCount { get; set; }
    public int NewScanCount { get; set; }
    public decimal OldSpent { get; set; }
    public decimal NewSpent { get; set; }
    public decimal OldPaid { get; set; }
    public decimal NewPaid { get; set; }

    /// <summary>T_User.rest - the old app stores debt, so the new balance should be its negative.</summary>
    public decimal OldRest { get; set; }
    public decimal NewBalance { get; set; }

    /// <summary>Scans the old app recorded that the new one has no counterpart for.</summary>
    public int MissingInNew { get; set; }

    /// <summary>Scans the new app recorded that the old one has no counterpart for.</summary>
    public int MissingInOld { get; set; }

    public bool Matches { get; set; }
}

/// <summary>A single scan that only one of the two systems has.</summary>
public class LegacyScanDiffDto
{
    public int LegacyUserId { get; set; }
    public string UserName { get; set; } = "";
    public DateTime ScannedAt { get; set; }
    public decimal Amount { get; set; }
    public string Side { get; set; } = "";  // "OnlyInOld" or "OnlyInNew"
}

public class LegacyVerificationDto
{
    public DateTime CheckedAt { get; set; }
    public DateTime? From { get; set; }

    /// <summary>How far apart two scans may be and still count as the same scan.</summary>
    public int MatchToleranceSeconds { get; set; }

    public int UsersCompared { get; set; }
    public int UsersMatching { get; set; }
    public int TotalMissingInNew { get; set; }
    public int TotalMissingInOld { get; set; }
    public decimal OldSpentTotal { get; set; }
    public decimal NewSpentTotal { get; set; }
    public decimal OldPaidTotal { get; set; }
    public decimal NewPaidTotal { get; set; }

    public List<LegacyUserComparisonDto> Users { get; set; } = new();

    /// <summary>Capped sample of individual differences, newest first.</summary>
    public List<LegacyScanDiffDto> Differences { get; set; } = new();
    public bool DifferencesTruncated { get; set; }
}

public class LegacyVerificationRequestDto
{
    public LegacyConnectionDto Connection { get; set; } = new();
    public DateTime? From { get; set; }
    public int MatchToleranceSeconds { get; set; } = 5;
}
