module Jim.DataContracts

open System.Runtime.Serialization

[<DataContract>]
type RenameRequest =
    { [<field:DataMember(Name = "name")>]
    name : string
    }