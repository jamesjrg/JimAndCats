module Jim.Tests.IntegrationTestHelpers

open Suave
open Suave.Types
open Suave.Web
open Suave.Testing

open System
open System.Net
open System.Text

open Jim.Domain
open Jim.UserModel
open Jim.WebServer

open NodaTime

open EventPersistence

open Fuchu
open Swensen.Unquote.Assertions

let run_with' = run_with default_config

let streamId = "testStream"

let storeWithEvents events =
    let projection = fun (x: Event) -> ()
    let store = EventPersistence.InMemoryStore<Event>(projection) :> IEventStore<Event>
    if not (List.isEmpty events) then
        store.AppendToStream streamId -1 events |> Async.RunSynchronously
    store

let guid1 = new Guid("3C71C09A-2902-4682-B8AB-663432C8867B")
let epoch = new Instant(0L)

