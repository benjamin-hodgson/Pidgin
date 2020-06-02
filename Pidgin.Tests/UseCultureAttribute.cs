using System;
using System.Globalization;
using System.Reflection;
using System.Threading;

using Xunit.Sdk;

namespace Pidgin.Tests
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class UseCultureAttribute : BeforeAfterTestAttribute
    {
        readonly Lazy<CultureInfo> culture;
        readonly Lazy<CultureInfo> uiCulture;

        CultureInfo? originalCulture;
        CultureInfo? originalUICulture;

        public UseCultureAttribute(string culture)
            : this(culture, culture) { }

        public UseCultureAttribute(string culture, string uiCulture)
        {
            this.culture = new Lazy<CultureInfo>(() => new CultureInfo(culture));
            this.uiCulture = new Lazy<CultureInfo>(() => new CultureInfo(uiCulture));
        }

        public CultureInfo Culture { get { return culture.Value; } }

        public CultureInfo UICulture { get { return uiCulture.Value; } }

        public override void Before(MethodInfo methodUnderTest)
        {
            originalCulture = Thread.CurrentThread.CurrentCulture;
            originalUICulture = Thread.CurrentThread.CurrentUICulture;

            Thread.CurrentThread.CurrentCulture = Culture;
            Thread.CurrentThread.CurrentUICulture = UICulture;
        }

        public override void After(MethodInfo methodUnderTest)
        {
            Thread.CurrentThread.CurrentCulture = originalCulture!;
            Thread.CurrentThread.CurrentUICulture = originalUICulture!;
        }
    }
}
