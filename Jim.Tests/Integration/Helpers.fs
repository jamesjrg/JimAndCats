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

let createPostData (str:string) =
    Some (new ByteArrayContent(Encoding.UTF8.GetBytes(str)))

let streamId = "testStream"

let statusCodeAndContent response =
    content_string response, status_code response

let getTestCommandPosterAndRepo events =
    let store = EventPersistence.InMemoryStore<Event>() :> IEventStore<Event>
    if not (List.isEmpty events) then
        store.AppendToStream streamId -1 events |> Async.RunSynchronously
    let repository = new InMemoryUserRepository()
    let initialVersion = repository.Load(store, streamId) |> Async.RunSynchronously
    (CommandAgent.getCommandPoster store repository streamId initialVersion), repository

let req_resp_with_defaults methd resource data f_result =
    req_resp methd resource "" data None DecompressionMethods.None id f_result

let requestResponseWithGet initialEvents resource fResult =
    let postCommand, repo = getTestCommandPosterAndRepo initialEvents
    (run_with' (webApp postCommand repo)) |> req_resp_with_defaults HttpMethod.GET resource None fResult

let requestResponseWithPostData initialEvents methodType resource postDataString fResult =
    let postCommand, repo = getTestCommandPosterAndRepo initialEvents
    let postData = createPostData postDataString
    (run_with' (webApp postCommand repo)) |> req_resp_with_defaults methodType resource postData fResult

let requestContentWithPostData initialEvents methodType resource postDataString =
    requestResponseWithPostData initialEvents methodType resource postDataString content_string

let requestContentWithGet initialEvents resource =
    requestResponseWithGet initialEvents resource content_string

let guid1 = new Guid("3C71C09A-2902-4682-B8AB-663432C8867B")
let epoch = new Instant(0L)