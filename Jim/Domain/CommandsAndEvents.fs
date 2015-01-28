module Jim.Domain.CommandsAndEvents

open NodaTime

open System
open System.Text.RegularExpressions

open Jim.ErrorHandling
open Jim.Domain.Hashing
open Jim.Domain.UserAggregate
open Jim.Domain.IUserRepository

(* Constants *)

//using PBKDF2 with lots of iterations so needn't be huge
let minPasswordLength = 7
let minUsernameLength = 5

(* End Constants *)

(* Commands *)

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

(* End commands *)

(* Events *)

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

(* End events *)

(* Event handlers *)
let userCreated (repository:IUserRepository) (event: UserCreated) =
    repository.Add(
        {
            User.Id = event.Id
            Name = event.Name
            Email = event.Email
            PasswordHash = event.PasswordHash
            CreationTime = event.CreationTime
        })

let nameChanged (repository:IUserRepository) (event : NameChanged) =
    match repository.Get(event.Id) with
    | Some user -> repository.Put({user with Name = event.Name})
    | None -> ()

let emailChanged (repository:IUserRepository) (event : EmailChanged) =
    match repository.Get(event.Id) with
    | Some user -> repository.Put({user with Email = event.Email})
    | None -> ()

let passwordChanged (repository:IUserRepository) (event : PasswordChanged) =
    match repository.Get(event.Id) with
    | Some user -> repository.Put({user with PasswordHash = event.PasswordHash})
    | None -> ()

let handleEvent (repository : IUserRepository) = function
    | UserCreated event -> userCreated repository event
    | NameChanged event -> nameChanged repository event
    | EmailChanged event -> emailChanged repository event
    | PasswordChanged event -> passwordChanged repository event

(* End Event Handlers *)

(* Command Handlers *)

let createUsername (s:string) =
    let trimmedName = s.Trim()
     
    if trimmedName.Length < minUsernameLength then
        Failure (sprintf "Username must be at least %d characters" minUsernameLength)
    else
        Success (Username trimmedName)

let canonicalizeEmail (input:string) =
    input.Trim().ToLower()

let createEmailAddress (s:string) =
    let canonicalized = canonicalizeEmail s
    if Regex.IsMatch(canonicalized, @"^\S+@\S+\.\S+$") 
        then Success (EmailAddress canonicalized)
        else Failure "Invalid email address"

let createPasswordHash hashFunc (s:string) =
    let trimmedPassword = s.Trim()

    if trimmedPassword.Length < minPasswordLength then
        Failure (sprintf "Password must be at least %d characters" minPasswordLength)
    else
        Success (PasswordHash (hashFunc (trimmedPassword)))

let createUser (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) hashFunc (command : CreateUser) (repository : IUserRepository) =
    let tryCreateUsername (command : CreateUser) =
        match createUsername command.Name with
        | Success name -> Success (name, command)
        | Failure f -> Failure f
    
    let tryCreateEmailAddress (name, command : CreateUser) =
        match createEmailAddress command.Email with
        | Success email -> Success (name, email, command)
        | Failure f -> Failure f

    let tryCreatePasswordHash (name, email, command : CreateUser) =
        match createPasswordHash hashFunc command.Password with
        | Success hash -> Success (name, email, hash)
        | Failure f -> Failure f

    //password hashing expensive so should come last
    command
    |> tryCreateUsername
    >>= tryCreateEmailAddress
    >>= tryCreatePasswordHash
    >>= (fun (name, email, hash) -> Success (UserCreated {
                Id = createGuid()
                Name = name
                Email = email
                PasswordHash = hash
                CreationTime = createTimestamp()
        }))

let setName (command : SetName) (repository : IUserRepository) =
    match createUsername command.Name with
    | Success name -> Success (NameChanged { Id = command.Id; Name = name; })
    | Failure f -> Failure f

let setEmail (command : SetEmail) (repository : IUserRepository) =
    match createEmailAddress command.Email with
    | Success email -> Success (EmailChanged { Id = command.Id; Email = email; })
    | Failure f -> Failure f

let setPassword hashFunc (command : SetPassword) (repository : IUserRepository) =
    match createPasswordHash hashFunc command.Password with
    | Success hash -> Success (PasswordChanged { Id = command.Id; PasswordHash = hash; })
    | Failure f -> Failure f

let handleCommand (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) (hashFunc: string -> string) (command:Command) (repository : IUserRepository) =
    match command with
        | CreateUser command -> createUser createGuid createTimestamp hashFunc command repository
        | SetName command -> setName command repository
        | SetEmail command -> setEmail command repository
        | SetPassword command -> setPassword hashFunc command repository

let handleCommandWithAutoGeneration (command:Command) (repository : IUserRepository) =
    handleCommand
        Guid.NewGuid
        (fun () -> SystemClock.Instance.Now)
        PBKDF2Hash
        command
        repository

(* End Command Handlers *)