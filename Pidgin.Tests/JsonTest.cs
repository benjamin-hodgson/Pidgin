using Pidgin.Examples.Json;
using Xunit;

namespace Pidgin.Tests
{
    public class JsonTest
    {
        [Fact]
        public void TestJsonObject()
        {
            var input = "[ { \"foo\" : \"bar\" } , [ \"baz\" ] ]";

            var result = JsonParser.Parse(input);

            Assert.Equal("[{\"foo\":\"bar\"},[\"baz\"]]", result.Value.ToString());
        }
    }
}