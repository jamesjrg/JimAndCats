module Jim.Tests.AppSettings

open ConfigMapping

type IAppSettings =
    abstract member WriteToInMemoryStoreOnly : bool with get
    abstract member PrivateEventStoreIp : string with get
    abstract member PrivateEventStorePort : int with get

let appSettings = ConfigMapper.Map<IAppSettings>();