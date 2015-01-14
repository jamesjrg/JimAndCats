module Jim.Tests.Domain

open Jim.Domain
open Jim.Tests.Specifications
open NodaTime
open System
open Xunit

let createGuid1 () = new Guid("3C71C09A-2902-4682-B8AB-663432C8867B")
let guid1 = createGuid1()

let createEpoch () = new Instant(0L)
let epoch = createEpoch()

[<Fact>]
let ``Should be able to create a user``() =
    let expectPartial = Expect createGuid1 createEpoch
    Given []
    |> When ( CreateUser { Name="Bob Holness"; Email="bob.holness@itv.com"; Password="p4ssw0rd" } )
    |> expectPartial [ UserCreated { Id = guid1; Name="Bob Holness"; Email="bob.holness@itv.com"; Password="p4ssw0rd"; CreationTime = epoch } ]