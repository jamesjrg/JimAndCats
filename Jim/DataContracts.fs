module Jim.DataContracts

open System
open System.Runtime.Serialization

[<DataContract>]
type CreateUser =
    { [<field:DataMember(Name = "name", IsRequired = true)>]
    name : string;

    [<field:DataMember(Name = "email", IsRequired = true)>]
    email : string;

    [<field:DataMember(Name = "password", IsRequired = true)>]
    password : string
    }
    

[<DataContract>]
type ChangeName =
    { [<field:DataMember(Name = "id", IsRequired = true)>]
    id : Guid;

    [<field:DataMember(Name = "name", IsRequired = true)>]
    name : string;
    }

[<DataContract>]
type ChangePassword =
    { [<field:DataMember(Name = "id", IsRequired = true)>]
    id : Guid;

    [<field:DataMember(Name = "password", IsRequired = true)>]
    password : string;
    }

[<DataContract>]
type JsonResponse =
    { [<field:DataMember(Name = "message", IsRequired = true)>]
    message : string;
    }