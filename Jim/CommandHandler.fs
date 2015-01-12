module Jim.CommandHandler

open NodaTime
open Jim.Domain
open System

let create (streamId:string) readStream appendToStream =
    let load =
        let rec fold (state: State) version =
            async {
            let! events, lastEvent, nextEvent = 
                readStream streamId version 500

            let state = List.fold handleEvent state events
            match nextEvent with
            | None -> return lastEvent, state
            | Some n -> return! fold state n }
        fold (new State()) 0

    let save expectedVersion events = appendToStream streamId expectedVersion events

    let agent = MailboxProcessor.Start <| fun inbox -> 
        let rec messageLoop version state = async {
            let! command = inbox.Receive()
            let newEvents = handleCommand command state
            do! save version newEvents
            let newState = List.fold handleEvent state newEvents
            return! messageLoop version state
            }
        async {
            let! version, state = load
            return! messageLoop version state }

    fun command -> agent.Post command