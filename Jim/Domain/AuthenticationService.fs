module Jim.Domain.AuthenticationService

open Jim.Domain.UserAggregate
open Jim.ErrorHandling
open Jim.Domain.IUserRepository
open Jim.Domain.Hashing
open System

type Authenticate = {
    Id: Guid
    Password: string   
}

let authenticate (command : Authenticate) (repository : IUserRepository) =
    match repository.Get(command.Id) with
    | Some user -> Success (validatePassword (extractPasswordHash user.PasswordHash) command.Password)
    | None -> Failure "User not found"