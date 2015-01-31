module Cats.AppSettings

open ConfigMapping

type IAppSettings =
    abstract member PublicIdentityStream : string with get
    abstract member PrivateCatStream : string with get
    abstract member WriteToInMemoryStoreOnly : bool with get
    abstract member Port : int with get
    abstract member PrivateEventStoreIp : string with get
    abstract member PrivateEventStorePort : int with get
    abstract member IdentityEventStoreIp : string with get
    abstract member IdentityEventStorePort : int with get

let appSettings = ConfigMapper.Map<IAppSettings>();