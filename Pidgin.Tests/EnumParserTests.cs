using System;
using Xunit;

namespace Pidgin.Tests;

public class EnumParserTests
{
      internal enum TestEnum
      {
            Value1 = 0,
            Value2,
            Value3
      }  
      
      [Fact]
      public void EnumParseTest()
      {
        const string value1 = "value1";
        var result = Parser.Enum<TestEnum>().Parse(value1);
        Assert.False(result.Success);
        
        result = Parser.CIEnum<TestEnum>().Parse(value1);
        Assert.Equal(TestEnum.Value1, result.Value);
        
        const string value2 = "Value2";
        result = Parser.Enum<TestEnum>().Parse(value2);
        Assert.Equal(TestEnum.Value2, result.Value);

        result = Parser.CIEnum<TestEnum>().Parse(value2);
        Assert.Equal(TestEnum.Value2, result.Value);

      }
}