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
let expectWithCreationFuncs (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) hashFunc (expected: Event list) (events, command) =    
    printList "Given" events
    printCommand "When" command
    printList "Expect" expected

    let actual = replay events |> handleCommand createGuid createTimestamp hashFunc command
    
    printList "Actual" actual
    actual =? expected