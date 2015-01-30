module Jim.Tests.Domain.Specifications

open Jim.Shared.ErrorHandling
open Jim.Domain.CommandsAndEvents
open Jim.InMemoryUserRepository
open NodaTime
open System

open Swensen.Unquote.Assertions

let inline replay events repository =
    List.iter (handleEvent repository) events

let Given (events: Event list) = events
let When (command: Command) events = events, command
let expectWithCreationFuncs (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) hashFunc (expected: Result<Event, CommandFailure>) (events, command) =  
    let repository = new InMemoryUserRepository()  
    replay events repository
    let actual = handleCommand createGuid createTimestamp hashFunc command repository

    match expected, actual with
    | Failure (BadRequest e), Failure (BadRequest a) -> a =? a //not concerned about the precise error message
    | Failure NotFound, Failure NotFound -> Failure NotFound =? Failure NotFound
    | _ -> expected =? actual