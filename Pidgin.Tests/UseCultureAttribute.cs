using System;
using System.Globalization;
using System.Reflection;
using System.Threading;

using Xunit.Sdk;

namespace Pidgin.Tests;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class UseCultureAttribute : BeforeAfterTestAttribute
{
    private readonly Lazy<CultureInfo> _culture;
    private readonly Lazy<CultureInfo> _uiCulture;
    private CultureInfo? _originalCulture;
    private CultureInfo? _originalUICulture;

    public UseCultureAttribute(string culture)
        : this(culture, culture) { }

    public UseCultureAttribute(string culture, string uiCulture)
    {
        _culture = new Lazy<CultureInfo>(() => new(culture));
        _uiCulture = new Lazy<CultureInfo>(() => new(uiCulture));
    }

    public CultureInfo Culture => _culture.Value;

    public CultureInfo UICulture => _uiCulture.Value;

    public override void Before(MethodInfo methodUnderTest)
    {
        _originalCulture = Thread.CurrentThread.CurrentCulture;
        _originalUICulture = Thread.CurrentThread.CurrentUICulture;

        Thread.CurrentThread.CurrentCulture = Culture;
        Thread.CurrentThread.CurrentUICulture = UICulture;
    }

    public override void After(MethodInfo methodUnderTest)
    {
        Thread.CurrentThread.CurrentCulture = _originalCulture!;
        Thread.CurrentThread.CurrentUICulture = _originalUICulture!;
    }
}
