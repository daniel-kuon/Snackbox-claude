using Microsoft.Data.SqlClient;
using Snackbox.Api.Dtos;

namespace Snackbox.Api.Services.LegacyImport;

public record LegacyUser(int UserId, string Name, string? Email, decimal Rest);

public record LegacyCode(int CodeId, int UserId, string Code, decimal Price, bool IsSnackCode);

public record LegacyScan(Guid PostenId, int UserId, int CodeId, decimal Price, DateTime Time);

public record LegacyPayment(Guid ToPayId, int UserId, decimal Amount, DateTime Time);

/// <summary>
/// Reads the old Snackbox SQL Server database. Its tables are German-named leftovers:
/// T_User (rest = debt), T_UserCodes (the cards and snack codes), T_Posten (one row per
/// barcode scan) and T_ToPay (payments). The time tracking tables are deliberately ignored.
/// </summary>
public class LegacySnackboxReader
{
    private readonly string _connectionString;

    public LegacySnackboxReader(LegacyConnectionDto connection)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = connection.Server,
            InitialCatalog = connection.Database,
            TrustServerCertificate = connection.TrustServerCertificate,
            ConnectTimeout = 15
        };

        if (string.IsNullOrWhiteSpace(connection.Username))
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = connection.Username;
            builder.Password = connection.Password ?? "";
        }

        _connectionString = builder.ConnectionString;
    }

    public async Task<LegacyConnectionTestDto> TestAsync(CancellationToken ct = default)
    {
        try
        {
            await using var db = await OpenAsync(ct);
            await using var cmd = db.CreateCommand();
            cmd.CommandText = """
                SELECT (SELECT COUNT(*) FROM T_User),
                       (SELECT COUNT(*) FROM T_UserCodes),
                       (SELECT COUNT(*) FROM T_Posten),
                       (SELECT COUNT(*) FROM T_ToPay),
                       (SELECT MIN(Time) FROM T_Posten),
                       (SELECT MAX(Time) FROM T_Posten)
                """;
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct)) return new LegacyConnectionTestDto { Connected = false, Error = "No result" };

            return new LegacyConnectionTestDto
            {
                Connected = true,
                Users = reader.GetInt32(0),
                Codes = reader.GetInt32(1),
                Scans = reader.GetInt32(2),
                Payments = reader.GetInt32(3),
                FirstScan = reader.IsDBNull(4) ? null : DateTime.SpecifyKind(reader.GetDateTime(4), DateTimeKind.Utc),
                LastScan = reader.IsDBNull(5) ? null : DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc)
            };
        }
        catch (Exception ex)
        {
            // The message is shown to the admin who typed the connection - it must not echo
            // the password back, and SqlException messages never contain it.
            return new LegacyConnectionTestDto { Connected = false, Error = ex.Message };
        }
    }

    public async Task<List<LegacyUser>> GetUsersAsync(CancellationToken ct = default) =>
        await QueryAsync("SELECT UserID, UserName, EMail, rest FROM T_User",
                         r => new LegacyUser(
                             r.GetInt32(0),
                             r.IsDBNull(1) ? "" : r.GetString(1).Trim(),
                             r.IsDBNull(2) ? null : r.GetString(2).Trim(),
                             r.IsDBNull(3) ? 0m : r.GetDecimal(3)),
                         ct);

    public async Task<List<LegacyCode>> GetCodesAsync(CancellationToken ct = default) =>
        await QueryAsync("SELECT CodeID, UserID, UserCode, Preis, IsSnackCode FROM T_UserCodes",
                         r => new LegacyCode(
                             r.GetInt32(0),
                             r.GetInt32(1),
                             r.IsDBNull(2) ? "" : r.GetString(2).Trim(),
                             r.IsDBNull(3) ? 0m : r.GetDecimal(3),
                             !r.IsDBNull(4) && r.GetBoolean(4)),
                         ct);

    public async Task<List<LegacyScan>> GetScansAsync(DateTime? from, CancellationToken ct = default) =>
        await QueryAsync("SELECT PostenID, UserID, CodeID, Preis, Time FROM T_Posten WHERE Time IS NOT NULL"
                         + (from.HasValue ? " AND Time >= @from" : "") + " ORDER BY UserID, Time",
                         r => new LegacyScan(
                             r.GetGuid(0),
                             r.GetInt32(1),
                             r.GetInt32(2),
                             r.IsDBNull(3) ? 0m : r.GetDecimal(3),
                             AsUtc(r.GetDateTime(4))),
                         ct, from);

    public async Task<List<LegacyPayment>> GetPaymentsAsync(DateTime? from, CancellationToken ct = default) =>
        await QueryAsync("SELECT ToPayID, userid, pay, Time FROM T_ToPay WHERE userid IS NOT NULL AND Time IS NOT NULL"
                         + (from.HasValue ? " AND Time >= @from" : "") + " ORDER BY userid, Time",
                         r => new LegacyPayment(
                             r.GetGuid(0),
                             r.GetInt32(1),
                             r.IsDBNull(2) ? 0m : r.GetDecimal(2),
                             AsUtc(r.GetDateTime(3))),
                         ct, from);

    /// <summary>
    /// The old app stores local time without an offset. The new one stores UTC, but both run
    /// on the same machine in the same zone, so the wall clock is what has to line up - we
    /// keep the value and only tag it, rather than shifting it by an offset nobody recorded.
    /// </summary>
    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        var db = new SqlConnection(_connectionString);
        await db.OpenAsync(ct);
        return db;
    }

    private async Task<List<T>> QueryAsync<T>(string sql, Func<SqlDataReader, T> map, CancellationToken ct, DateTime? from = null)
    {
        await using var db = await OpenAsync(ct);
        await using var cmd = db.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandTimeout = 120;
        if (from.HasValue) cmd.Parameters.AddWithValue("@from", from.Value);

        var result = new List<T>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(map(reader));
        return result;
    }
}
