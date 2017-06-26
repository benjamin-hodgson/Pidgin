module Pidgin.Bench.FParsec.ExpressionParser

open FParsec


let infixLOpp = new OperatorPrecedenceParser<int, unit, unit>()
infixLOpp.TermParser <- pint32
infixLOpp.AddOperator(InfixOperator("+", spaces, 1, Associativity.Left, fun x y -> x + y))

let parseL s = match run infixLOpp.ExpressionParser s with
                  | Success(result, _, _) -> result
                  | Failure(msg, _, _) -> failwith msg

let infixROpp = new OperatorPrecedenceParser<int, unit, unit>()
infixROpp.TermParser <- pint32
infixROpp.AddOperator(InfixOperator("+", spaces, 1, Associativity.Right, fun x y -> x + y))

let parseR s = match run infixROpp.ExpressionParser s with
                  | Success(result, _, _) -> result
                  | Failure(msg, _, _) -> failwith msg