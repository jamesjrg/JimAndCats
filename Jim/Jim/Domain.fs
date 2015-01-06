module Jim.Domain

open System
open System.Collections.Generic

//Domain model

type User = {
    Id: Guid
    Name: string
    Email: string
    Password: string
}

type State = Dictionary<string, User>

//Commands

type Command =
    | CreateUser of CreateUser
    | ChangeName of ChangeName

and CreateUser = {
    Name: string
    Email: string
    Password: string
}

and ChangeName = {
    Id: Guid
}

//Events

type Event =
    | UserCreated of UserCreated
    | NameChanged of NameChanged
    
and UserCreated = {
    Id: Guid
}

and NameChanged = {
    Id: Guid
}

//Handle events
let userCreated state event =
    state

let nameChanged state event =
    state

let handleEvent (state : State) = function
    | UserCreated event -> userCreated state event
    | NameChanged event -> nameChanged state event

//Apply commands
let createUser (command : CreateUser) (state : State) =
    [UserCreated {Id = Guid.NewGuid() }]

let changeName (command : ChangeName) (state : State) =
    [NameChanged { Id = command.Id }]

let handleCommand = function
    | CreateUser command -> createUser command
    | ChangeName command -> changeName command