(*
Based on the client FsUno.Prod by Jérémie Chassaing, except:
- event type is a generic rather than a specific domain type
- using FsPickler for serialization rather than custom code
- only subscribes to a single stream, not all streams
*)

module EventPersistence.EventStore

open EventStore.ClientAPI
open Microsoft.FSharp.Reflection
open Nessos.FsPickler.Json
open System
open System.Net

// This module implements AwaitTask for non generic Task
// It should be obsolete as of F# 4 when it will be implemented in FSharp.Core
[<AutoOpen>]
module AsyncExtensions =
    open System
    open System.Threading
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

let jsonPickler = FsPickler.CreateJson(indent = false)

let deserialize<'a> (event: ResolvedEvent) =
    jsonPickler.UnPickle<'a>(event.Event.Data)

let serialize (event : 'a) =
    let case,_ = FSharpValue.GetUnionFields(event, typeof<'a>)
    let data = jsonPickler.Pickle event
    EventData(Guid.NewGuid(), case.Name, true, data, null)

let create() = 
    async {
        let s = EventStoreConnection.Create(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113))
        do! Async.AwaitTask ( s.ConnectAsync() )
        return s }

let subscribe (streamId: string) (projection: 'a -> unit) (getStore: Async<IEventStoreConnection>) =
    async {
    let! store = getStore
    let credential = SystemData.UserCredentials("admin", "changeit")
    do! Async.AwaitTask
        <| store.SubscribeToStreamAsync(streamId, true, (fun s e -> deserialize e |> projection), userCredentials = credential) |> Async.Ignore
    return store }
    |> Async.RunSynchronously

let readStream (store: IEventStoreConnection) streamId version count = 
    async {
        let! slice = store.AsyncReadStreamEventsForward streamId version count true

        let events = 
            slice.Events 
            |> Seq.map deserialize
            |> Seq.toList
        
        let nextEventNumber = 
            if slice.IsEndOfStream 
            then None 
            else Some slice.NextEventNumber

        return events, slice.LastEventNumber, nextEventNumber }

let appendToStream (store: IEventStoreConnection) streamId expectedVersion newEvents = 
    async {
        let serializedEvents = [| for event in newEvents -> serialize event |]

        do! Async.Ignore <| store.AsyncAppendToStream streamId expectedVersion serializedEvents }