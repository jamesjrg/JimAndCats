module Jim.UserRepository.DatabaseUserRepository

open FSharp.Data
open Jim.Domain
open NodaTime
open System

[<Literal>]
let getUserById = "
    SELECT *
    FROM Jim.Users
    WHERE Id = @Id"

[<Literal>]
let getUserByEmail = "
    SELECT *
    FROM Jim.Users
    WHERE Email = @Email"

[<Literal>]
let getAllUsers = "SELECT * FROM Jim.Users"

type GetUserByIdQuery = SqlCommandProvider<getUserById, "name=JimUsers">
type GetUserByEmailQuery = SqlCommandProvider<getUserByEmail, "name=JimUsers">
type GetAllUsersQuery = SqlCommandProvider<getAllUsers, "name=JimUsers">

type DatabaseUserRepository() = 

    let mapResultToUser id name email passwordHash creationTime= {
        User.Id = id
        Name = Username name
        Email = EmailAddress email
        PasswordHash = PasswordHash passwordHash
        CreationTime = new Instant(creationTime)
    }

    interface IUserRepository with
        //FIXME async
        member this.List() =
            let cmd = new GetAllUsersQuery()
            cmd.AsyncExecute()
            |> Async.RunSynchronously
            |> Seq.map (fun result ->
                mapResultToUser result.Id result.Name result.Email result.PasswordHash result.CreationTime)

        //FIXME try get
        member this.Get (id:Guid) =
            let cmd = new GetUserByIdQuery()
            cmd.AsyncExecute(Id=id)
            |> Async.RunSynchronously
            |> Seq.head
            |> fun result -> mapResultToUser result.Id result.Name result.Email result.PasswordHash result.CreationTime
            |> Some

        member this.Put (x:User) =
            ()
    
        //FIXME try get
        member this.GetByEmail(email:EmailAddress) =
            let cmd = new GetUserByEmailQuery()
            cmd.AsyncExecute(Email=extractEmail email)
            |> Async.RunSynchronously
            |> Seq.head
            |> fun result -> mapResultToUser result.Id result.Name result.Email result.PasswordHash result.CreationTime
            |> Some