namespace Jim.ApplicationService

open EventPersistence
open Jim.ApiResponses
open Jim.AppSettings
open Jim.Domain
open System

type Message =
    | Command of Command * AsyncReplyChannel<Event list>
    | ListUsers of AsyncReplyChannel<User seq>

type AppService () =
    let streamId = appSettings.UserStream

    let projection = fun (x: Event) -> ()

    let store =
        match appSettings.UseEventStore with
        | true -> new EventPersistence.EventStore<Event>(streamId, projection) :> IEventStore<Event>
        | false -> new EventPersistence.InMemoryStore<Event>(projection) :> IEventStore<Event>

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
                let newEvents = handleCommandWithAutoGeneration command state
                do! save version newEvents
                let newState = List.fold handleEvent state newEvents
                replyChannel.Reply(newEvents)
                return! messageLoop (version + List.length newEvents) newState
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

    member this.listUsers() =
        agent.PostAndAsyncReply(fun replyChannel -> ListUsers (replyChannel))

    member this.createUser(command) =
        async {
            let! newEvents = agent.PostAndAsyncReply(makeMessage command)

            match newEvents with
            | UserCreated event :: tail -> return ResponseWithIdAndMessage {
                ResponseWithIdAndMessage.id = event.Id
                message = "User created: " + event.Name
                }
            | _ -> return ResponseWithMessage {
                message = "Failed to create user"
                }
        }

    member this.setName(command) =
        async {
            let! newEvents = agent.PostAndAsyncReply(makeMessage command)

            match newEvents with
            | NameChanged event :: tail -> return ResponseWithIdAndMessage {
                ResponseWithIdAndMessage.id = event.Id
                message = "Name changed to: " + event.Name
                }
            | _ -> return ResponseWithMessage {
                message = "Failed to change name"
                }
        }

    member this.setEmail(command) =
        async {
            let! newEvents = agent.PostAndAsyncReply(makeMessage command)

            return {
                ResponseWithIdAndMessage.id = Guid.Empty
                message = "Todo"
                }
        }

    member this.setPassword(command) =
        async {
            let! newEvents = agent.PostAndAsyncReply(makeMessage command)

            return {
                ResponseWithIdAndMessage.id = Guid.Empty
                message = "Todo"
                }
        }

    member this.authenticate(id, details) =
        async {
            return {
                ResponseWithIdAndMessage.id = Guid.Empty
                message = "Todo"
                }
        }


        