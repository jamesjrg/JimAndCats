namespace Jim.InMemoryUserRepository

open MicroCQRS.Common
open Jim.Domain.UserAggregate
open Jim.Domain.IUserRepository
open Jim.Domain.CommandsAndEvents
open System
open System.Collections.Generic

(* An in-memory repository implementation. This might be a SQL database in a large system *)

type State = Dictionary<Guid, User>

type InMemoryUserRepository() =
    let state = new State()

    member this.Load(store:IEventStore<Event>, streamId) =
            let rec fold version =
                async {
                let! events, lastEvent, nextEvent = 
                    store.ReadStream streamId version 500

                List.iter (handleEvent this) events
                match nextEvent with
                | None -> return lastEvent
                | Some n -> return! fold n }
            fold 0

    interface IUserRepository with
        member this.List() = state.Values :> User seq

        member this.Get (id:Guid) =
            match state.TryGetValue(id) with
            | true, user -> Some user
            | false, _ -> None

        member this.Put (user:User) =
            state.[user.Id] <- user
        