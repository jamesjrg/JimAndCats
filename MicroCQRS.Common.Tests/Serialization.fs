﻿module MicroCQRS.Common.Tests.Serialization

open MicroCQRS.Common.Serialization

open Newtonsoft.Json
open System.IO
open Fuchu
open Swensen.Unquote.Assertions

type WrapperUnion = WrapperUnion of string

type TestEvent =
        | TestUnionCase1 of TestUnionCase1
        | TestUnionCase2 of TestUnionCase2
    
    and TestUnionCase1 = {
        IntField: int
        WrapperField: WrapperUnion
    }

    and TestUnionCase2 = {
        IntField: int
        WrapperField: WrapperUnion
    }

[<Struct>]
type Digit =
    val value: int
    new(value) =
        if value < 0 || value > 9 then 
            invalidArg "value" "A digit value should be from 0 to 9" 
        { value = value }
    override this.ToString() = string this.value 

let createSerializer converters =
    let serializer = JsonSerializer()
    converters |> List.iter serializer.Converters.Add
    serializer

let serialize converters o = 
    let serializer = createSerializer converters
    use w = new StringWriter()
    serializer.Serialize(w, o)
    w.ToString()

let deserialize<'a> converters s =
    let serializer = createSerializer converters
    use r = new StringReader(s)
    serializer.Deserialize<'a>(new JsonTextReader(r))

[<Tests>]
let tests =
    testList "Serialization tests"
        [
            (* this functionality never gets used, could maybe just remove it... *)
            testCase "a value should be serialized as its content" (fun () ->            
                let converters = [ valueConverter typeof<Digit> ]
                let actual = serialize converters <| Digit 7
                "7" =? actual)

            (* this functionality never gets used, could maybe just remove it... *)
            testCase "a value should be deserialized from its content" (fun () ->            
                let converters = [ valueConverter typeof<Digit> ]    
                let actual = deserialize<Digit> converters "7"
                Digit 7 =? actual)

            testCase "a single case union should be serialized as its content" (fun () ->            
                let converters = [ unionConverter ]
                let actual = serialize converters <| WrapperUnion "testing testing"
                "testing testing" =? actual)

            testCase "a single case union should be deserialized from its content" (fun () ->            
                let converters = [ unionConverter ]    
                let actual = deserialize<WrapperUnion> converters "testing testing"
                WrapperUnion "testing testing" =? actual)
        ]