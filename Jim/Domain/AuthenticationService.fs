module Jim.Domain.AuthenticationService

open Jim.ErrorHandling
open Jim.Domain.IUserRepository
open System

type Authenticate = {
    Id: Guid
    Password: string   
}

let authenticate (command : Authenticate) (repository : IUserRepository) =
   Failure "unimplemented"