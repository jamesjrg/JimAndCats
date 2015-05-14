namespace Jim.CommandHandler.UserRepository

open Jim.Domain
open NodaTime
open System

type EventStoreRepository() = 

    let mapResultToUser id name email passwordHash creationTime= {
        User.Id = id
        Name = Username name
        Email = EmailAddress email
        PasswordHash = PasswordHash passwordHash
        CreationTime = new Instant(creationTime)
    }

    interface IUserRepository with
        member this.List() =    
            async {
                return []
            }

        //FIXME try get
        member this.Get (id:Guid) =
            async {                
                return None
            }

        member this.Put (user:User) =          
            async {
                ()                
            }