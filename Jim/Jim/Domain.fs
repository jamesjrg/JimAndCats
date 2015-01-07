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
    Id: Guid
    Name: string
    Email: string
    Password: string
}

and ChangeName = {
    Name: string
    Id: Guid
}

//Events

type Event =
    | UserCreated of UserCreated
    | NameChanged of NameChanged
    
and UserCreated = {
    Id: Guid
    Name: string
    Email: string
    Password: string
}

and NameChanged = {
    Id: Guid
    Name: string
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
    [UserCreated {Id = command.Id; Name = command.Name; Email = command.Email; Password = command.Password; }]

let changeName (command : ChangeName) (state : State) =
    [NameChanged { Id = command.Id; Name = command.Name; }]

let handleCommand = function
    | CreateUser command -> createUser command
    | ChangeName command -> changeName command