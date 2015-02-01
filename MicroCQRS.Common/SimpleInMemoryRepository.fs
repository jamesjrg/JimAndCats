namespace MicroCQRS.Common

open MicroCQRS.Common
open System
open System.Collections.Generic

(* An in-memory repository implementation. This might be persisted to disk using some sort of database in a large system *)

type SimpleInMemoryRepository<'TAggregate>() =
    let state = new Dictionary<Guid, 'TAggregate>()

    member this.Load<'TEvent>(store:IEventStore<'TEvent>, streamId, handleEvent) =
            let rec fold version =
                async {
                let! events, lastEvent, nextEvent = 
                    store.ReadStream streamId version 500

                List.iter (handleEvent this) events
                match nextEvent with
                | None -> return lastEvent
                | Some n -> return! fold n }
            fold 0

    interface ISimpleRepository<'TAggregate> with
        member this.List() = state.Values :> 'TAggregate seq

        member this.Get (id:Guid) =
            match state.TryGetValue(id) with
            | true, x -> Some x
            | false, _ -> None

        member this.Put id (x:'TAggregate) =
            state.[id] <- x
        
