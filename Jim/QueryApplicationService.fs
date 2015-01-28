module Jim.QueryApplicationService

open System
open Jim.ApiResponses
open Jim.ErrorHandling
open Jim.ApiResponses
open Jim.Domain.AuthenticationService
open Jim.Domain.UserAggregate
open Jim.Domain.IUserRepository

type QueryAppService(repository:IUserRepository) =    
    
    let mapUserToUserResponse user =
        {
            GetUserResponse.Id = user.Id
            Name = extractUsername user.Name
            Email = extractEmail user.Email
            CreationTime = user.CreationTime.ToString()
        } 

    member this.authenticate(details:Authenticate) =
        match authenticate details repository with
        | Success authResult -> OK ({ AuthResponse.IsAuthenticated = authResult})
        | Failure f -> BadRequest ({ GenericResponse.Message = f})

     member this.getUser(id) =
        match repository.Get(id) with
        | Some user -> OK (mapUserToUserResponse user)
        | None -> NotFound

    member this.listUsers() =
        let users = repository.List() |> Seq.map mapUserToUserResponse
        OK ({GetUsersResponse.Users = users})