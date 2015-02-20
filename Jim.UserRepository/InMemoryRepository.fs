namespace Jim.UserRepository

open Jim.Domain
open System
open System.Collections.Generic

type InMemoryUserRepository() =

    let usersById = new Dictionary<Guid, User>()
    let usersByEmail = new Dictionary<EmailAddress, User>()     

    interface IUserRepository with
        member this.List() = async { return usersById.Values :> User seq }

        member this.Get (id:Guid) =
            async { 
                match usersById.TryGetValue(id) with
                | true, x -> return Some x
                | false, _ -> return None
            }

        member this.Put (x:User) =
            async { 
                usersById.[x.Id] <- x
                usersByEmail.[x.Email] <- x
            }
    
        member this.GetByEmail(email:EmailAddress) =
            async {
                match usersByEmail.TryGetValue(email) with
                | true, x -> return Some x
                | false, _ -> return None
            }
