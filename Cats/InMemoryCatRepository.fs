namespace Cats.InMemoryCatRepository

open MicroCQRS.Common
open Cats.Domain.CatAggregate
open Cats.Domain.ICatRepository
open Cats.Domain.CommandsAndEvents
open System
open System.Collections.Generic

(* An in-memory repository implementation. This might be a SQL database in a large system *)

type State = Dictionary<Guid, Cat>

type InMemoryCatRepository() =
    let state = new State()

    member this.Load(store:IEventStore<Event>, streamId) =
            let rec fold version =
                async {
                let! events, lastEvent, nextEvent = 
                    store.ReadStream streamId version 500

                List.iter (handleEvent this) events
                match nextEvent with
                | None -> return lastEvent
                | Some n -> return! fold n }
            fold 0

    interface ICatRepository with
        member this.List() = state.Values :> Cat seq

        member this.Get (id:Guid) =
            match state.TryGetValue(id) with
            | true, cat -> Some cat
            | false, _ -> None

        member this.Put (cat:Cat) =
            state.[cat.Id] <- cat
        