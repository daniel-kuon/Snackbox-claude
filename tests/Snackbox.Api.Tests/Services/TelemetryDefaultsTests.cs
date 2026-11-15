using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using Snackbox.ServiceDefaults;
using Xunit;

namespace Snackbox.Api.Tests.Services;

public class TelemetryDefaultsTests
{
    /// <summary>
    /// The kiosk flushes this provider when it crashes (MauiProgram.ReportCrash); if it could not
    /// be resolved, the crash's log record would die with the process again, silently.
    /// </summary>
    [Fact]
    public void LoggingSetup_RegistersTheLoggerProvider_SoACrashCanFlushIt()
    {
        var services = new ServiceCollection();
        services.AddLogging(logging => logging.AddSnackboxOpenTelemetryLogging(new ConfigurationBuilder().Build(), "test"));

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<LoggerProvider>());
    }
}
