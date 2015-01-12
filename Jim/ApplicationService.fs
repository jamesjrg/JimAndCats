namespace Jim.ApplicationService

open EventPersistence.EventStore
open Jim.AppSettings
open Jim.CommandHandler
open Jim.Domain

type AppService () =
    let streamId = appSettings.UserStream

    let projection = fun (x: Event) -> ()
    let store = EventPersistence.EventStore.create() |> subscribe streamId projection
    let commandHandler = Jim.CommandHandler.create streamId (readStream store) (appendToStream store)

    member this.createUser () = commandHandler <| CreateUser { 
            Name="Bob Holness"
            Email="bob.holness@itv.com"
            Password="p4ssw0rd" }

    member this.listUsers () = ()