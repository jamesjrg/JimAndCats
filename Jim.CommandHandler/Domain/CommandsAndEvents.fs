namespace Jim.Domain

open Jim.Domain.AuthenticationService
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

    let checkForDuplicateEmail (repository:IUserRepository) (email:EmailAddress) =
        async {
            let! user = repository.GetByEmail(email)
            return
                match user with
                | Some _ -> Failure (BadRequest "User with this email address already exists")
                | None -> Success email        
        }

    let tryCreateUserAfterEmailValidation
        (uniqueEmail: EmailAddress)
        (createGuid: unit -> Guid)
        (createTimestamp: unit -> Instant)
        hashFunc
        (command : CreateUser) =
        //don't really need to use a custom computation builder here
        resultBuilder {
            let! name = createUsername command.Name
            
            //password hashing expensive so should come last
            let! hash = createPasswordHash hashFunc command.Password

            return Success (UserCreated {
                Id = createGuid()
                Name = name
                Email = uniqueEmail
                PasswordHash = hash
                CreationTime = createTimestamp()
            })
        }

    //FIXME there has to be a better way of doing this, if this was C# I could just use async combined with early returns
    let createUser
        (repository:IUserRepository)
        (createGuid: unit -> Guid)
        (createTimestamp: unit -> Instant)
        hashFunc
        (command : CreateUser) =
        async { 
            let maybeEmail = createEmailAddress command.Email
            match maybeEmail with
            | Success email -> 
                let! uniqueEmailResult = checkForDuplicateEmail repository email
                match uniqueEmailResult with
                | Success uniqueEmail -> return tryCreateUserAfterEmailValidation uniqueEmail createGuid createTimestamp hashFunc command
                | Failure f -> return Failure f
            | Failure f -> return Failure f
        }

    let runCommandIfUserExists (repository :IUserRepository) id command f =
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
    let handleCommand (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) (hashFunc: string -> string) (command:Command) (repository : IUserRepository) =
            match command with
            | CreateUser command -> createUser repository createGuid createTimestamp hashFunc command
            | SetName command -> runCommandIfUserExists repository command.Id command setName
            | SetEmail command -> runCommandIfUserExists repository command.Id command setEmail
            | SetPassword command -> runCommandIfUserExists repository command.Id command (setPassword hashFunc)

    let handleCommandWithAutoGeneration (command:Command) (repository : IUserRepository) =
        handleCommand
            Guid.NewGuid
            (fun () -> SystemClock.Instance.Now)
            PBKDF2.getHash
            command
            repository

(* End Command Handlers *)