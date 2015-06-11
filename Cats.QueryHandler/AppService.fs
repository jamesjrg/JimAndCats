module Cats.QueryHandler.AppService

open System
open EventStore.YetAnotherClient
open NodaTime
open Suave.Extensions.Json

let getCat id =
     genericNotFound