namespace Jim.CommandHandler.Domain

open Jim.CommandHandler.Domain.AuthenticationService
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
        (createGuid: unit -> Guid)
        (createTimestamp: unit -> Instant)
        hashFunc
        (command : CreateUser) =
        resultBuilder { 
            let! email = createEmailAddress command.Email
            let! name = createUsername command.Name            
            //password hashing expensive so should come last
            let! hash = createPasswordHash hashFunc command.Password

            return Success (UserCreated {
                Id = createGuid()
                Name = name
                Email = email
                PasswordHash = hash
                CreationTime = createTimestamp()
            })
        }

    let runCommandIfUserExists (maybeUser : User option) command f =        
        match maybeUser with
        | None -> Failure NotFound
        | _ -> f command

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
    let applyCommand (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) (hashFunc: string -> string) (command:Command) (maybeUser : User option) =
            match command with
            | CreateUser command -> createUser createGuid createTimestamp hashFunc command
            | SetName command -> runCommandIfUserExists maybeUser command setName
            | SetEmail command -> runCommandIfUserExists maybeUser command setEmail
            | SetPassword command -> runCommandIfUserExists maybeUser command (setPassword hashFunc)

    let applyCommandWithAutoGeneration (command:Command) maybeUser =
        applyCommand
            Guid.NewGuid
            (fun () -> SystemClock.Instance.Now)
            PBKDF2.getHash
            command
            maybeUser

(* End Command Handlers *)