namespace EventStore.YetAnotherClient

open System

type GenericRepository (store:IEventStore<'TEvent>, apply:'TAggregate -> 'TEvent-> 'TAggregate) =

    member this.List() =
        async {
            return state.Values :> 'TAggregate seq
        }

    member this.Get (id:Guid) =
        async {
            let aggregate = store.ReadStream id 

            match state.TryGetValue(id) with
            | true, x -> return Some x
            | false, _ -> return None
        }

    member this.Put id (x:'TAggregate) =
        async {
            state.[id] <- x
        }
        
