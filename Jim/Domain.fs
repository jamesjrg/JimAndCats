module Jim.Domain

open NodaTime

open System
open System.Collections.Generic
open System.Text.RegularExpressions

open Jim.Encryption

(* Domain model types *)

type EmailAddress = EmailAddress of string

type PasswordHash = PasswordHash of string

type User = {
    Id: Guid
    Name: string
    Email: EmailAddress
    PasswordHash: PasswordHash
    CreationTime: Instant
}

(* End domain model types *)

type State = Dictionary<Guid, User>

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
    Name: string
    Email: EmailAddress
    PasswordHash: PasswordHash
    CreationTime: Instant
}

and NameChanged = {
    Id: Guid
    Name: string
}

and EmailChanged = {
    Id: Guid
    Email: EmailAddress
}

and PasswordChanged = {
    Id: Guid
    PasswordHash: PasswordHash
}

(* End Events *)

(* Event handlers *)
let userCreated (state:State) (event: UserCreated) =
    state.Add(event.Id, {
        User.Id = event.Id
        Name = event.Name
        Email = event.Email
        PasswordHash = event.PasswordHash
        CreationTime = event.CreationTime
        })
    state

let nameChanged (state:State) (event : NameChanged) =
    let userResult = state.TryGetValue(event.Id)
    match userResult with
    | true, user ->
        state.[event.Id] <- {user with Name = event.Name}
        state
    | false, _ -> state

let handleEvent (state : State) = function
    | UserCreated event -> userCreated state event
    | NameChanged event -> nameChanged state event

(* End Event Handlers *)

(* Command Handlers *)

let createEmailAddress (s:string) = 
    if Regex.IsMatch(s,@"^\S+@\S+\.\S+$") 
        then Some (EmailAddress s)
        else None

let createPasswordHash hashFunc (s:string) = 
    PasswordHash (hashFunc s)

let createUser (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) hashFunc (command : CreateUser) (state : State) =
    match createEmailAddress command.Email with
    | Some email ->
        [ UserCreated {
            Id = createGuid()
            Name = command.Name
            Email = email
            PasswordHash = createPasswordHash hashFunc command.Password
            CreationTime = createTimestamp()
        }]
    | None -> []

let setName (command : SetName) (state : State) =
    [NameChanged { Id = command.Id; Name = command.Name; }]

let setEmail (command : SetEmail) (state : State) =
    match createEmailAddress command.Email with
    | Some email -> [EmailChanged { Id = command.Id; Email = email; }]
    | None -> []

let setPassword hashFunc (command : SetPassword) (state : State) =
    [PasswordChanged { Id = command.Id; PasswordHash = createPasswordHash hashFunc command.Password; }]

let handleCommand (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) (hashFunc: string -> string) command state =
    match command with
        | CreateUser command -> createUser createGuid createTimestamp hashFunc command state
        | SetName command -> setName command state
        | SetEmail command -> setEmail command state
        | SetPassword command -> setPassword hashFunc command state

let handleCommandWithAutoGeneration command state =
    handleCommand
        Guid.NewGuid
        (fun () -> SystemClock.Instance.Now)
        PBKDF2Hash
        command
        state

(* End Command Handlers *)