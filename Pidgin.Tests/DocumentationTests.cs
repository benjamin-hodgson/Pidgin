using System.Threading.Tasks;

using Benjamin.Pizza.DocTest;

using Xunit;

namespace Pidgin.Tests;

public class DocumentationTests
{
    private const string _preamble = @$"
        using {nameof(Pidgin)};
        using {nameof(Pidgin)}.{nameof(Expression)};
        using static {nameof(Pidgin)}.{nameof(Parser)};
        using static {nameof(Pidgin)}.{nameof(Parser)}<char>;
    ";

    [Theory]
    [DocTestData(typeof(Parser), Preamble = _preamble)]
    public async Task TestDocumentation(DocTest test)
    {
        await test.Run();
    }
}
