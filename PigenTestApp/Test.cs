using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ApplicationSupport.Parsers
{
    public class TestExpr
    {
        public static string TestExpressions()
        {
            // Create new stopwatch.
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            var eval = new Eval();
            eval.EnableLog = true;
            eval.Variables = new Dictionary<string, object>();

            object Result;
            Result = eval.ParseString("1", 1);
            Result = eval.ParseString("1+2", 3);
            Result = eval.ParseString("(1+2)+3+4", 10);
            Result = eval.ParseString("(1+2)*3", 9);
            Result = eval.ParseString("(1+2)*3+4", 13);
            Result = eval.ParseString("(1+2)*(3+4)", 21);
            Result = eval.ParseString("(1+2)*(3+4)+5", 26);
            Result = eval.ParseString("(1+2)*((3+4)+5)", 36);
            Result = eval.ParseString("9/3", 3);
            Result = eval.ParseString("9/3+1", 4);
            Result = eval.ParseString("9/(2+1)", 3);
            Result = eval.ParseString("-1", -1);
            Result = eval.ParseString("2-1", 1);
            Result = eval.ParseString("2--1", 3);
            Result = eval.ParseString("2- -1", 3);
            Result = eval.ParseString("2+-1", 1);
            Result = eval.ParseString("2++1", 3);
            Result = eval.ParseString("2+ +1", 3);
            Result = eval.ParseString("1==1", true);
            Result = eval.ParseString("1⩵1", true);
            Result = eval.ParseString("1==2", false);
            Result = eval.ParseString("1⩵2", false);
            Result = eval.ParseString("1!=2", true);
            Result = eval.ParseString("1≠2", true);
            Result = eval.ParseString("1≠1", false);
            Result = eval.ParseString("\"ABC\"", "ABC");
            Result = eval.ParseString("\"ABC\"+\"DEF\"", "ABCDEF");
            Result = eval.ParseString("\"ABC\"+123", "ABC123");
            Result = eval.ParseString("\"ABC\"==\"ABC\"", true);
            Result = eval.ParseString("\"ABC\"==\"DEF\"", false);
            Result = eval.ParseString("\"ABC\"!=\"DEFX\"", true);
            Result = eval.ParseString("INT(123)", 123);
            Result = eval.ParseString("INT(\"123\")", 0);   // Int of string
            Result = eval.ParseString("STR(123)", "123");
            Result = eval.ParseString("IF(1==1,\"123\",\"456\")", "123");
            Result = eval.ParseString("IF(1⩵1,\"123\",\"456\")", "123");
            Result = eval.ParseString("IF(1!=1,\"123\",\"456\")", "456");
            Result = eval.ParseString("IF(1≠1,\"123\",\"456\")", "456");
            Result = eval.ParseString("SUBSTRING(\"ABCDEF\",2,3)", "CDE");
            Result = eval.ParseString("SUBSTRING(\"ABCDEF\",2)", "CDEF");
            Result = eval.ParseString("SUM(123,\"456\",100)", 679);
            Result = eval.ParseString("VAR:=123", 123);
            Result = eval.ParseString("VAR≔123", 123);
            Result = eval.ParseString("VAR≔100", 100);
            Result = eval.ParseString("VAR", 100);
            Result = eval.ParseString("VAR*2", 200);
            Result = eval.ParseString("(VAR)*2", 200);
            Result = eval.ParseString("VAR:=(VAR+1)*2", 202);
            Result = eval.ParseString("VAR:=\"123\"", "123");
            Result = eval.ParseString("VAR≔\"1234\"", "1234");
            Result = eval.ParseString("VAR", "1234");

            Result = eval.ParseString("1=2", "");                       // Should fail
            Result = eval.ParseString("TEST()", "");
            Result = eval.ParseString("TEST(\"123.\")", "123.");

            eval.EvalStr = ""; // This is required because of default value limitations
            Result = eval.ParseString("", "");
            Result = eval.ParseString("\"\"", "");
            Result = eval.ParseString("(\"\")", "");

            Result = eval.ParseString("1.1", 1.1);
            Result = eval.ParseString("12.12", 12.12);
            Result = eval.ParseString("123.123", 123.123);
            Result = eval.ParseString("123.+.123", 123.123);
            Result = eval.ParseString("-123.456", -123.456);
            Result = eval.ParseString("(1.1)*2", 2.2);
            Result = eval.ParseString("Float(2)*2", 4.0);
            Result = eval.ParseString("Float(2)+1.1", 3.1);
            Result = eval.ParseString("2+1.1", 3);
            Result = eval.ParseString("PI:=3.14159265359", 3.14159265359);
            Result = eval.ParseString("INT(PI)", 3);
            Result = eval.ParseString("ROUND(PI,1)", 3.1);
            Result = eval.ParseString("ROUND(PI,2)", 3.14);
            Result = eval.ParseString("ROUND(PI,3)", 3.142);

            // Stop timing.
            stopwatch.Stop();

            var resultStr = "";
            string formattedLogs = eval.GetFormattedLogs();

            resultStr = resultStr + $"\nRequests: {eval.ProcessedCount} Errors: {eval.ErrorCount}";
            resultStr = resultStr + $"\nTime elapsed: {stopwatch.ElapsedMilliseconds} ms ({stopwatch.ElapsedTicks} ticks) ";
            resultStr = resultStr + $"\n";
            resultStr = resultStr + $"\n{formattedLogs}";

            resultStr = $"Results \n{resultStr}";
            //Console.Write(resultStr);

            return resultStr;

        }
    }
}
