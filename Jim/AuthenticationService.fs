module Jim.AuthenticationService

open Jim.ErrorHandling
open Jim.UserRepository
open System

type Authenticate = {
    Id: Guid
    Password: string   
}

let authenticate (command : Authenticate) (repository : Repository) =
   Failure "unimplemented"