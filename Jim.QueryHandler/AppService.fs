module Jim.QueryHandler.AppService

open Jim.Domain
open Jim.UserRepository
open Suave.Extensions.Json
open System

type GetUserResponse = {
    Id: Guid
    Name: string
    Email: string
    CreationTime: string
}

type GetUsersResponse = {
    Users: GetUserResponse seq
}

let getRepository() = 
    new InMemoryUserRepository()

let mapUserToUserResponse (user:User) =
    {
        GetUserResponse.Id = user.Id
        Name = extractUsername user.Name
        Email = extractEmail user.Email
        CreationTime = user.CreationTime.ToString()
    } 

let getUser (repository:IUserRepository) id =
    match repository.Get(id) with
    | Some user -> jsonOK (mapUserToUserResponse user)
    | None -> genericNotFound

let listUsers (repository:IUserRepository) =
    let users = repository.List() |> Seq.map mapUserToUserResponse
    jsonOK {GetUsersResponse.Users = users}