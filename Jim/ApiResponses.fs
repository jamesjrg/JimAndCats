module Jim.ApiResponses

open System

type GenericResponse = {
    Message: string
}

type UserCreatedResponse = {
    Id: Guid
    Message: string
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

type TaggedApiResponse =
    | OK of obj
    | NotFound
    | BadRequest of obj