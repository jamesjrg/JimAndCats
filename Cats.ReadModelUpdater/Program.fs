module Cats.ReadModelUpdater.Program

open Cats.ReadModelUpdater.AppSettings
open MicroCQRS.Common

module private JimModel =
    let subscribe() =
        let handleUserEvent = (fun e -> ())
        let store = new EventStore<Event>(appSettings.IdentityEventStoreIp, appSettings.IdentityEventStorePort) :> IEventStore<Event>
        store.SubscribeToStreamFrom appSettings.PublicIdentityStream 0 handleUserEvent

module private CatModel =
    let subscribe() =
        let handleUserEvent = (fun e -> ())
        let store = new EventStore<Event>(appSettings.PrivateEventStoreIp, appSettings.PrivateEventStorePort) :> IEventStore<Event>
        store.SubscribeToStreamFrom appSettings.PrivateCatStream 0 handleUserEvent

[<EntryPoint>]
let main argv = 
    JimModel.subscribe()
    CatModel.subscribe()
    0
