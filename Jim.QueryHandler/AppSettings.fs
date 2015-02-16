module Jim.QueryHandler.AppSettings

open ConfigMapping

type IAppSettings =
    abstract member Port : int with get

let appSettings = ConfigMapper.Map<IAppSettings>();