namespace EventStore.YetAnotherClient

open EventStore.ClientAPI
open System

module Repository =        
    let private makeStreamId streamPrefix (aggregateId:Guid) = sprintf "%s-%O" streamPrefix (aggregateId.ToString("N"))

    let getAggregate (store:IEventStore<'TEvent>) (apply:'TAggregate -> 'TEvent-> 'TAggregate) (invalidState:'TAggregate) (streamPrefix:string) (aggregateId:Guid) = 
        let streamId = makeStreamId streamPrefix aggregateId

        //TODO error handling
        let rec fold aggregate version =
            async {
                let! events, lastEvent, nextEvent = store.ReadStream streamId version 500
                let aggregate = List.fold apply aggregate events
                match nextEvent with
                | None -> return aggregate, lastEvent
                | Some n -> return! fold aggregate n
            }
        
        async {           
            let! result = fold invalidState 0
            return if fst result = invalidState then None, snd result else Some (fst result), snd result
        }

    //TODO error handling
    let saveEvents (store:IEventStore<'TEvent>) (streamPrefix:string) aggregateId expectedVersion (events:'TEvent list) =
        store.AppendToStream (makeStreamId streamPrefix aggregateId) expectedVersion events

    let saveEvent (store:IEventStore<'TEvent>) (streamPrefix:string) aggregateId expectedVersion (event:'TEvent) =
        saveEvents store streamPrefix aggregateId expectedVersion [event]

    let saveEventToNewStream (store:IEventStore<'TEvent>) (streamPrefix:string) aggregateId (event:'TEvent) =
        saveEvent store streamPrefix aggregateId ExpectedVersion.NoStream event
        
