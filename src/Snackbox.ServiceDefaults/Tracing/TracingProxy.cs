using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;

namespace Snackbox.ServiceDefaults.Tracing;

/// <summary>
/// DI interception without any third-party weaver: a <see cref="DispatchProxy"/> that sits
/// in front of an interface implementation and, for methods marked <see cref="TracedAttribute"/>
/// (on the method or its class), wraps each call in an Activity carrying the parameters,
/// the outcome and any exception. Untraced members (properties, events, other methods)
/// are forwarded untouched.
/// </summary>
public class TracingProxy<TInterface> : DispatchProxy where TInterface : class
{
    private static readonly ActivitySource Source = new(TelemetrySources.Traced);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
    private const int MaxValueLength = 512;

    private TInterface _target = default!;

    public static TInterface Create(TInterface target)
    {
        var proxy = Create<TInterface, TracingProxy<TInterface>>();
        ((TracingProxy<TInterface>)(object)proxy)._target = target;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null)
            return null;

        var (traced, implMethod) = ResolveTracing(targetMethod);
        if (traced == null)
            return InvokeTarget(targetMethod, args);

        var name = traced.Name ?? $"{_target.GetType().Name}.{targetMethod.Name}";
        var activity = Source.StartActivity(name, ActivityKind.Internal);
        if (activity == null)
            return InvokeTarget(targetMethod, args); // no listener - zero overhead path

        activity.SetTag("code.function", targetMethod.Name);
        activity.SetTag("code.namespace", _target.GetType().FullName);

        if (traced.LogParameters && args != null)
        {
            var parameters = (implMethod ?? targetMethod).GetParameters();
            for (var i = 0; i < parameters.Length && i < args.Length; i++)
            {
                var p = parameters[i];
                var value = p.GetCustomAttribute<SensitiveAttribute>() != null ? "***" : Format(args[i]);
                activity.SetTag($"param.{p.Name}", value);
            }
        }

        object? result;
        try
        {
            result = InvokeTarget(targetMethod, args);
        }
        catch (Exception ex)
        {
            RecordException(activity, ex);
            activity.Dispose();
            throw;
        }

        // Async methods: the activity must stay open until the returned task completes. It is
        // stopped in a continuation, i.e. NOT on the caller's execution context - so restore
        // Activity.Current here, otherwise the caller's next span would become a child of this
        // (already finished) one instead of a sibling. The target's own continuations captured
        // this activity as Current when they started, so their nesting is unaffected.
        if (result is Task task)
        {
            Activity.Current = activity.Parent;
            return WrapTask(task, targetMethod.ReturnType, activity, traced);
        }
        if (result != null && IsValueTask(targetMethod.ReturnType))
        {
            Activity.Current = activity.Parent;
            return WrapValueTask(result, targetMethod.ReturnType, activity, traced);
        }

        if (traced.LogReturnValue && targetMethod.ReturnType != typeof(void))
            activity.SetTag("result", Format(result));
        activity.SetStatus(ActivityStatusCode.Ok);
        activity.Dispose();
        return result;
    }

    private object? InvokeTarget(MethodInfo method, object?[]? args)
    {
        try
        {
            return method.Invoke(_target, args);
        }
        catch (TargetInvocationException tie) when (tie.InnerException != null)
        {
            // Rethrow the real exception with its original stack trace
            ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
            throw; // unreachable
        }
    }

    /// <summary>
    /// The proxy sees the interface method; attributes live on the implementation. Look
    /// there first (method, then class), falling back to the interface declaration.
    /// </summary>
    private (TracedAttribute? traced, MethodInfo? implMethod) ResolveTracing(MethodInfo interfaceMethod)
    {
        var implType = _target.GetType();
        var paramTypes = interfaceMethod.GetParameters().Select(p => p.ParameterType).ToArray();
        var implMethod = implType.GetMethod(interfaceMethod.Name, paramTypes);

        if (implMethod?.GetCustomAttribute<NotTracedAttribute>() != null
            || interfaceMethod.GetCustomAttribute<NotTracedAttribute>() != null)
            return (null, implMethod);

        // Property getters/setters and event accessors are never traced
        if (interfaceMethod.IsSpecialName)
            return (null, implMethod);

        var traced = implMethod?.GetCustomAttribute<TracedAttribute>()
                     ?? implType.GetCustomAttribute<TracedAttribute>()
                     ?? interfaceMethod.GetCustomAttribute<TracedAttribute>()
                     ?? interfaceMethod.DeclaringType?.GetCustomAttribute<TracedAttribute>();

        return (traced, implMethod);
    }

    private static object WrapTask(Task task, Type returnType, Activity activity, TracedAttribute traced)
    {
        if (returnType.IsGenericType) // Task<T>
        {
            var resultType = returnType.GetGenericArguments()[0];
            var method = typeof(TracingProxy<TInterface>)
                .GetMethod(nameof(TraceTaskOfT), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(resultType);
            return method.Invoke(null, [task, activity, traced])!;
        }

        return TraceTask(task, activity);
    }

    private static object WrapValueTask(object valueTask, Type returnType, Activity activity, TracedAttribute traced)
    {
        // Convert to Task, trace it, wrap back into a ValueTask of the same shape
        var asTask = (Task)returnType.GetMethod("AsTask")!.Invoke(valueTask, null)!;
        var tracedTask = WrapTask(asTask, returnType.IsGenericType
            ? typeof(Task<>).MakeGenericType(returnType.GetGenericArguments()[0])
            : typeof(Task), activity, traced);
        return returnType.IsGenericType
            ? Activator.CreateInstance(returnType, tracedTask)!
            : new ValueTask((Task)tracedTask);
    }

    private static async Task TraceTask(Task task, Activity activity)
    {
        try
        {
            await task.ConfigureAwait(false);
            activity.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            RecordException(activity, ex);
            throw;
        }
        finally
        {
            activity.Dispose();
        }
    }

    private static async Task<T> TraceTaskOfT<T>(Task<T> task, Activity activity, TracedAttribute traced)
    {
        try
        {
            var result = await task.ConfigureAwait(false);
            if (traced.LogReturnValue)
                activity.SetTag("result", Format(result));
            activity.SetStatus(ActivityStatusCode.Ok);
            return result;
        }
        catch (Exception ex)
        {
            RecordException(activity, ex);
            throw;
        }
        finally
        {
            activity.Dispose();
        }
    }

    private static void RecordException(Activity activity, Exception ex)
    {
        activity.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity.AddException(ex); // "exception" event with type, message and stack trace
    }

    private static bool IsValueTask(Type t) =>
        t == typeof(ValueTask) || (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ValueTask<>));

    /// <summary>Compact, bounded representation of a parameter or return value.</summary>
    internal static string Format(object? value)
    {
        switch (value)
        {
            case null:
                return "null";
            case string s:
                return Truncate(s);
            case IFormattable f when value.GetType().IsPrimitive || value is decimal || value is DateTime || value is DateTimeOffset || value is TimeSpan || value is Guid || value is Enum:
                return f.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
            case CancellationToken:
                return "CancellationToken";
            case ICollection c when value is not IDictionary:
                return $"{value.GetType().Name}[{c.Count}]";
        }

        try
        {
            return Truncate(JsonSerializer.Serialize(value, value.GetType(), JsonOptions));
        }
        catch
        {
            return Truncate(value.ToString() ?? value.GetType().Name);
        }
    }

    private static string Truncate(string s) =>
        s.Length <= MaxValueLength ? s : s[..MaxValueLength] + "…";
}
