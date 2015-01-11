﻿module Jim.Tests.EventStoreTests

open Jim.Domain
open Jim.Tests.Specifications
open NodaTime
open System
open Xunit

let guid1 = new Guid("3C71C09A-2902-4682-B8AB-663432C8867B")
let createdAtEpoch = new Instant(0L)
let createGuid = fun () -> guid1
let createTimestamp = fun () -> createdAtEpoch

[<Fact>]
let ``Should be able to create a user``() =
    Given []
    |> When ( CreateUser { Name="Bob Holness"; Email="bob.holness@itv.com"; Password="p4ssw0rd" } )
    |> Expect [ UserCreated { Id = guid1; Name="Bob Holness"; Email="bob.holness@itv.com"; Password="p4ssw0rd"; CreationTime = createdAtEpoch } ] createGuid createTimestamp