#r "packages/Fake/tools/FakeLib.dll"

open Fake
open Fake.ProcessTestRunner

let title = "JimAndCats"
let authors = [ "James Gregory" ]
let githubLink = "https://github.com/jamesjrg/JimAndCats"

Target "CleanBuildDebug" <| fun _ ->
    !! @"JimAndCats.sln"
    |> MSBuildDebug "" "Clean"
    |> Log "MsBuild"

Target "CleanBuildRelease" <| fun _ ->
    !! @"JimAndCats.sln"
    |> MSBuildRelease "" "Clean"
    |> Log "MsBuild"

Target "BuildDebug" <| fun _ ->
    !! @"JimAndCats.sln"
    |> MSBuildDebug "" "Build"
    |> Log "MsBuild"
    
Target "BuildRelease" <| fun _ ->
    !! @"JimAndCats.sln"
    |> MSBuildRelease "" "Build"
    |> Log "MsBuild"

Target "Test" <| fun _ ->
    [
        @"Cats.Tests\bin\Debug\Cats.Tests.exe"
        @"EventStore.YetAnotherClient.Tests\bin\Debug\EventStore.YetAnotherClient.Tests.exe"
        @"Jim.CommandHandler.Tests\bin\Debug\Jim.CommandHandler.Tests.exe"
        @"Jim.QueryHandler.Tests\bin\Debug\Jim.QueryHandler.Tests.exe"
        @"Pledges.Tests\bin\Debug\Pledges.Tests.exe"
        @"Suave.Extensions.Tests\bin\Debug\Suave.Extensions.Tests.exe"
    ]
    |> Seq.map (fun p -> (p, "")) //empty command line args
    |> RunConsoleTests (fun p -> p)

"CleanBuildDebug" ==> "BuildDebug"
"CleanBuildRelease" ==> "BuildRelease"
"BuildDebug" ==> "Test"

RunTargetOrDefault "BuildDebug"
