namespace Cats.ApplicationService

open EventPersistence
open Cats.ApiResponses
open Cats.AppSettings
open Cats.Domain
open System

type Message =
    | Command of Command * AsyncReplyChannel<Event list>
    //TODO
    | GetStuff of AsyncReplyChannel<string>

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
                let newEvents = handleCommandWithAutoGeneration command state
                do! save version newEvents
                let newState = List.fold handleEvent state newEvents
                replyChannel.Reply(newEvents)
                return! messageLoop (version + List.length newEvents) newState
            | GetStuff replyChannel ->
                replyChannel.Reply("FIXME")
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

        let store =
            match appSettings.UseEventStore with
            | true -> new EventPersistence.EventStore<Event>(streamId) :> IEventStore<Event>
            | false -> new EventPersistence.InMemoryStore<Event>() :> IEventStore<Event>
        AppService(store, streamId)

    member this.doSomething() =
        agent.PostAndAsyncReply(fun replyChannel -> GetStuff (replyChannel))




        