namespace EventStore.YetAnotherClient

open System

module Repository =        
    let private makeStreamId streamPrefix (aggregateId:Guid) = sprintf "%s-%O" streamPrefix (aggregateId.ToString("N"))

    let getAggregate (store:IEventStore<'TEvent>) (apply:'TAggregate -> 'TEvent-> 'TAggregate) (streamPrefix:string) (aggregateId:Guid) = 
        let streamId = makeStreamId streamPrefix aggregateId

        let rec fold version =
            async {
                let! events, lastEvent, nextEvent = store.ReadStream streamId version 500

                List.iter (fun x -> apply aggregate x) events
                match nextEvent with
                | None -> return lastEvent
                | Some n -> return! fold n
            }
        
        async {
            let! streamExists = store.StreamExists streamId            
            if streamExists then fold 0 else None
        }

    let save (store:IEventStore<'TEvent>) (streamPrefix:string) aggregateId expectedVersion (event:'TEvent) =
        store.AppendToStream (makeStreamId streamPrefix aggregateId) expectedVersion [event]
        
