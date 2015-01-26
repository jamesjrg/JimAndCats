namespace Jim.ApplicationService

open EventPersistence
open Jim.ApiResponses
open Jim.AppSettings
open Jim.Domain
open Jim.ErrorHandling
open Jim.UserModel
open Jim.UserRepository
open System

type Query =
    | ListUsers of AsyncReplyChannel<User seq>
    | GetUser of Guid * AsyncReplyChannel<User option>

type Message =
    | SingleEventMessage of SingleEventCommand * AsyncReplyChannel<Result<Event, string>>
    | AuthenticateMessage of Authenticate * AsyncReplyChannel<Result<unit,string>>
    | Query of Query

type AppService(store:IEventStore<Event>, streamId) =
    let load repository =
        let rec fold version =
            async {
            let! events, lastEvent, nextEvent = 
                store.ReadStream streamId version 500

            List.iter (handleEvent repository) events
            match nextEvent with
            | None -> return lastEvent
            | Some n -> return! fold n }
        fold 0

    let save expectedVersion events = store.AppendToStream streamId expectedVersion events    

    let agent = MailboxProcessor<Message>.Start <| fun inbox -> 
        let rec messageLoop version repository = async {
            let! message = inbox.Receive()

            match message with
            | SingleEventMessage (command, replyChannel) ->
                let result = handleSingleEventCommandWithAutoGeneration command repository
                match result with
                | Success newEvent ->
                    do! save version [newEvent]
                    handleEvent repository newEvent
                    replyChannel.Reply(result)
                    return! messageLoop (version + 1) repository
                | Failure f ->
                    replyChannel.Reply(result)
                    return! messageLoop version repository

            | AuthenticateMessage (command, replyChannel) -> 
                let result = authenticate command repository
                replyChannel.Reply(result)
                return! messageLoop version repository

            | Query query ->
                match query with
                | ListUsers replyChannel -> replyChannel.Reply(repository.List())
                | GetUser (id, replyChannel) -> replyChannel.Reply(repository.Get(id))

                return! messageLoop version repository
            }
        async {
            let repository = new Repository()
            let! version = load repository
            return! messageLoop version repository
            }

    let makeSingleEventMessage (command:SingleEventCommand) = 
        fun replyChannel ->
            SingleEventMessage (command, replyChannel)

    new() =
        let streamId = appSettings.UserStream

        let projection = fun (x: Event) -> ()

        let store =
            match appSettings.UseEventStore with
            | true -> new EventPersistence.EventStore<Event>(streamId, projection) :> IEventStore<Event>
            | false -> new EventPersistence.InMemoryStore<Event>(projection) :> IEventStore<Event>
        AppService(store, streamId)

    (* Commands. If the query model wasn't in memory there would be likely be two separate processes for command and query. *)

    member this.runSingleEventCommand(command:SingleEventCommand) =
        async {
            let! result = agent.PostAndAsyncReply(makeSingleEventMessage command)

            match result with
            | Success (UserCreated event) ->
                return Completed (ResponseWithIdAndMessage {
                ResponseWithIdAndMessage.id = event.Id
                message = "User created: " + extractUsername event.Name
                })
            | Success (NameChanged event) ->
                return Completed (ResponseWithIdAndMessage {
                ResponseWithIdAndMessage.id = event.Id
                message = "Name changed to: " + extractUsername event.Name
                })
            | Success (EmailChanged event) ->
                return Completed (ResponseWithIdAndMessage {
                ResponseWithIdAndMessage.id = event.Id
                message = "Email changed to: " + extractEmail event.Email
                })
            | Success (PasswordChanged event) ->
                return Completed (ResponseWithIdAndMessage {
                ResponseWithIdAndMessage.id = event.Id
                message = "Password changed"
                })
            | Failure f -> return BadRequest (ResponseWithMessage f)
        }

    member this.authenticate(command:Authenticate) =
        let makeMessage command = 
            fun replyChannel -> AuthenticateMessage (command, replyChannel)
        async {
            let! result = agent.PostAndAsyncReply(makeMessage command)
            return Completed (ResponseWithMessage "TODO")
        }

    (* End commands *)

    (* Queries *)

    member this.getUser(id) =
        async {
            let! user = agent.PostAndAsyncReply(fun replyChannel -> Query(GetUser(id, replyChannel)))
            return Completed (ResponseWithMessage <| user.ToString())
        }

    member this.listUsers() =
        async {
            let! users = agent.PostAndAsyncReply(fun replyChannel -> Query(ListUsers(replyChannel)))
            let usersAsString = "Users:\n" + (users |> Seq.map (fun u -> sprintf "%A" u) |> String.concat "\n")
            return Completed (ResponseWithMessage usersAsString)
        }

    (* End queries *)
