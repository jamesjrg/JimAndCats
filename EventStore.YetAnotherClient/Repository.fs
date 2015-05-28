namespace EventStore.YetAnotherClient

open EventStore.ClientAPI
open System

module Repository =        
    let private makeStreamId streamPrefix (aggregateId:Guid) = sprintf "%s-%O" streamPrefix (aggregateId.ToString("N"))

    let getAggregate (store:IEventStore<'TEvent>) (apply:'TAggregate option -> 'TEvent-> 'TAggregate option) (streamPrefix:string) (aggregateId:Guid) = 
        let streamId = makeStreamId streamPrefix aggregateId

        let rec fold aggregate version =
            async {
                let! events, lastEvent, nextEvent = store.ReadStream streamId version 500

                let aggregate = List.fold apply aggregate events
                match nextEvent with
                | None -> return aggregate, lastEvent
                | Some n -> return! fold aggregate n
            }
        
        async {
            let! streamExists = store.StreamExists streamId            
            if streamExists then return! fold None 0 else return None, ExpectedVersion.NoStream
        }

    let saveEvent (store:IEventStore<'TEvent>) (streamPrefix:string) aggregateId expectedVersion (event:'TEvent) =
        store.AppendToStream (makeStreamId streamPrefix aggregateId) expectedVersion [event]
        
