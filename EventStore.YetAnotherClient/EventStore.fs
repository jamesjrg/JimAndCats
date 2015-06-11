namespace EventStore.YetAnotherClient

open EventStore.ClientAPI
open EventStore.YetAnotherClient.Serialization
open System
open System.Net

// This module implements AwaitTask for non generic Task
// It should be obsolete as of F# 4 when it will be implemented in FSharp.Core
[<AutoOpen>]
module AsyncExtensions =
    open System
    open System.Threading.Tasks
    type Microsoft.FSharp.Control.Async with
        static member Raise(ex) = Async.FromContinuations(fun (_,econt,_) -> econt ex)

        static member AwaitTask (t: Task) =
            let tcs = new TaskCompletionSource<unit>(TaskContinuationOptions.None)
            t.ContinueWith((fun _ -> 
                if t.IsFaulted then tcs.SetException t.Exception
                elif t.IsCanceled then tcs.SetCanceled()
                else tcs.SetResult(())), TaskContinuationOptions.ExecuteSynchronously) |> ignore
            async {
                try
                    do! Async.AwaitTask tcs.Task
                with
                | :? AggregateException as ex -> 
                    do! Async.Raise (ex.Flatten().InnerExceptions |> Seq.head) }

    type IEventStoreConnection with
        member this.AsyncConnect() = Async.AwaitTask(this.ConnectAsync())
        member this.AsyncReadStreamEventsForward stream start count resolveLinkTos =
            Async.AwaitTask(this.ReadStreamEventsForwardAsync(stream, start, count, resolveLinkTos))
        member this.AsyncAppendToStream stream expectedVersion events =
            Async.AwaitTask(this.AppendToStreamAsync(stream, expectedVersion, events))

type EventStore<'a>(ipAddress, port) =
    let deserialize (event: ResolvedEvent) =
        deserializeUnion event.Event.EventType event.Event.Data 

    let serialize (event: 'a) =
        let case, data = serializeUnion event
        EventData(Guid.NewGuid(), case, true, data, null)

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
                    |> Seq.choose deserialize
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
                match deserialize rawEvent with
                | Some event -> handleEvent event
                | None -> () (* TODO this should be logged *)

            connection.SubscribeToStreamFrom(streamId,
                new Nullable<int>(lastCheckpoint),
                false,
                new System.Action<_,_>(handleRawEvent),
                null,
                null, //TODO handle subscription being dropped
                null) |> ignore