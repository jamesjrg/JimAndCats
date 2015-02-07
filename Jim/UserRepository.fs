namespace Jim

open Jim.Domain
open MicroCQRS.Common
open System
open System.Collections.Generic

type UserRepository() =

    let usersById = new Dictionary<Guid, User>()
    let usersByEmail = new Dictionary<EmailAddress, User>()

    member this.Load(store:IEventStore<Event>, streamId, handleEvent) =
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
        member this.List() = usersById.Values :> User seq

        member this.Get (id:Guid) =
            match usersById.TryGetValue(id) with
            | true, x -> Some x
            | false, _ -> None

        member this.Put (x:User) =
            usersById.[x.Id] <- x
            usersByEmail.[x.Email] <- x
    
        member this.GetByEmail(email:EmailAddress) =
            match usersByEmail.TryGetValue(email) with
            | true, x -> Some x
            | false, _ -> None
