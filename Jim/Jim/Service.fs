module Jim.App

open Jim.CommandHandler
open Jim.Domain
open EventStoreClient.Client
open System

//TODO: this should be a config variable
let streamId = "users"

let startService =
    try
        let projection = fun (x: Event) -> ()
        let store = EventStoreClient.Client.create() |> subscribe streamId projection
        let commandHandler = Jim.CommandHandler.create streamId (readStream store) (appendToStream store)
        commandHandler <| CreateUser { Id = Guid.NewGuid(); Name="Bob Holness"; Email="bob.holness@itv.com"; Password="p4ssw0rd" }
    with
    | e -> ()