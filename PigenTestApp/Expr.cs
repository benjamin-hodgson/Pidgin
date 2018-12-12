using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace ApplicationSupport.Parsers
{
    public interface IExpr : IEquatable<IExpr>
    {
    }

    public class Identifier : IExpr
    {
        public string Name { get; }

        public Identifier(string name)
        {
            Name = name;
        }

        public bool Equals(IExpr other)
            => other is Identifier i && this.Name == i.Name;
    }

    public class Literal : IExpr
    {
        //public int Value { get; }
        public object Value { get; }

        //public Literal(int value)
        public Literal(object value)
        {
            Value = value;
        }

        public bool Equals(IExpr other)
            => other is Literal l && this.Value == l.Value;
    }

    public class Call : IExpr
    {
        public IExpr Expr { get; }
        public ImmutableArray<IExpr> Arguments { get; }

        public Call(IExpr expr, ImmutableArray<IExpr> arguments)
        {
            Expr = expr;
            Arguments = arguments;
        }

        public bool Equals(IExpr other)
            => other is Call c
            && this.Expr.Equals(c.Expr)
            && this.Arguments.SequenceEqual(c.Arguments);
    }

    public enum UnaryOperatorType
    {
        Complement,
        Neg,
        UPlus,
        //Increment,        // TBD ++
        //Decrement         // TBD --
    }
    public class UnaryOp : IExpr
    {
        public UnaryOperatorType Type { get; }
        public IExpr Expr { get; }

        public UnaryOp(UnaryOperatorType type, IExpr expr)
        {
            Type = type;
            Expr = expr;
        }

        public bool Equals(IExpr other)
            => other is UnaryOp u
            && this.Type == u.Type
            && this.Expr.Equals(u.Expr);
    }

    public enum BinaryOperatorType
    {
        Plus,
        Minus,
        Multiply,
        Divide,
        EqualTo,
        NotEqualTo,
        AssignTo                    // Assign value to, Variables
    }
    public class BinaryOp : IExpr
    {
        public BinaryOperatorType Type { get; }
        public IExpr Left { get; }
        public IExpr Right { get; }

        public BinaryOp(BinaryOperatorType type, IExpr left, IExpr right)
        {
            Type = type;
            Left = left;
            Right = right;
        }

        public bool Equals(IExpr other)
            => other is BinaryOp b
            && this.Type == b.Type
            && this.Left.Equals(b.Left)
            && this.Right.Equals(b.Right);
    }

    // ------------------------------------------------------------------------
    // Main EVAL class
    // ------------------------------------------------------------------------
    public class Eval
    {
        string _evalString;
        //int _result;
        object _result;

        public object Result => _result;
        public object ResultExpected { get; set; }
        public bool EnableLog { get; set; }

        // Variables (dictionary)
        public Dictionary<string, object> Variables { get; set; }

        // Event callback to process external functions
        public delegate object CallbackEventHandler(string Funct, object[] args);
        public event CallbackEventHandler ExtFunctionParser;

        // Logs
        public class LogEntry
        {
            public string EvalString;
            //public int Result;
            public object Result;
            public object ResultExpected;
        }
        public List<LogEntry> Logs { get; set; }

        // Log statistics
        public int ErrorCount { get; set; }
        public int ProcessedCount { get; set; }

        public string GetFormattedLogs()
        {
            StringBuilder sb = new StringBuilder();
            ErrorCount = 0; ProcessedCount = 0;
            foreach (LogEntry logEntry in Logs)
            {
                ProcessedCount++;
                string failStr = "";
                if (logEntry.ResultExpected != null)
                {
                    if (NullToString(logEntry.ResultExpected) == NullToString(logEntry.Result))
                    {
                        failStr = " ... ok";
                    }
                    else
                    {
                        failStr = $" ... failed [{logEntry.ResultExpected}]";
                        ErrorCount++;
                    }
                }
                sb.Append($"'{logEntry.EvalString}' = {logEntry.Result}{failStr} \n");
            }
            return sb.ToString();
        }

        public string EvalStr
        {
            get
            {
                return _evalString;
            }
            set
            {
                _evalString = value;
                ParseString();
            }
        }

        // Constructor
        public Eval() { }
        public Eval(string input)
        {
            EvalStr = input;
        }

        // Parse the eval string
        public object ParseString(string evalStr = "", object expectedResult = null)
        {
            _result = null;
            ResultExpected = expectedResult;

            if (!string.IsNullOrEmpty(evalStr))
            {
                _evalString = evalStr;
            }
            if (!string.IsNullOrEmpty(_evalString))
            {
                // Parse the string
                var resultExpr = ExprParser.ParseOrThrow(_evalString);

                // Evaluate the results
                Literal resultA = (Literal)Accumulator(resultExpr);
                _result = resultA.Value;
                //if (resultA.Value != null){
                //    Int32.TryParse(resultA.Value.ToString(),out _result); }
            }

            // Add a log entry as required
            if (EnableLog)
            {
                if (Logs == null)
                {
                    Logs = new List<LogEntry>();
                }
                LogEntry logEntry = new LogEntry
                {
                    EvalString = _evalString,
                    Result = _result,
                    ResultExpected = ResultExpected
                };
                Logs.Add(logEntry);
            }

            // Complete
            return _result;
        }

        // Accumulate results
        private IExpr Accumulator(IExpr ResultExpr)
        {
            object newValue = null;
            string varName = "";

            if (ResultExpr is Literal)
            {
                // Single literal (1)
                return ResultExpr;
            }

            else if (ResultExpr is BinaryOp)
            {
                // Binary operation
                // (two values)
                newValue = ProcessBinaryOperation(ResultExpr, ref varName);
            }

            else if (ResultExpr is UnaryOp)
            {
                // Unary operation
                // (one value)
                newValue = ProcessUnaryOperation(ResultExpr, ref varName);
            }

            else if (ResultExpr is Call)
            {
                // Function Call
                // <function>([<arg>, ... <argn>])
                newValue = ProcessFunctionCall(ResultExpr, ref varName);
            }

            else if (ResultExpr is Identifier)
            {
                // Identifier (variable/constant -- TBD)
                // Load variable
                if (Variables != null)
                {
                    // Get new value
                    var idObj = (Identifier)ResultExpr;
                    string varNameA = idObj.Name;
                    Variables.TryGetValue(varNameA, out newValue);
                }
                else
                {
                    // Indicate variables not supported
                    //newValue = null;
                }

            }

            else
            {
                // Unsupported type 
                // (should never happen)
                //newValue = null;
            }

            // Update variable if original left was a variable
            if (!string.IsNullOrEmpty(varName))
            {
                // Update variable, return true
                if (Variables != null)
                {
                    Variables[varName] = newValue;
                    //newValue = true;
                }
                else
                {
                    // Indicate variables not supported
                    //newValue = false;
                }
            }

            // Complete
            return new Literal(newValue);
        }

        private object ProcessBinaryOperation(IExpr ResultExpr, ref string varName)
        {
            // Binary operation
            // (two values)
            object newValue = null;
            var op = (BinaryOp)ResultExpr;

            // Save variable name if the left is an identifier
            if (op.Left is Identifier && op.Type == BinaryOperatorType.AssignTo)
            {
                var idObj = (Identifier)op.Left;
                varName = idObj.Name;
            }

            // Process both sides of the expression
            if (!(op.Left is Literal))
            {
                // Process left operator
                op = new BinaryOp(op.Type, Accumulator(op.Left), op.Right);
            }
            if (!(op.Right is Literal))
            {
                // Process right operator
                op = new BinaryOp(op.Type, op.Left, Accumulator(op.Right));
            }

            // Process the operator
            if (op.Left is Literal && op.Right is Literal)
            {
                // Determine operation type
                var opl = (Literal)op.Left;
                var opr = (Literal)op.Right;

                // Assign?
                if (op.Type == BinaryOperatorType.AssignTo)
                {
                    // Assign right value -> variable
                    newValue = opr.Value;
                }

                //else if (oplValTypeString && oprValTypeString)
                else if (IsExpressionString(opl.Value))
                {
                    // Perform math as strings
                    newValue = "";
                    string l = ConvertExpressionToString(opl.Value);
                    string r = ConvertExpressionToString(opr.Value);

                    switch (op.Type)
                    {
                        case BinaryOperatorType.Plus:
                            newValue = l + r;
                            break;

                        case BinaryOperatorType.EqualTo:
                            //newValue = (l == r) ? 1 : 0;
                            newValue = (l == r);
                            break;

                        case BinaryOperatorType.NotEqualTo:
                            //newValue = (l != r) ? 1 : 0;
                            newValue = (l != r);
                            break;

                    }
                }

                else if (IsExpressionInt(opl.Value))
                {
                    // Perform math as numberic 64 bit integers
                    // (nulls are treated like zeros)
                    newValue = 0;
                    Int64 l = ConvertExpressionToInt(opl.Value);
                    Int64 r = ConvertExpressionToInt(opr.Value);

                    switch (op.Type)
                    {
                        case BinaryOperatorType.Plus:
                            newValue = l + r;
                            break;

                        case BinaryOperatorType.Minus:
                            newValue = l - r;
                            break;

                        case BinaryOperatorType.Multiply:
                            newValue = l * r;
                            break;

                        case BinaryOperatorType.Divide:
                            newValue = l / r;
                            break;

                        case BinaryOperatorType.EqualTo:
                            //newValue = (l == r) ? 1 : 0;
                            newValue = (l == r);
                            break;

                        case BinaryOperatorType.NotEqualTo:
                            //newValue = (l != r) ? 1 : 0;
                            newValue = (l != r);
                            break;

                    }
                }

                else if (IsExpressionDouble(opl.Value))
                {
                    // Perform math as numeric doubles
                    // (nulls are treated like zeros)
                    newValue = 0.0;
                    double l = ConvertExpressionToDouble(opl.Value);
                    double r = ConvertExpressionToDouble(opr.Value);

                    switch (op.Type)
                    {
                        case BinaryOperatorType.Plus:
                            newValue = l + r;
                            break;

                        case BinaryOperatorType.Minus:
                            newValue = l - r;
                            break;

                        case BinaryOperatorType.Multiply:
                            newValue = l * r;
                            break;

                        case BinaryOperatorType.Divide:
                            newValue = l / r;
                            break;

                        case BinaryOperatorType.EqualTo:
                            //newValue = (l == r) ? 1 : 0;
                            newValue = (l == r);
                            break;

                        case BinaryOperatorType.NotEqualTo:
                            //newValue = (l != r) ? 1 : 0;
                            newValue = (l != r);
                            break;

                    }
                }

                else
                {
                    // Unknown left type
                    // (only supported operation is assignment)
                }
            }

            else
            {
                // Not sure why we would ever get here
                newValue = false;
            }

            // Complete
            return newValue;
        }

        private object ProcessUnaryOperation(IExpr ResultExpr, ref string varName)
        {
            // Unary operation
            // (one value)
            object newValue = null;
            var op = (UnaryOp)ResultExpr;

            if (!(op.Expr is Literal))
            {
                // Process left operator
                op = new UnaryOp(op.Type, Accumulator(op.Expr));
            }

            if (op.Expr is Literal)
            {
                var ope = (Literal)op.Expr;
                object value = ope.Value;
                
                if (IsExpressionInt(value))
                {
                    // Perform math as integer
                    Int64 u = ConvertExpressionToInt(value);

                    switch (op.Type)
                    {
                        case UnaryOperatorType.Complement:
                            newValue = ~u;
                            break;

                        case UnaryOperatorType.Neg:
                            newValue = -u;
                            break;

                        case UnaryOperatorType.UPlus:
                            newValue = u;
                            break;

                        default:
                            break;
                    }
                }
                else if (IsExpressionDouble(value))
                {
                    // Perform math as double
                    double u = ConvertExpressionToDouble(value);

                    switch (op.Type)
                    {
                        // Not suuported for doubles
                        //case UnaryOperatorType.Complement:
                        //    newValue = ~u;
                        //    break;

                        case UnaryOperatorType.Neg:
                            newValue = -u;
                            break;

                        case UnaryOperatorType.UPlus:
                            newValue = u;
                            break;

                        default:
                            break;
                    }
                }
                else
                {
                    // Unsupported unary operand type
                }
            }
            else
            {
                // Not sure why we would ever get here
            }

            // Complete
            return newValue;
        }

        private object ProcessFunctionCall(IExpr ResultExpr, ref string varName)
        {
            // Function Call
            var cmd = (Call)ResultExpr;
            var id = (Identifier)cmd.Expr;

            // Create an object array for the arguments
            // Make sure there are always at least 2
            int argsL = cmd.Arguments.Length;
            if (argsL < 2) { argsL = 2; }
            object[] args = new object[argsL];

            int i = 0;
            foreach (IExpr expr in cmd.Arguments)
            {
                if (!(expr is Literal))
                {
                    // Process operator
                    var exprN = (Literal)Accumulator(expr);
                    args[i] = exprN.Value;
                }
                else
                {
                    // Use current literal
                    var exprN = (Literal)expr;
                    args[i] = exprN.Value;
                }
                i++;
            }

            // Process the function request
            return ProcessFunctionCall2(id.Name, args);
        }

        private object ProcessFunctionCall2(string Funct, object[] args)
        {
            object newValue = null;

            // External function processing (if specified)
            if (ExtFunctionParser != null)
                newValue = ExtFunctionParser(Funct, args);

            if (newValue == null)
            {
                // Built in function processing
                int i1 = 0; int i2 = 0;
                switch (Funct.ToLower())
                {
                    case "float":
                        newValue = ConvertExpressionToDouble(args[0]);
                        break;

                    case "int":
                        newValue = ConvertExpressionToInt(args[0]);
                        break;

                    case "if":
                        bool Condition = Convert.ToBoolean(args[0]);
                        if (Condition)
                        {
                            newValue = args[1];
                        }
                        else
                        {
                            if (args.Length > 2)
                            {
                                newValue = args[2];
                            }
                        }
                        break;

                    case "round":
                        double d1 = ConvertExpressionToDouble(args[0]);
                        i1 = Convert.ToInt32(args[1]);
                        newValue = Math.Round(d1, i1);
                        break;

                    case "str":
                        newValue = ConvertExpressionToString(args[0]);
                        break;

                    case "substring":
                        string str = ConvertExpressionToString(args[0]);

                        i1 = Convert.ToInt32(args[1]);
                        if (i1 < 0) { i1 = 0; }
                        //if(i1 > str.Length) {i1 = str.Length;}

                        if (args.Length > 2)
                        {
                            i2 = Convert.ToInt32(args[2]);
                            if (i2 < 0) { i2 = 0; }
                        }

                        // Return substring
                        if (i2 > 0)
                        {
                            newValue = str.Substring(i1, i2);
                        }
                        else
                        {
                            newValue = str.Substring(i1);
                        }
                        break;

                    case "sum":
                        foreach (object arg in args)
                        {
                            i1 = i1 + Convert.ToInt32(arg);
                        }
                        newValue = i1;
                        break;

                    case "test":
                        //newValue = Convert.ToString(null);
                        //newValue = ConvertExpressionToDouble(args[0]);
                        newValue = args[0];
                        break;
                }
            }

            // Complete
            return newValue;
        }

        private double ConvertExpressionToDouble(object Value)
        {
            if (Value == null)
            {
                return 0.0;
            }

            TypeCode typeCode = Type.GetTypeCode(Value.GetType());
            switch (typeCode)
            {
                case TypeCode.Double:
                    return (double)Value;

                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return Convert.ToDouble(Value);

                case TypeCode.String:
                    //Int32.TryParse(ValueToDouble(), out r);
                    return Convert.ToDouble(Value);
                    //return 0.0;

                //case TypeCode.DBNull:

                default:
                    return 0.0;
            }

        }

        private Int64 ConvertExpressionToInt(object Value)
        {
            if (Value == null)
            {
                return 0;
            }

            TypeCode typeCode = Type.GetTypeCode(Value.GetType());
            switch (typeCode)
            {
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                    return Convert.ToInt64(Value);

                case TypeCode.Int64:
                    return (Int64)Value;

                case TypeCode.String:
                    //Int32.TryParse(ValueToDouble(), out r);
                    return 0;

                //case TypeCode.DBNull:

                default:
                    return 0;
            }

        }

        private string ConvertExpressionToString(object Value)
        {
            if (Value == null)
            {
                return "";
            }

            TypeCode typeCode = Type.GetTypeCode(Value.GetType());
            switch (typeCode)
            {
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return Convert.ToString(Value);

                case TypeCode.String:
                    //Int32.TryParse(ValueToDouble(), out r);
                    return (string)Value;

                //case TypeCode.DBNull:

                default:
                    return "";
            }

        }

        private bool IsExpressionInt(object Value)
        {
            if (Value == null)
            {
                return false;
            }

            TypeCode typeCode = Type.GetTypeCode(Value.GetType());
            switch (typeCode)
            {
                //case TypeCode.Double:

                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return true;

                default:
                    return false;

            }
        }

        private bool IsExpressionDouble(object Value)
        {
            if (Value == null)
            {
                return false;
            }

            TypeCode typeCode = Type.GetTypeCode(Value.GetType());
            switch (typeCode)
            {
                case TypeCode.Double:
                    return true;

                //case TypeCode.Int16:
                //case TypeCode.Int32:
                //case TypeCode.Int64:
                //    return true;

                default:
                    return false;
            }
        }

        private bool IsExpressionString(object Value)
        {
            if (Value == null)
            {
                return false;
            }

            TypeCode typeCode = Type.GetTypeCode(Value.GetType());
            switch (typeCode)
            {
                //case TypeCode.Double:
                //case TypeCode.Int16:
                //case TypeCode.Int32:
                //case TypeCode.Int64:

                case TypeCode.String:
                    return true;

                default:
                    return false;

            }
        }

        private static string NullToString(object Value)
        {
            return Value == null ? "" : Value.ToString();
        }

    }
}