module Jim.Domain

open NodaTime

open System
open System.Collections.Generic
open System.Text.RegularExpressions

(* Domain model types *)

type EmailAdress = 
    | Unverified of string
    | Verified of string

type User = {
    Id: Guid
    Name: string
    Email: EmailAdress
    Password: string
    CreationTime: Instant
}

(* End domain model types *)

type State = Dictionary<Guid, User>

(* Commands *)

type Command =
    | CreateUser of CreateUser
    | ChangeName of ChangeName

and CreateUser = {
    Name: string
    Email: string
    Password: string
}

and ChangeName = {
    Name: string
    Id: Guid
}

(* End commands *)

(* Events *)

type Event =
    | UserCreated of UserCreated
    | NameChanged of NameChanged
    
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

(* End Events *)

(* Event handlers *)
let userCreated (state:State) (event: UserCreated) =
    state.Add(event.Id, {
        User.Id = event.Id
        Name = event.Name
        Email = Unverified event.Email
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
let createUser (command : CreateUser) (state : State) =
    [ UserCreated {
        Id = Guid.NewGuid()
        Name = command.Name
        Email = command.Email
        Password = command.Password
        CreationTime = SystemClock.Instance.Now
    }]

let changeName (command : ChangeName) (state : State) =
    [NameChanged { Id = command.Id; Name = command.Name; }]

let handleCommand command state =
    match command with
        | CreateUser command -> createUser command state
        | ChangeName command -> changeName command state

let createEmailAddress (s:string) = 
    if Regex.IsMatch(s,@"^\S+@\S+\.\S+$") 
        then Some s
        else None