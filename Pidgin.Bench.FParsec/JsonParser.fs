module Pidgin.Bench.FParsec.JsonParser

open FParsec
open Pidgin.Examples.Json
open System.Collections.Immutable

let upcastJson (x : IJson) = x

let lbrace : Parser<char, unit> = pchar '{'
let rbrace : Parser<char, unit> = pchar '}'
let lbracket : Parser<char, unit> = pchar '['
let rbracket : Parser<char, unit> = pchar ']'
let quote : Parser<char, unit> = pchar '"'
let colon : Parser<char, unit> = pchar ':'
let colonWhitespace : Parser<char, unit> = between spaces spaces colon
let comma : Parser<char, unit> = pchar ','

let string : Parser<string, unit> = between quote quote (manySatisfy (fun c -> c <> '"'))
let jsonString = string |>> JsonString |>> upcastJson

let json, jsonImpl = createParserForwardedToRef()
let jsonArray = between lbracket rbracket (sepBy (between spaces spaces json) comma) |>> ImmutableArray.CreateRange |>> JsonArray |>> upcastJson
let jsonMember = string .>> colonWhitespace .>>. json
let jsonObject = between lbrace rbrace (sepBy (between spaces spaces jsonMember) comma) |>> dict |>> ImmutableDictionary.CreateRange |>> JsonObject |>> upcastJson
do jsonImpl := jsonString <|> jsonArray <|> jsonObject

let parse s = match run json s with
                  | Success(result, _, _) -> result
                  | Failure(msg, _, _) -> failwith msg
