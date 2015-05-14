module Cats.QueryHandler.AppSettings

open ConfigMapping

type IAppSettings =
    abstract member PrivateCatStream : string with get
    abstract member WriteToInMemoryStoreOnly : bool with get
    abstract member Port : int with get
    abstract member PrivateEventStoreIp : string with get
    abstract member PrivateEventStorePort : int with get

let appSettings = ConfigMapper.Map<IAppSettings>();