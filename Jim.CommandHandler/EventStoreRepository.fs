namespace Jim.CommandHandler.UserRepository

open Jim.CommandHandler.Domain
open EventStore.YetAnotherClient
open NodaTime
open System

type EventStoreRepository() = 

    member this.List() =    
        async {
            return []
        }

    member this.Get (id:Guid) =
        async {                
            return None
        }

    member this.Put (user:User) =          
        async {
            ()                
        }