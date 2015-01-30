module Jim.Domain.AuthenticationService

open Jim.Domain.UserAggregate
open Jim.Shared.ErrorHandling
open Jim.Domain.IUserRepository
open Jim.Domain.Hashing
open System

let authenticate (repository : IUserRepository) (id:Guid) password =
    match repository.Get(id) with
    | Some user -> Success (validatePassword (extractPasswordHash user.PasswordHash) password)
    | None -> Failure "User not found"