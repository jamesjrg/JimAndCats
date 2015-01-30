module Jim.AppSettings

open ConfigMapping

type IAppSettings =
    abstract member PrivateIdentityStream : string with get
    abstract member UseEventStore : bool with get
    abstract member Port : int with get

let appSettings = ConfigMapper.Map<IAppSettings>();