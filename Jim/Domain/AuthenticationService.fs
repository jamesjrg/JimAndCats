module Jim.Domain.AuthenticationService

open Jim.ErrorHandling
open Jim.Domain.UserRepository
open System

type Authenticate = {
    Id: Guid
    Password: string   
}

let authenticate (command : Authenticate) (repository : Repository) =
   Failure "unimplemented"