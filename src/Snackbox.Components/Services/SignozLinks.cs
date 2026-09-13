using System.Text.Json;

namespace Snackbox.Components.Services;

/// <summary>
/// Builds deep links into SigNoz. The traces explorer takes its state in a
/// <c>compositeQuery</c> query parameter holding JSON that is URL-encoded twice; the filter
/// itself is a plain expression such as <c>user.id = 14 OR param.userId = '14'</c>.
/// </summary>
public static class SignozLinks
{
    public const string DefaultRelativeTime = "1w";

    /// <summary>Opens one trace (all spans and logs of a single request).</summary>
    public static string Trace(string? baseUrl, string traceId) =>
        $"{Normalize(baseUrl)}/trace/{traceId}";

    /// <summary>Opens the traces explorer filtered by <paramref name="filterExpression"/>.</summary>
    public static string Traces(string? baseUrl, string filterExpression, string relativeTime = DefaultRelativeTime)
    {
        var query = new
        {
            queryType = "builder",
            builder = new
            {
                queryData = new object[]
                {
                    new
                    {
                        dataSource = "traces",
                        queryName = "A",
                        aggregateAttribute = new { id = "----", dataType = "", key = "", type = "" },
                        timeAggregation = "rate",
                        spaceAggregation = "sum",
                        filter = new { expression = filterExpression },
                        aggregations = new object[] { new { expression = "count() " } },
                        functions = Array.Empty<object>(),
                        filters = new { items = Array.Empty<object>(), op = "AND" },
                        expression = "A",
                        disabled = false,
                        stepInterval = (int?)null,
                        having = Array.Empty<object>(),
                        limit = (int?)null,
                        orderBy = Array.Empty<object>(),
                        groupBy = Array.Empty<object>(),
                        legend = "",
                        reduceTo = "avg"
                    }
                },
                queryFormulas = Array.Empty<object>(),
                queryTraceOperator = Array.Empty<object>()
            },
            id = "snackbox",
            unit = ""
        };

        // SigNoz reads this parameter after decoding it twice
        var encoded = Uri.EscapeDataString(Uri.EscapeDataString(JsonSerializer.Serialize(query)));
        return $"{Normalize(baseUrl)}/traces-explorer?compositeQuery={encoded}&relativeTime={relativeTime}";
    }

    /// <summary>Everything recorded for one user: scans tag user.id, traced services tag param.userId.</summary>
    public static string TracesForUser(string? baseUrl, int userId, string? username = null)
    {
        var expression = $"user.id = {userId} OR param.userId = '{userId}'";
        if (!string.IsNullOrWhiteSpace(username))
            expression += $" OR user.username = '{Escape(username)}'";
        return Traces(baseUrl, expression);
    }

    /// <summary>Everything recorded for one product: its id, its name and any of its barcodes.</summary>
    public static string TracesForProduct(string? baseUrl, int productId, string? productName = null,
        IEnumerable<string>? barcodes = null)
    {
        var expression = $"param.productId = '{productId}'";
        if (!string.IsNullOrWhiteSpace(productName))
            expression += $" OR param.productName = '{Escape(productName)}'";
        foreach (var code in (barcodes ?? Array.Empty<string>()).Where(c => !string.IsNullOrWhiteSpace(c)))
            expression += $" OR param.barcode = '{Escape(code)}'";
        return Traces(baseUrl, expression);
    }

    private static string Normalize(string? baseUrl) =>
        (string.IsNullOrWhiteSpace(baseUrl) ? "http://localhost:3301" : baseUrl).TrimEnd('/');

    private static string Escape(string value) => value.Replace("'", "\\'");
}
