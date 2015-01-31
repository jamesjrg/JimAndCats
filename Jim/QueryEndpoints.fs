module Jim.QueryEndpoints

open System
open MicroCQRS.Common
open MicroCQRS.Common.Result
open Jim.Domain.AuthenticationService
open Jim.Domain.UserAggregate
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

let authenticate (repository:ISimpleRepository<User>) (id:Guid) (request:AuthenticateRequest) =
    match authenticate repository id request.Password with
    | Success authResult -> jsonOK ({ AuthResponse.IsAuthenticated = authResult})
    | Failure f -> genericNotFound

let getUser (repository:ISimpleRepository<User>) id =
    match repository.Get(id) with
    | Some user -> jsonOK (mapUserToUserResponse user)
    | None -> genericNotFound

let listUsers (repository:ISimpleRepository<User>) =
    let users = repository.List() |> Seq.map mapUserToUserResponse
    jsonOK {GetUsersResponse.Users = users}