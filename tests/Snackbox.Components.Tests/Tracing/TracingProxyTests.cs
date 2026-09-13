using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Snackbox.ServiceDefaults;
using Snackbox.ServiceDefaults.Tracing;
using Xunit;

namespace Snackbox.Components.Tests.Tracing;

public class TracingProxyTests : IDisposable
{
    public interface ICalc
    {
        int Add(int a, int b);
        Task<string> GreetAsync(string name, string secret);
        Task FailAsync(string reason);
        int Untraced(int x);
        int Counter { get; }
    }

    [Traced(LogReturnValue = true)]
    public class Calc : ICalc
    {
        public int Counter { get; private set; }

        public int Add(int a, int b) { Counter++; return a + b; }

        public async Task<string> GreetAsync(string name, [Sensitive] string secret)
        {
            await Task.Yield();
            return $"hi {name}";
        }

        public async Task FailAsync(string reason)
        {
            await Task.Yield();
            throw new InvalidOperationException(reason);
        }

        [NotTraced]
        public int Untraced(int x) => x * 2;
    }

    private readonly List<Activity> _activities = new();
    private readonly ActivityListener _listener;

    public TracingProxyTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == TelemetrySources.Traced || s.Name == "test-root",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a => { if (a.Source.Name == TelemetrySources.Traced) _activities.Add(a); }
        };
        ActivitySource.AddActivityListener(_listener);
    }

    private static ICalc Proxy() => TracingProxy<ICalc>.Create(new Calc());

    [Fact]
    public void SyncMethod_CreatesSpanWithParametersAndResult()
    {
        var result = Proxy().Add(2, 3);

        Assert.Equal(5, result);
        var span = Assert.Single(_activities);
        Assert.Equal("Calc.Add", span.DisplayName);
        Assert.Equal("2", span.GetTagItem("param.a"));
        Assert.Equal("3", span.GetTagItem("param.b"));
        Assert.Equal("5", span.GetTagItem("result"));
        Assert.Equal(ActivityStatusCode.Ok, span.Status);
    }

    [Fact]
    public async Task AsyncMethod_SpanStaysOpenUntilCompletion_AndMasksSensitiveParameter()
    {
        var result = await Proxy().GreetAsync("Jane", "p@ssw0rd");

        Assert.Equal("hi Jane", result);
        var span = Assert.Single(_activities);
        Assert.Equal("Calc.GreetAsync", span.DisplayName);
        Assert.Equal("Jane", span.GetTagItem("param.name"));
        Assert.Equal("***", span.GetTagItem("param.secret"));
        Assert.Equal("hi Jane", span.GetTagItem("result"));
    }

    [Fact]
    public async Task AsyncMethod_DoesNotLeaveFinishedSpanAsCallersCurrent_SoSequentialCallsAreSiblings()
    {
        using var root = new ActivitySource("test-root").StartActivity("root")
                         ?? throw new InvalidOperationException("listener missing");
        var calc = Proxy();

        await calc.GreetAsync("a", "x");
        Assert.Same(root, Activity.Current); // caller context restored after the awaited call
        await calc.GreetAsync("b", "y");

        Assert.Equal(2, _activities.Count);
        Assert.All(_activities, a => Assert.Equal(root.SpanId, a.ParentSpanId)); // siblings under root, not nested
    }

    [Fact]
    public async Task AsyncMethod_ThatThrows_RecordsExceptionAndRethrowsOriginal()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Proxy().FailAsync("boom"));

        Assert.Equal("boom", ex.Message);
        var span = Assert.Single(_activities);
        Assert.Equal(ActivityStatusCode.Error, span.Status);
        var evt = Assert.Single(span.Events, e => e.Name == "exception");
        Assert.Contains(evt.Tags, t => t.Key == "exception.type" && (string?)t.Value == typeof(InvalidOperationException).FullName);
        Assert.Contains(evt.Tags, t => t.Key == "exception.message" && (string?)t.Value == "boom");
    }

    [Fact]
    public void NotTracedMethod_AndPropertyGetter_ProduceNoSpans()
    {
        var calc = Proxy();

        Assert.Equal(8, calc.Untraced(4));
        _ = calc.Counter;

        Assert.Empty(_activities);
    }

    [Fact]
    public void AddTracedServices_WrapsAttributedRegistrations_KeepingLifetime()
    {
        var services = new ServiceCollection();
        services.AddScoped<ICalc, Calc>();

        services.AddTracedServices();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(ICalc));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.NotNull(descriptor.ImplementationFactory); // replaced by the proxy factory
        Assert.Null(descriptor.ImplementationType);
    }

    [Fact]
    public void AddTracing_WithoutRegistration_Throws()
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddTracing<ICalc>());
    }

    public void Dispose() => _listener.Dispose();
}
