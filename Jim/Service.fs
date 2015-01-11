module Jim.App

open Jim.CommandHandler
open Jim.Domain
open EventPersistence.EventStore
open System

//TODO: this should be a config variable
let streamId = "users"

let startService =
    try
        let projection = fun (x: Event) -> ()
        let store = EventPersistence.EventStore.create() |> subscribe streamId projection
        let commandHandler = Jim.CommandHandler.create streamId (readStream store) (appendToStream store)
        commandHandler <| CreateUser { 
            Name="Bob Holness"
            Email="bob.holness@itv.com"
            Password="p4ssw0rd" }
    with
    | e -> ()