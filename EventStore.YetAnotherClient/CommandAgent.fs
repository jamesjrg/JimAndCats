module EventStore.YetAnotherClient.CommandAgent

open EventStore.YetAnotherClient
open EventStore.ClientAPI
open GenericErrorHandling
open System

let getCommandAgent<'TCommand, 'TEvent, 'TAggregate>
    (getAggregate : Guid-> Async<'TAggregate option * int>)
    saveEvent
    (applyCommand:'TCommand -> 'TAggregate option -> Result<'TEvent, CQRSFailure>) =

    let agent = MailboxProcessor<Guid * 'TCommand * AsyncReplyChannel<Result<'TEvent, CQRSFailure>>>.Start <| fun inbox -> 
        let rec messageLoop () =
            async {
                let! aggregateId, command, replyChannel = inbox.Receive()
            
                let! aggregate, actualVersion = getAggregate aggregateId 
                let result = applyCommand command aggregate
                match result with
                | Success newEvent ->
                    //TODO: need to handle the case that there are multiple agents writing to the same aggregate
                    //alongside that may well want to run multiple agents per web server
                    do! saveEvent aggregateId (actualVersion + 1) newEvent
                | _ -> ()

                replyChannel.Reply(result)
                return! messageLoop ()
            }
        async {          
            return! messageLoop ()
            }

    fun aggregateId command -> agent.PostAndAsyncReply(fun replyChannel -> aggregateId, command, replyChannel)