using System;
#if NET6_0_OR_GREATER
using System.Runtime;
#endif

namespace Pidgin.Incremental;

/// <summary>
/// If you check the Target and it's null, you should null out your
/// copy of the ConditionalWeakReference for collection.
/// </summary>
internal class ConditionalWeakReference
{
#if NET6_0_OR_GREATER
    // DependentHandle is a mutable struct - don't make this readonly
    private DependentHandle _handle;

    public ConditionalWeakReference(object? target, object? dependent)
    {
        _handle = new(target, dependent);
    }

    public object? Target
    {
        get
        {
            var result = _handle.Target;
            GC.KeepAlive(this);
            return result;
        }
    }

    public object? Dependent
    {
        get
        {
            var result = _handle.Dependent;
            GC.KeepAlive(this);
            return result;
        }
    }

    public (object? Target, object? Dependent) TargetAndDependent
    {
        get
        {
            var result = _handle.TargetAndDependent;
            GC.KeepAlive(this);
            return result;
        }
    }

    // This is thread safe because the finaliser can't happen
    // concurrently with any instance methods, as long as all
    // instance methods call GC.KeepAlive(this)
    ~ConditionalWeakReference()
    {
        // DependentHandle.Dispose is idempotent.
        _handle.Dispose();
    }
#else
    private readonly WeakReference _target;
    private readonly object? _dependent;

    public ConditionalWeakReference(object? target, object? dependent)
    {
        _target = new WeakReference(target);
        _dependent = dependent;
    }

    public object? Target
    {
        get
        {
            var result = _target.Target;
            GC.KeepAlive(this);
            return result;
        }
    }

    public object? Dependent
    {
        get
        {
            var result = _dependent;
            GC.KeepAlive(this);
            return result;
        }
    }

    public (object? Target, object? Dependent) TargetAndDependent
    {
        get
        {
            var result = (_target.Target, _dependent);
            GC.KeepAlive(this);
            return result;
        }
    }
#endif
}
