module Jim.Tests.Specifications

open Jim.Domain
open Jim.ErrorHandling
open Jim.UserRepository
open NodaTime
open System

open Swensen.Unquote.Assertions

let inline replay events =
    List.fold handleEvent (new State()) events

let Given (events: Event list) = events
let When (command: Command) events = events, command
let expectWithCreationFuncs (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) hashFunc (expected: Result<Event list, string>) (events, command) =    
    let actual = replay events |> handleCommand createGuid createTimestamp hashFunc command

    match expected, actual with
    | Failure e, Failure a -> a =? a //not concerned about the precise error message
    | _ -> expected =? actual