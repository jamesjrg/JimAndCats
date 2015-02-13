module Cats.ReadModelUpdater.Program

open Cats.ReadModelUpdater.AppSettings
open MicroCQRS.Common
open NodaTime
open System

type PublicIdentityEvent =
    | UserCreated of string
    | NameChanged of string
    | EmailChanged of string

type PrivateCatsEvent =
    | CatCreated of CatCreated
    | TitleChanged of TitleChanged

and CatCreated = {
    Id: Guid
    Title: string
    Owner: Guid
    CreationTime: Instant
}

and TitleChanged = {
    Id: Guid
    Title: string
}

module private JimModel =
    let subscribe() =
        let handleUserEvent = (fun e -> ())
        let store = new EventStore<PublicIdentityEvent>(appSettings.IdentityEventStoreIp, appSettings.IdentityEventStorePort) :> IEventStore<PublicIdentityEvent>
        store.SubscribeToStreamFrom appSettings.PublicIdentityStream 0 handleUserEvent

module private CatModel =
    let subscribe() =
        let handleCatEvent = (fun e -> ())
        let store = new EventStore<PrivateCatsEvent>(appSettings.PrivateEventStoreIp, appSettings.PrivateEventStorePort) :> IEventStore<PrivateCatsEvent>
        store.SubscribeToStreamFrom appSettings.PrivateCatStream 0 handleCatEvent

[<EntryPoint>]
let main argv = 
    JimModel.subscribe()
    CatModel.subscribe()
    0
