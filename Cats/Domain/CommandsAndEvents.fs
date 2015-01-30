module Cats.Domain.CommandsAndEvents

open Cats.Shared.ErrorHandling
open Cats.Domain.ICatRepository

open NodaTime
open System
open System.Collections.Generic

type State = int

type Command =
    | SomethingCommand of SomethingCommand

and SomethingCommand = {
    Something: int
   }

type Event =
    | SomethingEvent of SomethingEvent

and SomethingEvent = {
    Something: int
   }

let handleEvent (repository : ICatRepository) = function
    | x -> ()

let handleCommand (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) command state =
    match command with
        | x -> Success (SomethingEvent {Something=5})

let handleCommandWithAutoGeneration command state = handleCommand Guid.NewGuid (fun () -> SystemClock.Instance.Now) command state