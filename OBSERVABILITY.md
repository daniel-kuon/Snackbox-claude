# Snackbox OpenTelemetry Observability Setup

This document describes the comprehensive OpenTelemetry (OTEL) integration implemented in Snackbox for monitoring, tracing, and logging.

## Architecture

The system uses a modern observability stack integrated with .NET Aspire:

```
┌─────────────┐     ┌──────────────┐
│  Blazor Web │────▶│ Snackbox API │
└─────────────┘     └──────────────┘
       │                    │
       │                    │
       ▼                    ▼
   ┌────────────────────────────┐
   │   OpenTelemetry Collector  │
   │     (via OTLP/gRPC)        │
   └────────────────────────────┘
                │
      ┌─────────┼─────────┐
      │         │         │
      ▼         ▼         ▼
  ┌──────┐ ┌───────┐ ┌──────────┐
  │ Loki │ │ Tempo │ │Prometheus│
  │(Logs)│ │(Traces│ │(Metrics) │
  └──────┘ └───────┘ └──────────┘
      │         │         │
      └─────────┼─────────┘
                ▼
          ┌──────────┐
          │ Grafana  │
          │(Visualize│
          └──────────┘
```

## Components

### 1. Observability Backend

All components run as Docker containers orchestrated by .NET Aspire:

- **Grafana** (http://localhost:3000)
  - Central dashboard for all observability data
  - Pre-configured with anonymous admin access for development
  - Aggregates logs, traces, and metrics

- **Loki** (http://localhost:3100)
  - Log aggregation system
  - Stores structured logs from all services
  - 31-day retention period configured

- **Tempo** (http://localhost:3200, OTLP: 4317/4318)
  - Distributed tracing backend
  - Receives traces via OTLP protocol
  - Correlates requests across services

- **Prometheus** (http://localhost:9090)
  - Metrics collection and storage
  - Configured with OTLP write receiver
  - Scrapes metrics from services

### 2. Backend (Snackbox.Api)

The API implements comprehensive telemetry:

#### Activity Sources & Tracing
- Custom `ActivitySource`: `Snackbox.Api v1.0.0`
- Distributed traces for all operations:
  - Authentication flows
  - Product management (CRUD)
  - Purchase workflows
  - Payment processing
  - Barcode operations

#### Custom Metrics
All metrics use the `Snackbox.Api` meter:

| Metric | Type | Description |
|--------|------|-------------|
| `snackbox.purchases.count` | Counter | Total purchases completed |
| `snackbox.product_scans.count` | Counter | Products scanned |
| `snackbox.purchases.amount` | Histogram (CHF) | Purchase amounts distribution |
| `snackbox.payments.count` | Counter | Payments recorded |
| `snackbox.payments.amount` | Histogram (CHF) | Payment amounts distribution |
| `snackbox.products.created` | Counter | Products created |
| `snackbox.batches.created` | Counter | Batches created |
| `snackbox.auth.attempts` | Counter | Authentication attempts |
| `snackbox.auth.success` | Counter | Successful authentications |
| `snackbox.auth.failure` | Counter | Failed authentications |

#### Structured Logging
Every significant operation logs:
- **Input parameters** (sanitized for security)
- **Operation outcomes** (success/failure)
- **Timing information**
- **User context** (user IDs, not PII)
- **Error details** with stack traces

Example log entry:
```
[2026-01-01 18:45:23 INF] Purchase completed successfully. 
PurchaseId: 42, UserId: 7, ItemCount: 3, TotalAmount: 15.50
```

#### Enhanced Controllers
All controllers include:
- Activity creation with semantic tags
- Detailed logging at key decision points
- Metrics recording for business events
- Error tracking with activity status codes

### 3. Frontend (Snackbox.BlazorServer)

The Blazor Server app also emits telemetry:
- HTTP client instrumentation (outgoing API calls)
- ASP.NET Core instrumentation (incoming requests)
- Runtime metrics (GC, thread pool, etc.)
- Custom frontend activities (when needed)

## Configuration

### OTLP Endpoint
Services are configured to send telemetry to Tempo:
```
OTEL_EXPORTER_OTLP_ENDPOINT=http://tempo:4317
```

### Service Identification
Each service identifies itself:
- **API**: `Snackbox.Api v1.0.0`
- **Web**: `Snackbox.BlazorServer v1.0.0`

### Sampling
Currently configured with `AlwaysOnSampler` - all traces are captured.
This is appropriate for the low-volume scenario (few dozen requests per week).

## Usage

### Starting the Stack

Run the Aspire AppHost:
```bash
cd src/Snackbox.AppHost
dotnet run
```

This will start:
1. PostgreSQL database
2. Observability stack (Grafana, Loki, Tempo, Prometheus)
3. Snackbox API
4. Blazor Server web app

### Accessing Grafana

1. Navigate to http://localhost:3000
2. No login required (anonymous admin access enabled for dev)
3. Add data sources:
   - Loki: http://loki:3100
   - Tempo: http://tempo:3200
   - Prometheus: http://prometheus:9090

### Viewing Telemetry

#### Logs (Loki)
In Grafana:
1. Go to Explore
2. Select Loki data source
3. Query examples:
   ```
   {service_name="Snackbox.Api"}
   {service_name="Snackbox.Api"} |= "Purchase"
   {service_name="Snackbox.Api"} | json | level="Error"
   ```

#### Traces (Tempo)
In Grafana:
1. Go to Explore
2. Select Tempo data source
3. Search by:
   - Service name
   - Operation name
   - Duration
   - Tags (user.id, product.id, etc.)

#### Metrics (Prometheus)
In Grafana:
1. Go to Explore
2. Select Prometheus data source
3. Query examples:
   ```promql
   rate(snackbox_purchases_count[5m])
   histogram_quantile(0.95, snackbox_purchases_amount_bucket)
   snackbox_auth_success / snackbox_auth_attempts
   ```

## Correlation

All telemetry is correlated using:
- **Trace ID**: Links all spans in a distributed trace
- **Span ID**: Identifies individual operations
- **Parent Span ID**: Creates operation hierarchy

Example flow:
```
HTTP Request → Activity: CompletePurchase
  ├─ Activity: Database Query
  ├─ Activity: Calculate Total
  └─ Metric: purchase_amount recorded
  └─ Logs: "Purchase completed successfully"
```

## Development Notes

### Adding New Telemetry

To add telemetry to a new controller:

1. **Import namespaces:**
   ```csharp
   using System.Diagnostics;
   using Snackbox.Api.Telemetry;
   ```

2. **Create activities:**
   ```csharp
   using var activity = SnackboxTelemetry.ActivitySource.StartActivity("OperationName");
   activity?.SetTag("key", "value");
   ```

3. **Record metrics:**
   ```csharp
   SnackboxTelemetry.CustomCounter.Add(1, 
       new KeyValuePair<string, object?>("tag", "value"));
   ```

4. **Add logging:**
   ```csharp
   _logger.LogInformation("Operation completed. Details: {Data}", data);
   ```

### Best Practices

- ✅ **DO** log operation start and completion
- ✅ **DO** include relevant IDs (user, product, purchase)
- ✅ **DO** set activity status on errors
- ✅ **DO** use structured logging parameters
- ✅ **DO** sanitize sensitive data (mask barcodes, no passwords)
- ❌ **DON'T** log PII (emails, names) unless necessary
- ❌ **DON'T** log entire objects (select specific properties)
- ❌ **DON'T** create activities for trivial operations

## Production Considerations

For production deployment:

1. **Sampling**: Change to `TraceIdRatioBasedSampler` if volume increases
2. **Retention**: Configure appropriate retention policies in Loki/Tempo
3. **Security**: Enable authentication in Grafana
4. **Alerting**: Set up alerts on key metrics
5. **Storage**: Mount persistent volumes for data
6. **Backup**: Configure backup strategies for observability data

## Troubleshooting

### No telemetry appearing

1. Check OTLP endpoint is reachable:
   ```bash
   curl http://localhost:4317
   ```

2. Verify service is starting:
   ```bash
   dotnet run --project src/Snackbox.Api
   ```

3. Check logs for OTLP exporter errors

### Grafana not showing data

1. Verify data sources are configured correctly
2. Check time range in Grafana
3. Ensure services have generated telemetry (make some requests)

### High cardinality warnings

If you see cardinality warnings:
- Review metric labels
- Consider aggregating before exporting
- Reduce label combinations

## Resources

- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/languages/net/)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Grafana Documentation](https://grafana.com/docs/)
- [Loki Documentation](https://grafana.com/docs/loki/)
- [Tempo Documentation](https://grafana.com/docs/tempo/)
