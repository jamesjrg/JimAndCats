module Cats.Domain

open NodaTime

open System
open System.Collections.Generic

type State = int

type Command = int

type Event = int

let handleEvent (state : State) = function
    | x -> x

let handleCommand (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) command state =
    match command with
        | x -> [Event 5]

let handleCommandWithAutoGeneration command state = handleCommand Guid.NewGuid (fun () -> SystemClock.Instance.Now) command state