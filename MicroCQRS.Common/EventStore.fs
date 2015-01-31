namespace MicroCQRS.Common

open MicroCQRS.Common.EventStoreConnectionExtensions
open EventStore.ClientAPI
open Microsoft.FSharp.Reflection
open Nessos.FsPickler.Json
open System
open System.Net

type EventStore<'a>(ipAddress, port) =
    let jsonPickler = FsPickler.CreateJson(indent = false)

    let deserialize (event: ResolvedEvent) =
        jsonPickler.UnPickle<'a>(event.Event.Data)

    let serialize (event : 'a) =
        let case,_ = FSharpValue.GetUnionFields(event, typeof<'a>)
        let data = jsonPickler.Pickle event
        EventData(Guid.NewGuid(), case.Name, true, data, null)

    let create() = 
        async {
            let connection = EventStoreConnection.Create(new IPEndPoint(IPAddress.Parse(ipAddress), port))
            do! Async.AwaitTask ( connection.ConnectAsync() )
            return connection
        }
        
    let connection = create() |> Async.RunSynchronously

    interface IEventStore<'a> with
    
        member this.ReadStream streamId version count = 
            async {
                let! slice = connection.AsyncReadStreamEventsForward streamId version count true

                let events = 
                    slice.Events 
                    |> Seq.map deserialize
                    |> Seq.toList
        
                let nextEventNumber = 
                    if slice.IsEndOfStream 
                    then None 
                    else Some slice.NextEventNumber

                return events, slice.LastEventNumber, nextEventNumber
                }

        member this.AppendToStream streamId expectedVersion newEvents = 
            async {
                let serializedEvents = [| for event in newEvents -> serialize event |]

                do! Async.Ignore <| connection.AsyncAppendToStream streamId expectedVersion serializedEvents }

        member this.SubscribeToStreamFrom
            streamId
            (lastCheckpoint : int)
            (handleEvent: 'a -> unit) =
        
            let handleRawEvent (subscription:EventStoreCatchUpSubscription) rawEvent =
                handleEvent <| deserialize rawEvent

            connection.SubscribeToStreamFrom(streamId,
                new Nullable<int>(lastCheckpoint),
                false,
                new System.Action<_,_>(handleRawEvent),
                null,
                null, //TODO handle subscription being dropped
                null) |> ignore