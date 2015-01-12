namespace Jim.ApplicationService

open EventPersistence
open Jim.AppSettings
open Jim.CommandHandler
open Jim.Domain

type AppService () =
    let streamId = appSettings.UserStream

    let projection = fun (x: Event) -> ()

    let store = match appSettings.UseEventStore with
    | true -> new EventPersistence.EventStore<Event>(streamId, projection) :> IEventStore<Event>
    | false -> new EventPersistence.InMemoryStore<Event>(projection) :> IEventStore<Event>

    let commandHandler = Jim.CommandHandler.create streamId store

    member this.createUser () = commandHandler <| CreateUser { 
            Name="Bob Holness"
            Email="bob.holness@itv.com"
            Password="p4ssw0rd" }

    member this.listUsers () = ()