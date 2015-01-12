module Jim.AppSettings

open ConfigMapping

type IAppSettings =
    abstract member UserStream : string with get
    abstract member UseEventStore : bool with get

let appSettings = ConfigMapper.Map<IAppSettings>();