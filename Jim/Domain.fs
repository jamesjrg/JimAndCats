module Jim.Domain

open NodaTime

open System
open System.Collections.Generic
open System.Text.RegularExpressions

open Jim.Hashing

(* Error handling *)

type Result<'a> = 
    | Success of 'a
    | Failure of string

(* End error handling *)

(* Constants *)

//using PBKDF2 with lots of iterations so needn't be huge
let minPasswordLength = 7

(* End Constants *)

(* Domain model types *)

type EmailAddress = EmailAddress of string

let extractString (EmailAddress s) = s

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

(* End events *)

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
    match state.TryGetValue(event.Id) with
    | true, user ->
        state.[event.Id] <- {user with Name = event.Name}
        state
    | false, _ -> state

let emailChanged (state:State) (event : EmailChanged) =
    match state.TryGetValue(event.Id) with
    | true, user ->
        state.[event.Id] <- {user with Email = event.Email}
        state
    | false, _ -> state

let passwordChanged (state:State) (event : PasswordChanged) =
    match state.TryGetValue(event.Id) with
    | true, user ->
        state.[event.Id] <- {user with PasswordHash = event.PasswordHash}
        state
    | false, _ -> state

let handleEvent (state : State) = function
    | UserCreated event -> userCreated state event
    | NameChanged event -> nameChanged state event
    | EmailChanged event -> emailChanged state event
    | PasswordChanged event -> passwordChanged state event

(* End Event Handlers *)

(* Command Handlers *)

let canonicalizeEmail (input:string) =
    input.Trim().ToLower()

let createEmailAddress (s:string) = 
    if Regex.IsMatch(s,@"^\S+@\S+\.\S+$") 
        then Success (EmailAddress (canonicalizeEmail s))
        else Failure "Invalid email address"

let createPasswordHash hashFunc (s:string) =
    if s.Length < minPasswordLength then
        Failure (sprintf "Password must be at least %d characters" minPasswordLength)
    else
        Success (PasswordHash (hashFunc (s.Trim())))

let createUser (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) hashFunc (command : CreateUser) (state : State) =
    //probably don't want to expend resources on password hash unless everything else is valid
    match createEmailAddress command.Email with
    | Success email ->
        match createPasswordHash hashFunc command.Password with
        | Success hash ->
            Success [ UserCreated {
                Id = createGuid()
                Name = command.Name
                Email = email
                PasswordHash = hash
                CreationTime = createTimestamp()
            }]
        | Failure f -> Failure f
    | Failure f -> Failure f

let setName (command : SetName) (state : State) =
    Success [NameChanged { Id = command.Id; Name = command.Name; }]

let setEmail (command : SetEmail) (state : State) =
    match createEmailAddress command.Email with
    | Success email -> Success [EmailChanged { Id = command.Id; Email = email; }]
    | Failure f -> Failure f

let setPassword hashFunc (command : SetPassword) (state : State) =
    match createPasswordHash hashFunc command.Password with
    | Success hash -> Success [PasswordChanged { Id = command.Id; PasswordHash = hash; }]
    | Failure f -> Failure f

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