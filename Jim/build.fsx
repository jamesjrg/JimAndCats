#r "packages/Fake/tools/FakeLib.dll"

open Fake   

let title = "Jim"
let authors = [ "James Gregory" ]
let githubLink = "https://github.com/jamesjrg/JimAndCats"

Target "CleanBuild" <| fun _ ->
    CleanDir @"bin\Release"

Target "Build" <| fun _ ->
    !! "Jim.sln"
    |> MSBuildRelease @"bin\Release" "Build"
    |> Log "MsBuild"

Target "All" DoNothing

"CleanBuild" ==> "Build"

"Build" ==> "All"

RunTargetOrDefault "All"
