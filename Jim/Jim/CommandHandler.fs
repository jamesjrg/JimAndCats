module Jim.CommandHandler

open Jim.Domain
open EventStoreClient.Client
open System

let create readStream appendToStream =
    let load =
        let rec fold (state: State) version =
            async {
            let! events, lastEvent, nextEvent = 
                readStream version 500

            let state = List.fold handleEvent state events
            match nextEvent with
            | None -> return lastEvent, state
            | Some n -> return! fold state n }
        fold (new State()) 0

    let save events = appendToStream events

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

let testFunc =
    let store = EventStoreClient.Client.create() |> subscribe (fun x -> ())
    try
        appendToStream store "testStream" -1 [UserCreated { Id = Guid.NewGuid(); Name="Bob Holness"; Email="bob.holness@itv.com"; Password="p4ssw0rd" }] |> Async.RunSynchronously
    with
    | e -> ()
