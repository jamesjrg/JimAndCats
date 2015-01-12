(*
Loosely based on the client in FsUno.Prod by Jérémie Chassaing
*)

module EventPersistence.EventStore

open EventPersistence.EventStoreConnectionExtensions
open EventStore.ClientAPI
open Microsoft.FSharp.Reflection
open Nessos.FsPickler.Json
open System
open System.Net

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