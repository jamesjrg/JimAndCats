module Jim.Tests.Specifications

open Jim.Domain
open NodaTime
open System

open Swensen.Unquote.Assertions

let printList label stuff =
    printfn label
    stuff |> List.iter (printfn "\t%A")

let printCommand label command =
    printfn label
    command |> printfn "\t%A"

let inline replay events =
    List.fold handleEvent (new State()) events

let Given (events: Event list) = events
let When (command: Command) events = events, command
let expectWithCreationFuncs (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) hashFunc (expected: Result<Event list>) (events, command) =    
    printList "Given" events
    printCommand "When" command

    let actual = replay events |> handleCommand createGuid createTimestamp hashFunc command

    match expected, actual with
    | Failure e, Failure a -> a =? a //not concerned about the precise error message
    | _ -> expected =? actual