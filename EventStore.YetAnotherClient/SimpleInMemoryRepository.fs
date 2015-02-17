namespace EventStore.YetAnotherClient

open System
open System.Collections.Generic

type SimpleInMemoryRepository<'TAggregate>() =
    let state = new Dictionary<Guid, 'TAggregate>()

    interface ISimpleRepository<'TAggregate> with
        member this.List() = state.Values :> 'TAggregate seq

        member this.Get (id:Guid) =
            match state.TryGetValue(id) with
            | true, x -> Some x
            | false, _ -> None

        member this.Put id (x:'TAggregate) =
            state.[id] <- x
        
