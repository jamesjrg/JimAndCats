namespace Jim.Domain

open MicroCQRS.Common.Result
open MicroCQRS.Common.CommandFailure
open System
open NodaTime
open System.Text.RegularExpressions

type Username = Username of string

type EmailAddress = EmailAddress of string

type PasswordHash = PasswordHash of string

type User = {
    Id: Guid
    Name: Username
    Email: EmailAddress
    PasswordHash: PasswordHash
    CreationTime: Instant
}

[<AutoOpen>]
module Extraction =
    let extractUsername (Username s) = s
    let extractEmail (EmailAddress s) = s
    let extractPasswordHash (PasswordHash s) = s

[<AutoOpen>]
module Email =
    let canonicalizeEmail (input:string) =
        input.Trim().ToLower()

    let createEmailAddress (s:string) =
        let canonicalized = canonicalizeEmail s
        if Regex.IsMatch(canonicalized, @"^\S+@\S+\.\S+$") 
            then Success (EmailAddress canonicalized)
            else Failure (BadRequest "Not a valid email address")
