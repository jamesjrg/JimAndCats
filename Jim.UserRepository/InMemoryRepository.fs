namespace Jim.UserRepository

open Jim.Domain
open System
open System.Collections.Generic

type InMemoryUserRepository() =

    let usersById = new Dictionary<Guid, User>()
    let usersByEmail = new Dictionary<EmailAddress, User>()     

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
