namespace Jim.ApplicationService

open EventPersistence
open Jim.ApiResponses
open Jim.AppSettings
open Jim.Domain
open Jim.ErrorHandling
open Jim.UserModel
open Jim.UserRepository
open System

type Message =
    | Command of Command * AsyncReplyChannel<Result<Event list, string>>
    | Query of Query * AsyncReplyChannel<User seq>

type AppService(store:IEventStore<Event>, streamId) =
    let load =
        let rec fold (state: State) version =
            async {
            let! events, lastEvent, nextEvent = 
                store.ReadStream streamId version 500

            let state = List.fold handleEvent state events
            match nextEvent with
            | None -> return lastEvent, state
            | Some n -> return! fold state n }
        fold (new State()) 0

    let save expectedVersion events = store.AppendToStream streamId expectedVersion events

    let agent = MailboxProcessor<Message>.Start <| fun inbox -> 
        let rec messageLoop version state = async {
            let! message = inbox.Receive()

            match message with
            | Command (command, replyChannel) -> 
                let result = handleCommandWithAutoGeneration command state
                match result with
                | Success newEvents ->
                    do! save version newEvents
                    let newState = List.fold handleEvent state newEvents
                    replyChannel.Reply(result)
                    return! messageLoop (version + List.length newEvents) newState
                | Failure f ->
                    replyChannel.Reply(result)
                    return! messageLoop version state

            | Query (query, replyChannel) ->
                handleQuery state query replyChannel |> ignore
                return! messageLoop version state
            }
        async {
            let! version, state = load
            return! messageLoop version state
            }

    let makeMessage command = 
        fun replyChannel ->
            Command (command, replyChannel)

    let singleEventOrFailure (result: Result<Event list,string>) =
        match result with
            | Success (event :: []) -> Success event
            | Failure f -> Failure (BadRequest (ResponseWithMessage f))
            | _ -> Failure (InternalError (ResponseWithMessage "Unexpected events"))

    new() =
        let streamId = appSettings.UserStream

        let projection = fun (x: Event) -> ()

        let store =
            match appSettings.UseEventStore with
            | true -> new EventPersistence.EventStore<Event>(streamId, projection) :> IEventStore<Event>
            | false -> new EventPersistence.InMemoryStore<Event>(projection) :> IEventStore<Event>
        AppService(store, streamId)

    (* Commands. If the query model wasn't in memory there would be likely be two separate processes for command and query. *)

    member this.createUser(command) =
        async {
            let! result = agent.PostAndAsyncReply(makeMessage command)

            match singleEventOrFailure result with
            | Success (UserCreated event) ->
                return Completed (ResponseWithIdAndMessage {
                ResponseWithIdAndMessage.id = event.Id
                message = "User created: " + extractUsername event.Name
                })
            | Failure f -> return f
            | _ -> return InternalError (ResponseWithMessage "Unexpected event type")
        }

    member this.setName(command) =
        async {
            let! result = agent.PostAndAsyncReply(makeMessage command)

            match singleEventOrFailure result with
            | Success (NameChanged event) ->
                return Completed (ResponseWithIdAndMessage {
                ResponseWithIdAndMessage.id = event.Id
                message = "Name changed to: " + extractUsername event.Name
                })
            | Failure f -> return f
            | _ -> return InternalError (ResponseWithMessage "Unexpected event type")
        }

    member this.setEmail(command) =
        async {
            let! result = agent.PostAndAsyncReply(makeMessage command)            

            match singleEventOrFailure result with
            | Success (EmailChanged event) ->
                return Completed (ResponseWithIdAndMessage {
                ResponseWithIdAndMessage.id = event.Id
                message = "Email changed to: " + extractEmail event.Email
                })
            | Failure f -> return f
            | _ -> return InternalError (ResponseWithMessage "Unexpected event type")
        }

    member this.setPassword(command) =
        async {
            let! newEvents = agent.PostAndAsyncReply(makeMessage command)

            return Completed ( ResponseWithIdAndMessage
                {
                ResponseWithIdAndMessage.id = Guid.Empty
                message = "Password changed"
                })
        }

    member this.authenticate(command) =
        async {
            let! result = agent.PostAndAsyncReply(makeMessage command)
            return Completed (ResponseWithMessage "TODO")
        }

    (* End commands *)

    (* Queries *)

    member this.listUsers() =
        async {
            let! users = agent.PostAndAsyncReply(fun replyChannel -> Query(ListUsers, replyChannel))
            let usersAsString = "Users:\n" + (users |> Seq.map (fun u -> sprintf "%A" u) |> String.concat "\n")
            return Completed (ResponseWithMessage usersAsString)
        }

    (* End queries *)


        