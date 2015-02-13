module Jim.QueryHandler.Logging

open Logary
open Logary.Configuration
open Logary.Targets
open Logary.Metrics

let logary =
    withLogary' "Jim" (
      withTargets [
        Console.create Console.empty "console"
        Debugger.create Debugger.empty "debugger"
      ] >>
      withRules [
        Rule.createForTarget "console"
        Rule.createForTarget "debugger"
      ]
    )