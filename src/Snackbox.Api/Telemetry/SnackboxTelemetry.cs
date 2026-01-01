using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Snackbox.Api.Telemetry;

public static class SnackboxTelemetry
{
    public const string ServiceName = "Snackbox.Api";
    public const string ServiceVersion = "1.0.0";

    // Activity Source for distributed tracing
    public static readonly ActivitySource ActivitySource = new(ServiceName, ServiceVersion);

    // Meter for custom metrics
    public static readonly Meter Meter = new(ServiceName, ServiceVersion);

    // Custom metrics
    public static readonly Counter<long> PurchaseCounter = Meter.CreateCounter<long>(
        "snackbox.purchases.count",
        description: "Number of purchases completed");

    public static readonly Counter<long> ProductScanCounter = Meter.CreateCounter<long>(
        "snackbox.product_scans.count",
        description: "Number of product scans");

    public static readonly Histogram<double> PurchaseAmountHistogram = Meter.CreateHistogram<double>(
        "snackbox.purchases.amount",
        unit: "CHF",
        description: "Purchase amount in CHF");

    public static readonly Counter<long> PaymentCounter = Meter.CreateCounter<long>(
        "snackbox.payments.count",
        description: "Number of payments recorded");

    public static readonly Histogram<double> PaymentAmountHistogram = Meter.CreateHistogram<double>(
        "snackbox.payments.amount",
        unit: "CHF",
        description: "Payment amount in CHF");

    public static readonly Counter<long> ProductCreatedCounter = Meter.CreateCounter<long>(
        "snackbox.products.created",
        description: "Number of products created");

    public static readonly Counter<long> BatchCreatedCounter = Meter.CreateCounter<long>(
        "snackbox.batches.created",
        description: "Number of batches created");

    public static readonly Counter<long> AuthenticationAttemptCounter = Meter.CreateCounter<long>(
        "snackbox.auth.attempts",
        description: "Number of authentication attempts");

    public static readonly Counter<long> AuthenticationSuccessCounter = Meter.CreateCounter<long>(
        "snackbox.auth.success",
        description: "Number of successful authentications");

    public static readonly Counter<long> AuthenticationFailureCounter = Meter.CreateCounter<long>(
        "snackbox.auth.failure",
        description: "Number of failed authentications");
}
