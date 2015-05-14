module Jim.DuplicateEmailChecker.AppSettings

open ConfigMapping

type IAppSettings =
    abstract member PublicIdentityStream : string with get
    abstract member PublicEventStoreIp : string with get
    abstract member PublicEventStorePort : int with get

let appSettings = ConfigMapper.Map<IAppSettings>();