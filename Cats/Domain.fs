module Cats.Domain

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

let handleEvent (state : State) = function
    | x -> 10

let handleCommand (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) command state =
    match command with
        | x -> [SomethingEvent {Something=5}]

let handleCommandWithAutoGeneration command state = handleCommand Guid.NewGuid (fun () -> SystemClock.Instance.Now) command state