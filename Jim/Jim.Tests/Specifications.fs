module Jim.Tests.Specifications

open FsUnit.Xunit
open Jim.Domain

let printEvent event = event
let printCommand command = command

let printGiven events =
    printfn "Given"
    events 
    |> List.map printEvent
    |> List.iter (printfn "\t%A")
   
let printWhen command =
    printfn "When"
    command |> printCommand  |> printfn "\t%A"

let printExpect events =
    printfn "Expect"
    events 
    |> List.map printEvent
    |> List.iter (printfn "\t%A")

let inline replay events =
    let initial = (^S: (static member initial: ^S) ()) 
    let apply s = (^S: (static member apply: ^S -> (^E -> ^S)) s)
    List.fold apply initial events

let Given (events: Event list) = events
let When (command: Command) events = events, command
let Expect (expected: Event list) (events, command) =
    printGiven events
    printWhen command
    printExpect expected

    replay events
    |> handleCommand command
    |> should equal expected