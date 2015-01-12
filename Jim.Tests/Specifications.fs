module Jim.Tests.Specifications

open FsUnit.Xunit
open Jim.Domain
open NodaTime
open System

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
let Expect (expected: Event list) (mapActualWithoutGenerated: Event -> Event) (events, command) =
    printList "Given" events
    printCommand "When" command
    printList "Expect" expected

    let actual = replay events |> handleCommand command |> List.map mapActualWithoutGenerated
    
    printList "Actual" actual
    actual |> should equal expected

