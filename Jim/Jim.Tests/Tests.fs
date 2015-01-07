module Jim.Tests.EventStoreTests

open Jim.Domain
open Jim.Tests.Specifications
open System
open Xunit

[<Fact>]
let ``Should be able to create a user``() =
    let guid = Guid.NewGuid()
    Given []
    |> When ( CreateUser { Id = guid; Name="Bob Holness"; Email="bob.holness@itv.com"; Password="p4ssw0rd" } )
    |> Expect [ UserCreated { Id = guid; Name="Bob Holness"; Email="bob.holness@itv.com"; Password="p4ssw0rd" } ]