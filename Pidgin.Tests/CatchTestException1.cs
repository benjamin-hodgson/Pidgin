using System;

namespace Pidgin.Tests;

public class CatchTest1Exception : Exception
{
    public CatchTest1Exception()
    {
    }

    public CatchTest1Exception(string message)
        : base(message)
    {
    }

    public CatchTest1Exception(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
