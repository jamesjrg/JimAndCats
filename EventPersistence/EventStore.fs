namespace EventPersistence

open EventPersistence.EventStoreConnectionExtensions
open EventStore.ClientAPI
open Microsoft.FSharp.Reflection
open Nessos.FsPickler.Json
open System
open System.Net

type EventStore<'a>(streamId:string) =
    let jsonPickler = FsPickler.CreateJson(indent = false)

    let deserialize (event: ResolvedEvent) =
        jsonPickler.UnPickle<'a>(event.Event.Data)

    let serialize (event : 'a) =
        let case,_ = FSharpValue.GetUnionFields(event, typeof<'a>)
        let data = jsonPickler.Pickle event
        EventData(Guid.NewGuid(), case.Name, true, data, null)

    let create() = 
        async {
            let connection = EventStoreConnection.Create(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113))
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