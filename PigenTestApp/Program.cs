using System;

namespace PidgenTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string resultStr = ApplicationSupport.Parsers.TestExpr.TestExpressions();
            Console.Write(resultStr);
            //Console.WriteLine("Hello World!");
        }
    }
}
