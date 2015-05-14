module Jim.CommandHandler.Logging

open Logary
open Logary.Configuration
open Logary.Targets
open Logary.Metrics

let logary =
    withLogary' "Cats.QueryHandler" (
      withTargets [
        Console.create Console.empty "console"
        Debugger.create Debugger.empty "debugger"
      ] >>
      withRules [
        Rule.createForTarget "console"
        Rule.createForTarget "debugger"
      ]
    )