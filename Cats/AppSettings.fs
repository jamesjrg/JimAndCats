﻿module Cats.AppSettings

open ConfigMapping

type IAppSettings =
    abstract member PublicIdentityStream : string with get
    abstract member PrivateCatStream : string with get
    abstract member UseEventStore : bool with get
    abstract member Port : int with get

let appSettings = ConfigMapper.Map<IAppSettings>();