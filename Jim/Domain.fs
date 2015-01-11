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

type State = Dictionary<string, User>

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
let userCreated state event =
    state

let nameChanged state event =
    state

let handleEvent (state : State) = function
    | UserCreated event -> userCreated state event
    | NameChanged event -> nameChanged state event

(* End Event Handlers *)

//Apply commands
let createUser (command : CreateUser) createGuid createTimestamp (state : State) =
    [ UserCreated {
        Id = createGuid()
        Name = command.Name
        Email = command.Email
        Password = command.Password
        CreationTime = createTimestamp()
    }]

let changeName (command : ChangeName) (state : State) =
    [NameChanged { Id = command.Id; Name = command.Name; }]

let handleCommand createGuid createTimestamp command state =
    match command with
        | CreateUser command -> createUser command createGuid createTimestamp state
        | ChangeName command -> changeName command state

let createEmailAddress (s:string) = 
    if Regex.IsMatch(s,@"^\S+@\S+\.\S+$") 
        then Some s
        else None