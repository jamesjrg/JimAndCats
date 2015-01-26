namespace Jim.UserRepository

open Jim.UserModel
open System
open System.Collections.Generic

(* The in-memory query model - this might be a SQL database in a large system *)

type State = Dictionary<Guid, User>

type Repository() =
    let state = new State()

    member this.List() = state.Values

    member this.Get (id:Guid) =
        match state.TryGetValue(id) with
        | true, user -> Some user
        | false, _ -> None

    member this.Put (user:User) =
        state.[user.Id] <- user

    member this.Add (user:User) =
        state.[user.Id] <- user
        