module Jim.Tests.IntegrationTests

open Suave
open Suave.Types
open Suave.Http
open Suave.Web
open Suave.Testing

open System
open System.Net
open System.Net.Http
open System.Text

open Jim.ApplicationService
open Jim.Domain
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

[<Tests>]
let tests =
    testList "Integration tests"
        [
        testCase "Should be able to create a user" (fun () ->
            let store = storeWithEvents []

            let postData = new ByteArrayContent(Encoding.UTF8.GetBytes("""{"name":"Frank Moss", "email":"frank@somewhere.com","password":"kkk"}"""))

            let actual = (run_with' (webApp <| new AppService(store, streamId))) |> req HttpMethod.POST "/users/create" (Some postData)

            test <@ actual.Contains("\"id\":") && actual.Contains("User created") @>)

        testCase "Should be able to rename a user" (fun () ->
            let store = storeWithEvents [UserCreated { Id = guid1; Name="Bob Holness"; Email="bob.holness@itv.com"; Password="p4ssw0rd"; CreationTime = epoch} ]

            let postData = new ByteArrayContent(Encoding.UTF8.GetBytes("""{"id":"3C71C09A-2902-4682-B8AB-663432C8867B", "name":"Frank Moss"}"""))

            let actual = (run_with' (webApp <| new AppService(store, streamId))) |> req HttpMethod.PUT "/users/3C71C09A-2902-4682-B8AB-663432C8867B/name" (Some postData)

            test <@ actual.Contains("Frank Moss") && actual.Contains("Name changed to") @>)
        ]