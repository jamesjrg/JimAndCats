module Jim.QueryEndpoints

open System
open Jim.Result
open Jim.Domain.AuthenticationService
open Jim.Domain.UserAggregate
open Jim.Domain.IUserRepository
open Suave
open Suave.Types
open Suave.Extensions.Json

type AuthenticateRequest = {
    Password: string   
}

type GetUserResponse = {
    Id: Guid
    Name: string
    Email: string
    CreationTime: string
}

type GetUsersResponse = {
    Users: GetUserResponse seq
}

type AuthResponse = {
    IsAuthenticated: bool
}

let mapUserToUserResponse (user:User) =
    {
        GetUserResponse.Id = user.Id
        Name = extractUsername user.Name
        Email = extractEmail user.Email
        CreationTime = user.CreationTime.ToString()
    } 

let authenticate (repository:IUserRepository) (id:Guid) (request:AuthenticateRequest) =
    match authenticate repository id request.Password with
    | Success authResult -> jsonOK ({ AuthResponse.IsAuthenticated = authResult})
    | Failure f -> genericNotFound

let getUser (repository:IUserRepository) id =
    match repository.Get(id) with
    | Some user -> jsonOK (mapUserToUserResponse user)
    | None -> genericNotFound

let listUsers (repository:IUserRepository) =
    let users = repository.List() |> Seq.map mapUserToUserResponse
    jsonOK {GetUsersResponse.Users = users}