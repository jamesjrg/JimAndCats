module Jim.Hawk

open Jim.Domain
open MicroCQRS.Common.CommandFailure
open MicroCQRS.Common.Result
open logibit.hawk
open logibit.hawk.Server
open logibit.hawk.Types

(* FIXME this shouldn't need to match types of command failures *)
let hawkSettings (userRepository:IUserRepository) =
    { Settings.empty<User>() with
     // sign: UserId -> Choice<Credentials * 'a, CredsError>
        creds_repo = fun id ->
           match createEmailAddress id with
           | Success email ->
                match userRepository.GetByEmail(email) with
                | Some user ->
                    Choice1Of2 (
                        {   id = extractEmail user.Email
                            key = extractPasswordHash user.PasswordHash
                            algorithm = SHA256 }, user)
                | None -> Choice2Of2 CredentialsNotFound
           | Failure (BadRequest f) -> Choice2Of2 (CredsError.Other "Not a valid email address")
           | _ -> Choice2Of2 (CredsError.Other "Internal server error finding credentials")
    }