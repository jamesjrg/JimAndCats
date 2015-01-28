module Jim.Domain.Hashing

open System
open System.Security.Cryptography

(* based on:
https://cmatskas.com/-net-password-hashing-using-pbkdf2/
*)

let SALT_BYTE_SIZE = 16 //http://security.stackexchange.com/questions/17994/with-pbkdf2-what-is-an-optimal-hash-size-in-bytes-what-about-the-size-of-the-s recommends 128 bit hash
let HASH_BYTE_SIZE = 20 // to match the size of the PBKDF2-HMAC-SHA-1 hash
let PBKDF2_ITERATIONS = 128000 // OWASP recommendation 2014
let ITERATION_INDEX = 0
let SALT_INDEX = 1
let PBKDF2_INDEX = 2

let private PBKDF2 password (salt: Byte array) iterations outputBytes =
    let pbkdf2 = new Rfc2898DeriveBytes(password, salt, IterationCount = iterations)
    pbkdf2.GetBytes(outputBytes)

let PBKDF2Hash password =
    let cryptoProvider = new RNGCryptoServiceProvider()
    let salt = Array.zeroCreate SALT_BYTE_SIZE
    cryptoProvider.GetBytes(salt)

    let hash = PBKDF2 password salt PBKDF2_ITERATIONS HASH_BYTE_SIZE
    PBKDF2_ITERATIONS.ToString() + ":" +
        Convert.ToBase64String(salt) + ":" +
        Convert.ToBase64String(hash)

let private slowEquals (a:byte[]) (b:byte[]) =
    let mutable diff = (uint32 a.Length) ^^^ (uint32 b.Length);

    let len = (if a.Length < b.Length then a.Length else b.Length) - 1
    for i in 0 .. len do
        diff <- diff ^^^ (uint32 a.[i]) ^^^ (uint32 b.[i])

    diff = 0u

let validatePassword password (expectedHash:string) =
    let split = expectedHash.Split ':'
    let iterations = Int32.Parse(split.[ITERATION_INDEX]);
    let salt = Convert.FromBase64String(split.[SALT_INDEX]);
    let hash = Convert.FromBase64String(split.[PBKDF2_INDEX]);

    let testHash = PBKDF2 password salt iterations hash.Length

    slowEquals hash testHash