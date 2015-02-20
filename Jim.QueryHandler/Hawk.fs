﻿module Jim.QueryHandler.Hawk

open Jim.Domain
open GenericErrorHandling
open logibit.hawk
open logibit.hawk.Server
open logibit.hawk.Types

let hawkSettings (userRepository:IUserRepository) =
    { Settings.empty<User>() with
        credsRepo = fun id ->
           match createEmailAddress id with
           | Success email ->
                let! user = userRepository.GetByEmail(email)
                match user with
                | Some user ->
                    Choice1Of2 (
                        {   id = extractEmail user.Email
                            key = extractPasswordHash user.PasswordHash
                            algorithm = SHA256 }, user)
                | None -> Choice2Of2 CredsError.CredentialsNotFound
           | Failure (BadRequest f) -> Choice2Of2 (CredsError.Other "Not a valid email address")
           | _ -> Choice2Of2 (CredsError.Other "Internal server error finding credentials")
    }