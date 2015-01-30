module Jim.Tests.Integration.Helpers

open Suave
open Suave.Types
open Suave.Web
open Suave.Testing

open System
open System.Net
open System.Net.Http
open System.Text

open Jim
open Jim.Domain.CommandsAndEvents
open Jim.Domain.UserAggregate
open Jim.InMemoryUserRepository
open Jim.WebServer

open NodaTime

open EventPersistence

open Fuchu
open Swensen.Unquote.Assertions

let run_with' = run_with default_config

let streamId = "testStream"

let getTestCommandPosterAndRepo events =
    let store = EventPersistence.InMemoryStore<Event>() :> IEventStore<Event>
    if not (List.isEmpty events) then
        store.AppendToStream streamId -1 events |> Async.RunSynchronously
    let repository = new InMemoryUserRepository()
    let initialVersion = repository.Load(store, streamId) |> Async.RunSynchronously
    (CommandAgent.getCommandPoster store repository streamId initialVersion), repository

let guid1 = new Guid("3C71C09A-2902-4682-B8AB-663432C8867B")
let epoch = new Instant(0L)

let req_resp_with_defaults methd resource data f_result =
    req_resp methd resource "" data None DecompressionMethods.None id f_result

let createPostData (str:string) =
    Some (new ByteArrayContent(Encoding.UTF8.GetBytes(str)))