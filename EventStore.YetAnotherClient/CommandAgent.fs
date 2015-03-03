module EventStore.YetAnotherClient.CommandAgent

open EventStore.YetAnotherClient
open GenericErrorHandling

let getCommandPoster<'TCommand, 'TEvent, 'TState>
    (store:IEventStore<'TEvent>)
    (state:'TState)
    (handleCommand:'TCommand -> 'TState -> Async<Result<'TEvent, CQRSFailure>>)
    (handleEvent:'TState -> 'TEvent -> Async<unit>)
    (streamId:string)
    (initialVersion:int) = 
    
    let save expectedVersion events = store.AppendToStream streamId expectedVersion events

    let agent = MailboxProcessor<'TCommand * AsyncReplyChannel<Result<'TEvent, CQRSFailure>>>.Start <| fun inbox -> 
        let rec messageLoop version = async {
            let! command, replyChannel = inbox.Receive()
            
            let! result = handleCommand command state
            match result with
            | Success newEvent ->
                do! save version [newEvent]
                handleEvent state newEvent |> ignore
                replyChannel.Reply(result)
                return! messageLoop (version + 1)
            | Failure f ->
                replyChannel.Reply(result)
                return! messageLoop version            
            }
        async {          
            return! messageLoop initialVersion
            }

    fun command -> agent.PostAndAsyncReply(fun replyChannel -> command, replyChannel)