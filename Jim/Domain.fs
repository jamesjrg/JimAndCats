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

let bind func result = 
    match result with
    | Success s -> func s
    | Failure f -> Failure f

let (>>=) twoTrackInput switchFunction = 
    bind switchFunction twoTrackInput

(* End error handling *)

(* Constants *)

//using PBKDF2 with lots of iterations so needn't be huge
let minPasswordLength = 7
let minUsernameLength = 5

(* End Constants *)

(* Domain model types *)

type Username = Username of string

type EmailAddress = EmailAddress of string

type PasswordHash = PasswordHash of string

type User = {
    Id: Guid
    Name: Username
    Email: EmailAddress
    PasswordHash: PasswordHash
    CreationTime: Instant
}

let extractUsername (Username s) = s
let extractEmail (EmailAddress s) = s

(* End domain model types *)

(* The in-memory query model - this might be a SQL database in a large system *)

type State = Dictionary<Guid, User>

(* end query model *)

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

let createUsername (s:string) = 
    if s.Length < minUsernameLength then
        Failure (sprintf "Username must be at least %d characters" minUsernameLength)
    else
        Success (Username s)

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
    let result =
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

    match result with
    | Success s -> Success [s]
    | Failure f -> Failure f

let setName (command : SetName) (state : State) =
    match createUsername command.Name with
    | Success name -> Success [NameChanged { Id = command.Id; Name = name; }]
    | Failure f -> Failure f

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