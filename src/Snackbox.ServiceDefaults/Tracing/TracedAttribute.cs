namespace Snackbox.ServiceDefaults.Tracing;

/// <summary>
/// Marks a class (all its public interface methods) or a single method for automatic
/// tracing: each call becomes an <see cref="System.Diagnostics.Activity"/> named
/// <c>{Type}.{Method}</c> with the parameters as tags; exceptions are recorded on the
/// span and set its status to Error.
///
/// Applies to services resolved through DI behind an interface — register them with
/// <c>services.AddTracedServices()</c> (auto-discovers attributed implementation types) or
/// <c>services.AddTracing&lt;TInterface&gt;()</c> (for factory-registered ones such as typed HttpClients).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class TracedAttribute : Attribute
{
    /// <summary>Record method parameters as tags (default true).</summary>
    public bool LogParameters { get; init; } = true;

    /// <summary>Record the return value as a tag (default false - often large).</summary>
    public bool LogReturnValue { get; init; }

    /// <summary>Override the activity name (default <c>{Type}.{Method}</c>).</summary>
    public string? Name { get; init; }
}

/// <summary>Excludes a method from tracing when its class is marked <see cref="TracedAttribute"/>.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class NotTracedAttribute : Attribute
{
}

/// <summary>Masks a parameter's value in the trace (passwords, tokens, personal data).</summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class SensitiveAttribute : Attribute
{
}
