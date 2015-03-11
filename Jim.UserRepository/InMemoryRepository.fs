namespace Jim.UserRepository.InMemory

open Jim.Domain
open System
open System.Collections.Generic

type UserRepository() =

    let usersById = new Dictionary<Guid, User>()  

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
            }
