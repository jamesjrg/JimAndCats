module Cats.Domain.CommandsAndEvents

open Cats.Shared.ErrorHandling
open Cats.Domain.ICatRepository

open NodaTime
open System
open System.Collections.Generic

type State = int

type Command =
    | CreateCat of CreateCat

and CreateCat = {
    Something: int
   }

type Event =
    | CatCreated of CatCreated

and CatCreated = {
    Id: Guid
    CreationTime: Instant
}

let handleEvent (repository : ICatRepository) = function
    | x -> ()

let createCat (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) =
    Success (CatCreated {
            Id = createGuid()
            CreationTime = createTimestamp()
        })

let handleCommand (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) command repository =
    match command with
        | x -> createCat createGuid createTimestamp

let handleCommandWithAutoGeneration command repository =
    handleCommand Guid.NewGuid (fun () -> SystemClock.Instance.Now) command repository