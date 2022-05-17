using System;
using System.Globalization;
using System.Reflection;
using System.Threading;

using Xunit.Sdk;

namespace Pidgin.Tests;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class UseCultureAttribute : BeforeAfterTestAttribute
{
    private readonly CultureInfo _culture;
    private readonly CultureInfo _uiCulture;
    private CultureInfo? _originalCulture;
    private CultureInfo? _originalUICulture;
    public string Culture { get; }
    public string UiCulture { get; }

    public UseCultureAttribute(string culture)
        : this(culture, culture) { }

    public UseCultureAttribute(string culture, string uiCulture)
    {
        Culture = culture;
        UiCulture = uiCulture;
        _culture = new(culture);
        _uiCulture = new(uiCulture);
    }

    public override void Before(MethodInfo methodUnderTest)
    {
        _originalCulture = Thread.CurrentThread.CurrentCulture;
        _originalUICulture = Thread.CurrentThread.CurrentUICulture;

        Thread.CurrentThread.CurrentCulture = _culture;
        Thread.CurrentThread.CurrentUICulture = _uiCulture;
    }

    public override void After(MethodInfo methodUnderTest)
    {
        Thread.CurrentThread.CurrentCulture = _originalCulture!;
        Thread.CurrentThread.CurrentUICulture = _originalUICulture!;
    }

}
