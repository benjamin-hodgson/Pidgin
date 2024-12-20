using System;
using System.Globalization;
using System.Reflection;

using Xunit.v3;

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
        : this(culture, culture)
    {
    }

    public UseCultureAttribute(string culture, string uiCulture)
    {
        Culture = culture;
        UiCulture = uiCulture;
        _culture = new(culture);
        _uiCulture = new(uiCulture);
    }

    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        _originalCulture = CultureInfo.CurrentCulture;
        _originalUICulture = CultureInfo.CurrentUICulture;

        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
    }

    public override void After(MethodInfo methodUnderTest, IXunitTest test)
    {
        CultureInfo.CurrentCulture = _originalCulture!;
        CultureInfo.CurrentUICulture = _originalUICulture!;
    }
}
