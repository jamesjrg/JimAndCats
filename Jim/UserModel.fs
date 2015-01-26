module Jim.UserModel

open System
open NodaTime

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

let extractUsername (Username s) = s
let extractEmail (EmailAddress s) = s