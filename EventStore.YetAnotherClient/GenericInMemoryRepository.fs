namespace EventStore.YetAnotherClient

open System
open System.Collections.Generic

type GenericInMemoryRepository<'TAggregate>() =
    let state = new Dictionary<Guid, 'TAggregate>()

    interface IGenericRepository<'TAggregate> with
        member this.List() =
            async {
                return state.Values :> 'TAggregate seq
            }

        member this.Get (id:Guid) =
            async {
                match state.TryGetValue(id) with
                | true, x -> return Some x
                | false, _ -> return None
            }

        member this.Put id (x:'TAggregate) =
            async { 
                state.[id] <- x
            }
        
