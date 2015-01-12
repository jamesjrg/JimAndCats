module Jim.AppSettings

open ConfigMapping

type AppSettings = abstract member UserStream : string with get

let appSettings = ConfigMapper.Map<AppSettings>();