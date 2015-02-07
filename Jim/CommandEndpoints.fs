module Jim.CommandEndpoints

open System

open Jim
open Jim.AppSettings
open Jim.CommandContracts
open Jim.Domain

open MicroCQRS.Common
open MicroCQRS.Common.CommandAgent
open MicroCQRS.Common.CommandFailure
open MicroCQRS.Common.Result

open Suave
open Suave.Http
open Suave.Extensions.Json

let getCommandPosterAndRepository() =
    let streamId = appSettings.PrivateIdentityStream
    let store =
        match appSettings.WriteToInMemoryStoreOnly with
        | false -> new EventStore<Event>(appSettings.PrivateEventStoreIp, appSettings.PrivateEventStorePort) :> IEventStore<Event>
        | true -> new MicroCQRS.Common.InMemoryStore<Event>() :> IEventStore<Event>
    let repository = new UserRepository()
    let initialVersion = repository.Load(store, streamId, handleEvent) |> Async.RunSynchronously
    let postCommand = getCommandPoster store repository handleCommandWithAutoGeneration handleEvent streamId initialVersion
    
    postCommand, repository

let private runCommand postCommand (command:Command) : Types.WebPart =
    fun httpContext ->
    async {
        let! result = postCommand command

        match result with
        | Success (UserCreated event) ->
            return! jsonResponse Successful.CREATED ( { UserCreatedResponse.Id = event.Id; Message = "User created: " + extractUsername event.Name }) httpContext
        | Success (NameChanged event) ->
            return! jsonOK ( { GenericResponse.Message = "Name changed to: " + extractUsername event.Name }) httpContext
        | Success (EmailChanged event) ->
            return! jsonOK ( { GenericResponse.Message = "Email changed to: " + extractEmail event.Email }) httpContext
        | Success (PasswordChanged event) ->
            return! jsonOK ( { GenericResponse.Message = "Password changed" }) httpContext
        | Failure (BadRequest f) -> return! RequestErrors.BAD_REQUEST f httpContext
        | Failure NotFound -> return! genericNotFound httpContext
    }

let createUser postCommand (requestDetails:CreateUserRequest) =   
    runCommand postCommand (CreateUser {Name=requestDetails.name; Email=requestDetails.email; Password=requestDetails.password})

let setName postCommand (id:Guid) (requestDetails:SetNameRequest) =    
    runCommand postCommand (SetName{ Id=id; Name = requestDetails.name})

let setEmail postCommand (id:Guid) (requestDetails:SetEmailRequest) =
    runCommand postCommand ( SetEmail {Id = id; Email = requestDetails.email} )

let setPassword postCommand (id:Guid) (requestDetails:SetPasswordRequest) =    
    runCommand postCommand ( SetPassword{ Id=id; Password = requestDetails.password})

