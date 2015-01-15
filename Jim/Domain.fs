module Jim.Domain

open NodaTime

open System
open System.Collections.Generic
open System.Text.RegularExpressions

(* Domain model types *)

type User = {
    Id: Guid
    Name: string
    Email: string
    Password: string
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
    Email: string
    Password: string
    CreationTime: Instant
}

and NameChanged = {
    Id: Guid
    Name: string
}

and EmailChanged = {
    Id: Guid
    Email: string
}

and PasswordChanged = {
    Id: Guid
    Password: string
}

(* End Events *)

(* Event handlers *)
let userCreated (state:State) (event: UserCreated) =
    state.Add(event.Id, {
        User.Id = event.Id
        Name = event.Name
        Email = event.Email
        Password = event.Password
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

//Apply commands
let createUser (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) (command : CreateUser) (state : State) =
    [ UserCreated {
        Id = createGuid()
        Name = command.Name
        Email = command.Email
        Password = command.Password
        CreationTime = createTimestamp()
    }]

let setName (command : SetName) (state : State) =
    [NameChanged { Id = command.Id; Name = command.Name; }]

let setEmail (command : SetEmail) (state : State) =
    [EmailChanged { Id = command.Id; Email = command.Email; }]

let setPassword (command : SetPassword) (state : State) =
    [PasswordChanged { Id = command.Id; Password = command.Password; }]

let handleCommand (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) command state =
    match command with
        | CreateUser command -> createUser createGuid createTimestamp command state
        | SetName command -> setName command state
        | SetEmail command -> setEmail command state
        | SetPassword command -> setPassword command state

let handleCommandWithAutoGeneration command state = handleCommand Guid.NewGuid (fun () -> SystemClock.Instance.Now) command state

let createEmailAddress (s:string) = 
    if Regex.IsMatch(s,@"^\S+@\S+\.\S+$") 
        then Some s
        else None