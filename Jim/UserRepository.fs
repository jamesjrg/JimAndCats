module Jim.UserRepository

open Jim.UserModel
open System
open System.Collections.Generic

type Query =
    | ListUsers
    | GetUser of Guid

(* The in-memory query model - this might be a SQL database in a large system *)

type State = Dictionary<Guid, User>

let handleQuery (state:State) query (replyChannel:AsyncReplyChannel<User seq>) =
    match query with
    | ListUsers -> replyChannel.Reply(state.Values)
    | GetUser id -> replyChannel.Reply([state.[id]]) //TODO not yet added the web endpoint