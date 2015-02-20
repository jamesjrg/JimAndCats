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
    async {
        let! result = repository.Get(id)
        return
            match result with
            | Some user -> jsonOK (mapUserToUserResponse user)
            | None -> genericNotFound
    }

let listUsers (repository:IUserRepository) =
    async {    
        let! users = repository.List()
        let mappedUsers = Seq.map mapUserToUserResponse users
        return jsonOK {GetUsersResponse.Users = mappedUsers}
    }