namespace Jim.ApplicationService

open EventPersistence
open Jim.ApiResponses
open Jim.AppSettings
open Jim.Domain
open System

type Message =
    | Command of Command * AsyncReplyChannel<Result<Event list>>
    | ListUsers of AsyncReplyChannel<User seq>

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

            | ListUsers replyChannel ->
                replyChannel.Reply(state.Values)
                return! messageLoop version state
            }
        async {
            let! version, state = load
            return! messageLoop version state
            }

    let makeMessage command = 
        fun replyChannel ->
            Command (command, replyChannel)

    new() =
        let streamId = appSettings.UserStream

        let projection = fun (x: Event) -> ()

        let store =
            match appSettings.UseEventStore with
            | true -> new EventPersistence.EventStore<Event>(streamId, projection) :> IEventStore<Event>
            | false -> new EventPersistence.InMemoryStore<Event>(projection) :> IEventStore<Event>
        AppService(store, streamId)

    member this.listUsers() =
        agent.PostAndAsyncReply(fun replyChannel -> ListUsers (replyChannel))

    member this.createUser(command) =
        async {
            let! result = agent.PostAndAsyncReply(makeMessage command)

            match result with
            | Success ((UserCreated event) :: []) ->
                return Completed (ResponseWithIdAndMessage {
                ResponseWithIdAndMessage.id = event.Id
                message = "User created: " + event.Name
                })
            | Failure f -> return BadRequest (ResponseWithMessage { message = f })
            | _ -> return InternalError (ResponseWithMessage { message = "Unexpected events" })
        }

    member this.setName(command) =
        async {
            let! result = agent.PostAndAsyncReply(makeMessage command)

            match result with
            | Success ((NameChanged event) :: []) ->
                return Completed (ResponseWithIdAndMessage {
                ResponseWithIdAndMessage.id = event.Id
                message = "Name changed to: " + event.Name
                })
            | Failure f -> return BadRequest (ResponseWithMessage { message = f })
            | _ -> return InternalError (ResponseWithMessage { message = "Unexpected events" })
        }

    member this.setEmail(command) =
        async {
            let! result = agent.PostAndAsyncReply(makeMessage command)            

            match result with
            | Success ((EmailChanged event) :: []) ->
                return Completed (ResponseWithIdAndMessage {
                ResponseWithIdAndMessage.id = event.Id
                message = "Email changed to: " + extractString event.Email
                })
            | Failure f -> return BadRequest (ResponseWithMessage { message = f })
            | _ -> return InternalError (ResponseWithMessage { message = "Unexpected events" })
        }

    member this.setPassword(command) =
        async {
            let! newEvents = agent.PostAndAsyncReply(makeMessage command)

            return Completed ( ResponseWithIdAndMessage
                {
                ResponseWithIdAndMessage.id = Guid.Empty
                message = "Todo"
                })
        }

    member this.authenticate(id, details) =
        async {
            return Completed ( ResponseWithIdAndMessage{
                ResponseWithIdAndMessage.id = Guid.Empty
                message = "Todo"
                })
        }


        