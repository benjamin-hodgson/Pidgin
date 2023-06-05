using System;

namespace Pidgin.Tests;

public class CatchTest2Exception : Exception
{
    public CatchTest2Exception()
    {
    }

    public CatchTest2Exception(string message)
        : base(message)
    {
    }

    public CatchTest2Exception(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
