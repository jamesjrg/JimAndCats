module Cats.Tests.Domain.Specifications

open Cats.Shared.ErrorHandling
open Cats.Domain.CommandsAndEvents
open Cats.InMemoryCatRepository
open NodaTime
open System

open Swensen.Unquote.Assertions

let inline replay events repository =
    List.iter (handleEvent repository) events

let Given (events: Event list) = events
let When (command: Command) events = events, command
let expectWithCreationFuncs (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) (expected: Result<Event, string>) (events, command) =  
    let repository = new InMemoryCatRepository()  
    replay events repository
    let actual = handleCommand createGuid createTimestamp command repository

    match expected, actual with
    | Failure e, Failure a -> a =? a //not concerned about the precise error message
    | _ -> expected =? actual