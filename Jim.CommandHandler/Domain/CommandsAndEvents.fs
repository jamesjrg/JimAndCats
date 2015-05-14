namespace Jim.CommandHandler.Domain

open Jim.CommandHandler.Domain.AuthenticationService
open EventStore.YetAnotherClient
open NodaTime
open GenericErrorHandling
open System

[<AutoOpen>]
module Commands =
    type Command =
        | CreateUser of CreateUser
        | SetName of SetName
        | SetEmail of SetEmail
        | SetPassword of SetPassword

    and CreateUser = {
        Name: string
        Email: string
        Password: string
    }

    and SetName = {
        Id: Guid
        Name: string    
    }

    and SetEmail = {
        Id: Guid
        Email: string   
    }

    and SetPassword = {
        Id: Guid
        Password: string   
    }

[<AutoOpen>]
module Events =
    type Event =
        | UserCreated of UserCreated
        | NameChanged of NameChanged
        | EmailChanged of EmailChanged
        | PasswordChanged of PasswordChanged
    
    and UserCreated = {
        Id: Guid
        Name: Username
        Email: EmailAddress
        PasswordHash: PasswordHash
        CreationTime: Instant
    }

    and NameChanged = {
        Id: Guid
        Name: Username
    }

    and EmailChanged = {
        Id: Guid
        Email: EmailAddress
    }

    and PasswordChanged = {
        Id: Guid
        PasswordHash: PasswordHash
    }

[<AutoOpen>]
module private CommandHandlers =
    module private Constants =
        let minPasswordLength = 7 //using PBKDF2 with lots of iterations so needn't be huge
        let minUsernameLength = 5

    let createUsername (s:string) =
        let trimmedName = s.Trim()
     
        if trimmedName.Length < Constants.minUsernameLength then
            Failure (BadRequest (sprintf "Username must be at least %d characters" Constants.minUsernameLength))
        else
            Success (Username trimmedName)

    let createPasswordHash hashFunc (s:string) =
        let trimmedPassword = s.Trim()

        if trimmedPassword.Length < Constants.minPasswordLength then
            Failure (BadRequest (sprintf "Password must be at least %d characters" Constants.minPasswordLength))
        else
            Success (PasswordHash (hashFunc (trimmedPassword)))

    let createUser
        (repository:GenericRepository<User>)
        (createGuid: unit -> Guid)
        (createTimestamp: unit -> Instant)
        hashFunc
        (command : CreateUser) =
        resultBuilder { 
            let! email = async { return createEmailAddress command.Email }
            let! name = createUsername command.Name            
            //password hashing expensive so should come last
            let! hash = createPasswordHash hashFunc command.Password

            return! Success (UserCreated {
                Id = createGuid()
                Name = name
                Email = email
                PasswordHash = hash
                CreationTime = createTimestamp()
            })
        }

    let runCommandIfUserExists (repository :GenericRepository<User>) id command f =
        async {
            let! user = repository.Get(id)
            return
                match user with
                | None -> Failure NotFound
                | _ -> f command
        }

    let setName (command : SetName) =
        match createUsername command.Name with
        | Success name -> Success (NameChanged { Id = command.Id; Name = name; })
        | Failure f -> Failure f

    let setEmail (command : SetEmail) =
        match createEmailAddress command.Email with
        | Success email -> Success (EmailChanged { Id = command.Id; Email = email; })
        | Failure f -> Failure f

    let setPassword hashFunc (command : SetPassword) =
        match createPasswordHash hashFunc command.Password with
        | Success hash -> Success (PasswordChanged { Id = command.Id; PasswordHash = hash; })
        | Failure f -> Failure f

[<AutoOpen>]
module PublicCommandHandlers = 
    let handleCommand (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) (hashFunc: string -> string) (command:Command) (repository : GenericRepository<User>) =
            match command with
            | CreateUser command -> createUser repository createGuid createTimestamp hashFunc command
            | SetName command -> runCommandIfUserExists repository command.Id command setName
            | SetEmail command -> runCommandIfUserExists repository command.Id command setEmail
            | SetPassword command -> runCommandIfUserExists repository command.Id command (setPassword hashFunc)

    let handleCommandWithAutoGeneration (command:Command) (repository : GenericRepository<User>) =
        handleCommand
            Guid.NewGuid
            (fun () -> SystemClock.Instance.Now)
            PBKDF2.getHash
            command
            repository

(* End Command Handlers *)