module Jim.Encryption

open System.Security.Cryptography

(* based on https://cmatskas.com/-net-password-hashing-using-pbkdf2/ *)

let SALT_BYTE_SIZE = 16;
let HASH_BYTE_SIZE = 20; // to match the size of the PBKDF2-HMAC-SHA-1 hash
let PBKDF2_ITERATIONS = 128000; // OWASP recommendation 2014
let ITERATION_INDEX = 0;
let SALT_INDEX = 1;
let PBKDF2_INDEX = 2;

let PBKDF2Hash password =
    password