module Jim.CommandHandler.DuplicateEmailChecker

open FSharp.Data

[<Literal>]
let checkForDuplicateEmail =
    "select exists (select * from Jim.UserEmail where Email = @Email)"

type CheckForDuplicateEmailQuery = SqlCommandProvider<checkForDuplicateEmail, "name=Jim">

type DuplicateEmailChecker() = 

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
            async {
                let! result = cmd.AsyncExecute()
                return result |> Seq.map (fun result ->
                    mapResultToUser result.Id result.Name result.Email result.PasswordHash result.CreationTime)
            }

        //FIXME try get
        member this.Get (id:Guid) =
            let cmd = new GetUserByIdQuery()
            async {                
                let! result = cmd.AsyncExecute(Id=id)
                return result |> Seq.head
                |> fun result -> mapResultToUser result.Id result.Name result.Email result.PasswordHash result.CreationTime
                |> Some
            }

        member this.Put (user:User) =            
            let cmd = new InsertUserCommand()
            async {
                cmd.AsyncExecute(
                    Id=user.Id,
                    Name=extractUsername user.Name,
                    Email=extractEmail user.Email,
                    PasswordHash=extractPasswordHash user.PasswordHash,
                    CreationTime=user.CreationTime.Ticks) |> ignore
            }
    
        //FIXME try get
        member this.GetByEmail(email:EmailAddress) =
            let cmd = new GetUserByEmailQuery()
            async {
                let! result = cmd.AsyncExecute(Email=extractEmail email)
                return result |> Seq.head
                |> fun result -> mapResultToUser result.Id result.Name result.Email result.PasswordHash result.CreationTime
                |> Some
            }