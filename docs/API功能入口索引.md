# API 功能入口索引

> 源码快照：2026-10-12，基于 `a45d1415d4` 的弃用 API 与直播/DVR 移除后工作区。配套阅读：[开发者功能手册](开发者功能手册.md)。

覆盖 `Jellyfin.Api/Controllers` 的 55 个控制器及内置 `TmdbController`，合计 **56 个控制器、324 条 HTTP 方法与路由声明**。同一动作的兼容路由分别计数；这不是业务功能数量，也不是实际运行实例的 OpenAPI 端点数量。

本索引从控制器属性静态整理，保留方法名称以便检索实现。参数、DTO、响应码及最终权限判断请阅读链接源码或运行实例的 `/api-docs/openapi.json`；接口可能另有方法体内的用户、设备、路径或资源权限检查。

路由已合并控制器前缀和动作模板，但未添加部署时的 `BaseUrl`；表中 `{...}` 是路径参数。兼容别名和隐藏端点照常列出，不代表建议新客户端使用它们。外装插件增加的路由、WebSocket、健康检查和启动临时服务另见主手册。

## 控制器目录

| 控制器 | 功能域 | 路由声明数 |
| --- | --- | ---: |
| [ActivityLogController](#activitylogcontroller) | 活动审计记录 | 1 |
| [ApiKeyController](#apikeycontroller) | API 密钥管理 | 3 |
| [AudioController](#audiocontroller) | 音频流 | 4 |
| [BackupController](#backupcontroller) | 备份与恢复 | 4 |
| [BrandingController](#brandingcontroller) | 品牌配置与样式 | 3 |
| [ClientLogController](#clientlogcontroller) | 客户端日志上传 | 1 |
| [CollectionController](#collectioncontroller) | 合集管理 | 3 |
| [ConfigurationController](#configurationcontroller) | 服务器及命名配置 | 6 |
| [DashboardController](#dashboardcontroller) | 插件配置页面 | 2 |
| [DevicesController](#devicescontroller) | 客户端设备管理 | 5 |
| [DisplayPreferencesController](#displaypreferencescontroller) | 显示偏好 | 2 |
| [DynamicHlsController](#dynamichlscontroller) | 动态 HLS 播放清单与分片 | 9 |
| [EnvironmentController](#environmentcontroller) | 服务器文件系统浏览与路径验证 | 5 |
| [FilterController](#filtercontroller) | 可选筛选条件 | 2 |
| [GenresController](#genrescontroller) | 媒体类型标签 | 2 |
| [HlsSegmentController](#hlssegmentcontroller) | HLS 编码分片与相关资源 | 5 |
| [ImageController](#imagecontroller) | 本地图片读取与管理 | 37 |
| [InstantMixController](#instantmixcontroller) | 即时音乐混播 | 5 |
| [ItemLookupController](#itemlookupcontroller) | 远程元数据搜索与匹配 | 11 |
| [ItemRefreshController](#itemrefreshcontroller) | 条目元数据刷新 | 1 |
| [ItemUpdateController](#itemupdatecontroller) | 条目编辑与内容类型 | 3 |
| [ItemsController](#itemscontroller) | 条目查询与用户观看数据 | 4 |
| [LibraryController](#librarycontroller) | 媒体库内容维护及关联查询 | 25 |
| [LibraryStructureController](#librarystructurecontroller) | 虚拟媒体库与目录配置 | 8 |
| [LocalizationController](#localizationcontroller) | 语言、地区与分级字典 | 4 |
| [LyricsController](#lyricscontroller) | 歌词读取与管理 | 6 |
| [MediaInfoController](#mediainfocontroller) | 播放信息协商与动态媒体源生命周期 | 5 |
| [MediaSegmentsController](#mediasegmentscontroller) | 媒体片段查询 | 1 |
| [MoviesController](#moviescontroller) | 电影推荐分组 | 1 |
| [PackageController](#packagecontroller) | 插件包、仓库与安装任务 | 6 |
| [PersonsController](#personscontroller) | 演职人员 | 2 |
| [PlaylistsController](#playlistscontroller) | 播放列表及成员权限 | 11 |
| [PlaystateController](#playstatecontroller) | 播放状态上报及已观看标记 | 6 |
| [PluginsController](#pluginscontroller) | 已安装插件及配置 | 9 |
| [QuickConnectController](#quickconnectcontroller) | 跨设备登录授权 | 4 |
| [RemoteImageController](#remoteimagecontroller) | 远程图片发现与下载 | 3 |
| [ScheduledTasksController](#scheduledtaskscontroller) | 计划任务、执行与触发器 | 5 |
| [SearchController](#searchcontroller) | 媒体搜索提示 | 1 |
| [SessionController](#sessioncontroller) | 会话、客户端能力与远程控制 | 16 |
| [StartupController](#startupcontroller) | 首次启动配置 | 2 |
| [StudiosController](#studioscontroller) | 工作室分类 | 2 |
| [SubtitleController](#subtitlecontroller) | 字幕搜索、上传、提取与转换 | 10 |
| [SuggestionsController](#suggestionscontroller) | 建议内容 | 1 |
| [SyncPlayController](#syncplaycontroller) | 同步观看组及队列控制 | 22 |
| [SystemController](#systemcontroller) | 服务器状态、日志及进程控制 | 10 |
| [TimeSyncController](#timesynccontroller) | 同步观看时钟校准 | 1 |
| [TrickplayController](#trickplaycontroller) | 进度预览图与 HLS 图块清单 | 2 |
| [TvShowsController](#tvshowscontroller) | 剧集、季、下一集与即将播出 | 4 |
| [UniversalAudioController](#universalaudiocontroller) | 客户端音频格式协商 | 2 |
| [UserController](#usercontroller) | 用户、认证、配置和权限 | 14 |
| [UserLibraryController](#userlibrarycontroller) | 用户视角条目、收藏与附件内容 | 10 |
| [UserViewsController](#userviewscontroller) | 用户媒体视图 | 2 |
| [VideoAttachmentsController](#videoattachmentscontroller) | 视频内嵌附件 | 1 |
| [VideosController](#videoscontroller) | 视频流、多版本与附加片段 | 7 |
| [YearsController](#yearscontroller) | 年份分类 | 2 |
| [TmdbController](#tmdbcontroller) | 内置 TMDb 客户端配置 | 1 |

## ActivityLogController

活动审计记录。源码：[ActivityLogController.cs](../Jellyfin.Api/Controllers/ActivityLogController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/System/ActivityLog/Entries` | `GetLogEntries`（L57） | — |

## ApiKeyController

API 密钥管理。源码：[ApiKeyController.cs](../Jellyfin.Api/Controllers/ApiKeyController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Auth/Keys` | `GetKeys`（L41） | — |
| `POST` | `/Auth/Keys` | `CreateKey`（L57） | — |
| `DELETE` | `/Auth/Keys/{key}` | `RevokeKey`（L73） | — |

## AudioController

音频流。源码：[AudioController.cs](../Jellyfin.Api/Controllers/AudioController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Audio/{itemId}/stream` | `GetAudioStream`（L92） | — |
| `HEAD` | `/Audio/{itemId}/stream` | `GetAudioStream`（L92） | — |
| `GET` | `/Audio/{itemId}/stream.{container}` | `GetAudioStreamByContainer`（L256） | — |
| `HEAD` | `/Audio/{itemId}/stream.{container}` | `GetAudioStreamByContainer`（L256） | — |

## BackupController

备份与恢复。源码：[BackupController.cs](../Jellyfin.Api/Controllers/BackupController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `POST` | `/Backup/Create` | `CreateBackup`（L45） | — |
| `POST` | `/Backup/Restore` | `StartRestoreBackup`（L61） | — |
| `GET` | `/Backup` | `ListBackups`（L82） | — |
| `GET` | `/Backup/Manifest` | `GetBackup`（L101） | — |

## BrandingController

品牌配置与样式。源码：[BrandingController.cs](../Jellyfin.Api/Controllers/BrandingController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Branding/Configuration` | `GetBrandingOptions`（L32） | — |
| `GET` | `/Branding/Css` | `GetBrandingCss`（L60） | — |
| `GET` | `/Branding/Css.css` | `GetBrandingCss`（L60） | — |

## ClientLogController

客户端日志上传。源码：[ClientLogController.cs](../Jellyfin.Api/Controllers/ClientLogController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `POST` | `/ClientLog/Document` | `LogFile`（L51） | — |

## CollectionController

合集管理。源码：[CollectionController.cs](../Jellyfin.Api/Controllers/CollectionController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `POST` | `/Collections` | `CreateCollection`（L51） | — |
| `POST` | `/Collections/{collectionId}/Items` | `AddToCollection`（L87） | — |
| `DELETE` | `/Collections/{collectionId}/Items` | `RemoveFromCollection`（L104） | — |

## ConfigurationController

服务器及命名配置。源码：[ConfigurationController.cs](../Jellyfin.Api/Controllers/ConfigurationController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/System/Configuration` | `GetConfiguration`（L51） | — |
| `POST` | `/System/Configuration` | `UpdateConfiguration`（L65） | — |
| `GET` | `/System/Configuration/{key}` | `GetNamedConfiguration`（L80） | — |
| `POST` | `/System/Configuration/{key}` | `UpdateNamedConfiguration`（L95） | — |
| `GET` | `/System/Configuration/MetadataOptions/Default` | `GetDefaultMetadataOptions`（L117） | — |
| `POST` | `/System/Configuration/Branding` | `UpdateBrandingConfiguration`（L131） | — |

## DashboardController

插件配置页面。源码：[DashboardController.cs](../Jellyfin.Api/Controllers/DashboardController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/web/ConfigurationPages` | `GetConfigurationPages`（L53） | — |
| `GET` | `/web/ConfigurationPage` | `GetDashboardConfigurationPage`（L77） | — |

## DevicesController

客户端设备管理。源码：[DevicesController.cs](../Jellyfin.Api/Controllers/DevicesController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Devices` | `GetDevices`（L53） | — |
| `GET` | `/Devices/Info` | `GetDeviceInfo`（L69） | — |
| `GET` | `/Devices/Options` | `GetDeviceOptions`（L90） | — |
| `POST` | `/Devices/Options` | `UpdateDeviceOptions`（L110） | — |
| `DELETE` | `/Devices` | `DeleteDevice`（L128） | — |

## DisplayPreferencesController

显示偏好。源码：[DisplayPreferencesController.cs](../Jellyfin.Api/Controllers/DisplayPreferencesController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/DisplayPreferences/{displayPreferencesId}` | `GetDisplayPreferences`（L51） | — |
| `POST` | `/DisplayPreferences/{displayPreferencesId}` | `UpdateDisplayPreferences`（L115） | — |

## DynamicHlsController

动态 HLS 播放清单与分片。源码：[DynamicHlsController.cs](../Jellyfin.Api/Controllers/DynamicHlsController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Videos/{itemId}/live.m3u8` | `GetLiveHlsStream`（L167） | 隐藏于 OpenAPI |
| `GET` | `/Videos/{itemId}/master.m3u8` | `GetMasterHlsVideoPlaylist`（L408） | 隐藏于 OpenAPI |
| `HEAD` | `/Videos/{itemId}/master.m3u8` | `GetMasterHlsVideoPlaylist`（L408） | 隐藏于 OpenAPI |
| `GET` | `/Audio/{itemId}/master.m3u8` | `GetMasterHlsAudioPlaylist`（L581） | 隐藏于 OpenAPI |
| `HEAD` | `/Audio/{itemId}/master.m3u8` | `GetMasterHlsAudioPlaylist`（L581） | 隐藏于 OpenAPI |
| `GET` | `/Videos/{itemId}/main.m3u8` | `GetVariantHlsVideoPlaylist`（L748） | 隐藏于 OpenAPI |
| `GET` | `/Audio/{itemId}/main.m3u8` | `GetVariantHlsAudioPlaylist`（L917） | 隐藏于 OpenAPI |
| `GET` | `/Videos/{itemId}/hls1/{playlistId}/{segmentId}.{container}` | `GetHlsVideoSegment`（L1090） | 隐藏于 OpenAPI |
| `GET` | `/Audio/{itemId}/hls1/{playlistId}/{segmentId}.{container}` | `GetHlsAudioSegment`（L1272） | 隐藏于 OpenAPI |

## EnvironmentController

服务器文件系统浏览与路径验证。源码：[EnvironmentController.cs](../Jellyfin.Api/Controllers/EnvironmentController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Environment/DirectoryContents` | `GetDirectoryContents`（L50） | — |
| `POST` | `/Environment/ValidatePath` | `ValidatePath`（L79） | — |
| `GET` | `/Environment/Drives` | `GetDrives`（L137） | — |
| `GET` | `/Environment/ParentPath` | `GetParentPath`（L149） | — |
| `GET` | `/Environment/DefaultDirectoryBrowser` | `GetDefaultDirectoryBrowser`（L178） | — |

## FilterController

可选筛选条件。源码：[FilterController.cs](../Jellyfin.Api/Controllers/FilterController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Items/Filters` | `GetQueryFiltersLegacy`（L55） | — |
| `GET` | `/Items/Filters2` | `GetQueryFilters`（L112） | — |

## GenresController

媒体类型标签。源码：[GenresController.cs](../Jellyfin.Api/Controllers/GenresController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Genres` | `GetGenres`（L76） | — |
| `GET` | `/Genres/{genreName}` | `GetGenre`（L164） | — |

## HlsSegmentController

HLS 编码分片与相关资源。源码：[HlsSegmentController.cs](../Jellyfin.Api/Controllers/HlsSegmentController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Audio/{itemId}/hls/{segmentId}/stream.mp3` | `GetHlsAudioSegmentLegacy`（L60） | 隐藏于 OpenAPI |
| `GET` | `/Audio/{itemId}/hls/{segmentId}/stream.aac` | `GetHlsAudioSegmentLegacy`（L60） | 隐藏于 OpenAPI |
| `GET` | `/Videos/{itemId}/hls/{playlistId}/stream.m3u8` | `GetHlsPlaylistLegacy`（L84） | 隐藏于 OpenAPI |
| `DELETE` | `/Videos/ActiveEncodings` | `StopEncodingProcess`（L106） | 隐藏于 OpenAPI |
| `GET` | `/Videos/{itemId}/hls/{playlistId}/{segmentId}.{segmentContainer}` | `GetHlsVideoSegmentLegacy`（L131） | 隐藏于 OpenAPI |

## ImageController

本地图片读取与管理。源码：[ImageController.cs](../Jellyfin.Api/Controllers/ImageController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `POST` | `/UserImage` | `PostUserImage`（L103） | — |
| `DELETE` | `/UserImage` | `DeleteUserImage`（L162） | — |
| `DELETE` | `/Items/{itemId}/Images/{imageType}` | `DeleteItemImage`（L208） | — |
| `DELETE` | `/Items/{itemId}/Images/{imageType}/{imageIndex}` | `DeleteItemImageByIndex`（L236） | — |
| `POST` | `/Items/{itemId}/Images/{imageType}` | `SetItemImage`（L266） | — |
| `POST` | `/Items/{itemId}/Images/{imageType}/{imageIndex}` | `SetItemImageByIndex`（L309） | — |
| `POST` | `/Items/{itemId}/Images/{imageType}/{imageIndex}/Index` | `UpdateItemImageIndex`（L351） | — |
| `GET` | `/Items/{itemId}/Images` | `GetItemImageInfos`（L378） | — |
| `GET` | `/Items/{itemId}/Images/{imageType}` | `GetItemImage`（L464） | — |
| `HEAD` | `/Items/{itemId}/Images/{imageType}` | `GetItemImage`（L464） | — |
| `GET` | `/Items/{itemId}/Images/{imageType}/{imageIndex}` | `GetItemImageByIndex`（L542） | — |
| `HEAD` | `/Items/{itemId}/Images/{imageType}/{imageIndex}` | `GetItemImageByIndex`（L542） | — |
| `GET` | `/Items/{itemId}/Images/{imageType}/{imageIndex}/{tag}/{format}/{maxWidth}/{maxHeight}/{percentPlayed}/{unplayedCount}` | `GetItemImage2`（L620） | — |
| `HEAD` | `/Items/{itemId}/Images/{imageType}/{imageIndex}/{tag}/{format}/{maxWidth}/{maxHeight}/{percentPlayed}/{unplayedCount}` | `GetItemImage2`（L620） | — |
| `GET` | `/Artists/{name}/Images/{imageType}/{imageIndex}` | `GetArtistImage`（L698） | — |
| `HEAD` | `/Artists/{name}/Images/{imageType}/{imageIndex}` | `GetArtistImage`（L698） | — |
| `GET` | `/Genres/{name}/Images/{imageType}` | `GetGenreImage`（L776） | — |
| `HEAD` | `/Genres/{name}/Images/{imageType}` | `GetGenreImage`（L776） | — |
| `GET` | `/Genres/{name}/Images/{imageType}/{imageIndex}` | `GetGenreImageByIndex`（L854） | — |
| `HEAD` | `/Genres/{name}/Images/{imageType}/{imageIndex}` | `GetGenreImageByIndex`（L854） | — |
| `GET` | `/MusicGenres/{name}/Images/{imageType}` | `GetMusicGenreImage`（L932） | — |
| `HEAD` | `/MusicGenres/{name}/Images/{imageType}` | `GetMusicGenreImage`（L932） | — |
| `GET` | `/MusicGenres/{name}/Images/{imageType}/{imageIndex}` | `GetMusicGenreImageByIndex`（L1010） | — |
| `HEAD` | `/MusicGenres/{name}/Images/{imageType}/{imageIndex}` | `GetMusicGenreImageByIndex`（L1010） | — |
| `GET` | `/Persons/{name}/Images/{imageType}` | `GetPersonImage`（L1088） | — |
| `HEAD` | `/Persons/{name}/Images/{imageType}` | `GetPersonImage`（L1088） | — |
| `GET` | `/Persons/{name}/Images/{imageType}/{imageIndex}` | `GetPersonImageByIndex`（L1166） | — |
| `HEAD` | `/Persons/{name}/Images/{imageType}/{imageIndex}` | `GetPersonImageByIndex`（L1166） | — |
| `GET` | `/Studios/{name}/Images/{imageType}` | `GetStudioImage`（L1244） | — |
| `HEAD` | `/Studios/{name}/Images/{imageType}` | `GetStudioImage`（L1244） | — |
| `GET` | `/Studios/{name}/Images/{imageType}/{imageIndex}` | `GetStudioImageByIndex`（L1322） | — |
| `HEAD` | `/Studios/{name}/Images/{imageType}/{imageIndex}` | `GetStudioImageByIndex`（L1322） | — |
| `GET` | `/UserImage` | `GetUserImage`（L1388） | — |
| `HEAD` | `/UserImage` | `GetUserImage`（L1388） | — |
| `GET` | `/Branding/Splashscreen` | `GetSplashscreen`（L1445） | — |
| `POST` | `/Branding/Splashscreen` | `UploadCustomSplashscreen`（L1522） | — |
| `DELETE` | `/Branding/Splashscreen` | `DeleteCustomSplashscreen`（L1556） | — |

## InstantMixController

即时音乐混播。源码：[InstantMixController.cs](../Jellyfin.Api/Controllers/InstantMixController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Songs/{itemId}/InstantMix` | `GetInstantMixFromSong`（L71） | — |
| `GET` | `/Albums/{itemId}/InstantMix` | `GetInstantMixFromAlbum`（L114） | — |
| `GET` | `/Playlists/{itemId}/InstantMix` | `GetInstantMixFromPlaylist`（L157） | — |
| `GET` | `/Artists/{itemId}/InstantMix` | `GetInstantMixFromArtists`（L200） | — |
| `GET` | `/Items/{itemId}/InstantMix` | `GetInstantMixFromItem`（L243） | — |

## ItemLookupController

远程元数据搜索与匹配。源码：[ItemLookupController.cs](../Jellyfin.Api/Controllers/ItemLookupController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Items/{itemId}/ExternalIdInfos` | `GetExternalIdInfos`（L68） | — |
| `POST` | `/Items/RemoteSearch/Movie` | `GetMovieRemoteSearchResults`（L89） | — |
| `POST` | `/Items/RemoteSearch/Trailer` | `GetTrailerRemoteSearchResults`（L106） | — |
| `POST` | `/Items/RemoteSearch/MusicVideo` | `GetMusicVideoRemoteSearchResults`（L123） | — |
| `POST` | `/Items/RemoteSearch/Series` | `GetSeriesRemoteSearchResults`（L140） | — |
| `POST` | `/Items/RemoteSearch/BoxSet` | `GetBoxSetRemoteSearchResults`（L157） | — |
| `POST` | `/Items/RemoteSearch/MusicArtist` | `GetMusicArtistRemoteSearchResults`（L174） | — |
| `POST` | `/Items/RemoteSearch/MusicAlbum` | `GetMusicAlbumRemoteSearchResults`（L191） | — |
| `POST` | `/Items/RemoteSearch/Person` | `GetPersonRemoteSearchResults`（L209） | — |
| `POST` | `/Items/RemoteSearch/Book` | `GetBookRemoteSearchResults`（L226） | — |
| `POST` | `/Items/RemoteSearch/Apply/{itemId}` | `ApplySearchCriteria`（L249） | — |

## ItemRefreshController

条目元数据刷新。源码：[ItemRefreshController.cs](../Jellyfin.Api/Controllers/ItemRefreshController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `POST` | `/Items/{itemId}/Refresh` | `RefreshItem`（L62） | — |

## ItemUpdateController

条目编辑与内容类型。源码：[ItemUpdateController.cs](../Jellyfin.Api/Controllers/ItemUpdateController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `POST` | `/Items/{itemId}` | `UpdateItem`（L74） | — |
| `GET` | `/Items/{itemId}/MetadataEditor` | `GetMetadataEditorInfo`（L147） | — |
| `POST` | `/Items/{itemId}/ContentType` | `UpdateItemContentType`（L207） | — |

## ItemsController

条目查询与用户观看数据。源码：[ItemsController.cs](../Jellyfin.Api/Controllers/ItemsController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Items` | `GetItems`（L173） | — |
| `GET` | `/UserItems/Resume` | `GetResumeItems`（L654） | — |
| `GET` | `/UserItems/{itemId}/UserData` | `GetItemUserData`（L750） | — |
| `POST` | `/UserItems/{itemId}/UserData` | `UpdateItemUserData`（L788） | — |

## LibraryController

媒体库内容维护及关联查询。源码：[LibraryController.cs](../Jellyfin.Api/Controllers/LibraryController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Items/{itemId}/File` | `GetFile`（L114） | — |
| `GET` | `/Items/{itemId}/ThemeSongs` | `GetThemeSongs`（L151） | — |
| `GET` | `/Items/{itemId}/ThemeVideos` | `GetThemeVideos`（L225） | — |
| `GET` | `/Items/{itemId}/ThemeMedia` | `GetThemeMedia`（L297） | — |
| `POST` | `/Library/Refresh` | `RefreshLibrary`（L340） | — |
| `DELETE` | `/Items/{itemId}` | `DeleteItem`（L367） | — |
| `DELETE` | `/Items` | `DeleteItems`（L411） | — |
| `GET` | `/Items/Counts` | `GetItemCounts`（L456） | — |
| `GET` | `/Items/{itemId}/Ancestors` | `GetAncestors`（L491） | — |
| `GET` | `/Library/PhysicalPaths` | `GetPhysicalPaths`（L535） | — |
| `GET` | `/Library/MediaFolders` | `GetMediaFolders`（L550） | — |
| `POST` | `/Library/Series/Added` | `PostUpdatedSeries`（L580） | — |
| `POST` | `/Library/Series/Updated` | `PostUpdatedSeries`（L580） | — |
| `POST` | `/Library/Movies/Added` | `PostUpdatedMovies`（L610） | — |
| `POST` | `/Library/Movies/Updated` | `PostUpdatedMovies`（L610） | — |
| `POST` | `/Library/Media/Updated` | `PostUpdatedMedia`（L651） | — |
| `GET` | `/Items/{itemId}/Download` | `GetDownload`（L674） | — |
| `GET` | `/Items/{itemId}/Collections` | `GetItemCollections`（L739） | — |
| `GET` | `/Artists/{itemId}/Similar` | `GetSimilarItems`（L808） | — |
| `GET` | `/Items/{itemId}/Similar` | `GetSimilarItems`（L808） | — |
| `GET` | `/Albums/{itemId}/Similar` | `GetSimilarItems`（L808） | — |
| `GET` | `/Shows/{itemId}/Similar` | `GetSimilarItems`（L808） | — |
| `GET` | `/Movies/{itemId}/Similar` | `GetSimilarItems`（L808） | — |
| `GET` | `/Trailers/{itemId}/Similar` | `GetSimilarItems`（L808） | — |
| `GET` | `/Libraries/AvailableOptions` | `GetLibraryOptionsInfo`（L867） | — |

## LibraryStructureController

虚拟媒体库与目录配置。源码：[LibraryStructureController.cs](../Jellyfin.Api/Controllers/LibraryStructureController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Library/VirtualFolders` | `GetVirtualFolders`（L74） | — |
| `POST` | `/Library/VirtualFolders` | `AddVirtualFolder`（L91） | — |
| `DELETE` | `/Library/VirtualFolders` | `RemoveVirtualFolder`（L122） | — |
| `POST` | `/Library/VirtualFolders/Name` | `RenameVirtualFolder`（L149） | — |
| `POST` | `/Library/VirtualFolders/Paths` | `AddMediaPath`（L366） | — |
| `POST` | `/Library/VirtualFolders/Paths/Update` | `UpdateMediaPath`（L409） | — |
| `DELETE` | `/Library/VirtualFolders/Paths` | `RemoveMediaPath`（L431） | — |
| `POST` | `/Library/VirtualFolders/LibraryOptions` | `UpdateLibraryOptions`（L477） | — |

## LocalizationController

语言、地区与分级字典。源码：[LocalizationController.cs](../Jellyfin.Api/Controllers/LocalizationController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Localization/Cultures` | `GetCultures`（L37） | — |
| `GET` | `/Localization/Countries` | `GetCountries`（L56） | — |
| `GET` | `/Localization/ParentalRatings` | `GetParentalRatings`（L68） | — |
| `GET` | `/Localization/Options` | `GetLocalizationOptions`（L80） | — |

## LyricsController

歌词读取与管理。源码：[LyricsController.cs](../Jellyfin.Api/Controllers/LyricsController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Audio/{itemId}/Lyrics` | `GetLyrics`（L71） | — |
| `POST` | `/Audio/{itemId}/Lyrics` | `UploadLyrics`（L103） | — |
| `DELETE` | `/Audio/{itemId}/Lyrics` | `DeleteLyrics`（L156） | — |
| `GET` | `/Audio/{itemId}/RemoteSearch/Lyrics` | `SearchRemoteLyrics`（L180） | — |
| `POST` | `/Audio/{itemId}/RemoteSearch/Lyrics/{lyricId}` | `DownloadRemoteLyrics`（L204） | — |
| `GET` | `/Providers/Lyrics/{lyricId}` | `GetRemoteLyrics`（L235） | — |

## MediaInfoController

播放信息协商与动态媒体源生命周期。源码：[MediaInfoController.cs](../Jellyfin.Api/Controllers/MediaInfoController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Items/{itemId}/PlaybackInfo` | `GetPlaybackInfo`（L75） | — |
| `POST` | `/Items/{itemId}/PlaybackInfo` | `GetPostedPlaybackInfo`（L119） | — |
| `POST` | `/LiveStreams/Open` | `OpenLiveStream`（L271） | — |
| `POST` | `/LiveStreams/Close` | `CloseLiveStream`（L316） | — |
| `GET` | `/Playback/BitrateTest` | `GetBitrateTestBytes`（L331） | — |

## MediaSegmentsController

媒体片段查询。源码：[MediaSegmentsController.cs](../Jellyfin.Api/Controllers/MediaSegmentsController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/MediaSegments/{itemId}` | `GetItemSegments`（L49） | — |

## MoviesController

电影推荐分组。源码：[MoviesController.cs](../Jellyfin.Api/Controllers/MoviesController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Movies/Recommendations` | `GetMovieRecommendations`（L60） | — |

## PackageController

插件包、仓库与安装任务。源码：[PackageController.cs](../Jellyfin.Api/Controllers/PackageController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Packages/{name}` | `GetPackageInfo`（L55） | — |
| `GET` | `/Packages` | `GetPackages`（L88） | — |
| `POST` | `/Packages/Installed/{name}` | `InstallPackage`（L108） | — |
| `DELETE` | `/Packages/Installing/{packageId}` | `CancelPackageInstallation`（L151） | — |
| `GET` | `/Repositories` | `GetRepositories`（L165） | — |
| `POST` | `/Repositories` | `SetRepositories`（L178） | — |

## PersonsController

演职人员。源码：[PersonsController.cs](../Jellyfin.Api/Controllers/PersonsController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Persons` | `GetPersons`（L74） | — |
| `GET` | `/Persons/{name}` | `GetPerson`（L154） | — |

## PlaylistsController

播放列表及成员权限。源码：[PlaylistsController.cs](../Jellyfin.Api/Controllers/PlaylistsController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `POST` | `/Playlists` | `CreatePlaylist`（L78） | — |
| `POST` | `/Playlists/{playlistId}` | `UpdatePlaylist`（L121） | — |
| `GET` | `/Playlists/{playlistId}` | `GetPlaylist`（L166） | — |
| `GET` | `/Playlists/{playlistId}/Users` | `GetPlaylistUsers`（L199） | — |
| `GET` | `/Playlists/{playlistId}/Users/{userId}` | `GetPlaylistUser`（L230） | — |
| `POST` | `/Playlists/{playlistId}/Users/{userId}` | `UpdatePlaylistUser`（L281） | — |
| `DELETE` | `/Playlists/{playlistId}/Users/{userId}` | `RemoveUserFromPlaylist`（L327） | — |
| `POST` | `/Playlists/{playlistId}/Items` | `AddItemToPlaylist`（L373） | — |
| `POST` | `/Playlists/{playlistId}/Items/{itemId}/Move/{newIndex}` | `MoveItem`（L412） | — |
| `DELETE` | `/Playlists/{playlistId}/Items` | `RemoveItemFromPlaylist`（L450） | — |
| `GET` | `/Playlists/{playlistId}/Items` | `GetPlaylistItems`（L513） | — |

## PlaystateController

播放状态上报及已观看标记。源码：[PlaystateController.cs](../Jellyfin.Api/Controllers/PlaystateController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `POST` | `/UserPlayedItems/{itemId}` | `MarkPlayedItem`（L75） | — |
| `DELETE` | `/UserPlayedItems/{itemId}` | `MarkUnplayedItem`（L122） | — |
| `POST` | `/Sessions/Playing` | `ReportPlaybackStart`（L164） | — |
| `POST` | `/Sessions/Playing/Progress` | `ReportPlaybackProgress`（L180） | — |
| `POST` | `/Sessions/Playing/Ping` | `PingPlaybackSession`（L196） | — |
| `POST` | `/Sessions/Playing/Stopped` | `ReportPlaybackStopped`（L210） | — |

## PluginsController

已安装插件及配置。源码：[PluginsController.cs](../Jellyfin.Api/Controllers/PluginsController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Plugins` | `GetPlugins`（L54） | — |
| `POST` | `/Plugins/{pluginId}/{version}/Enable` | `EnablePlugin`（L72） | — |
| `POST` | `/Plugins/{pluginId}/{version}/Disable` | `DisablePlugin`（L95） | — |
| `DELETE` | `/Plugins/{pluginId}/{version}` | `UninstallPluginByVersion`（L118） | — |
| `DELETE` | `/Plugins/{pluginId}` | `UninstallPlugin`（L140） | — |
| `GET` | `/Plugins/{pluginId}/Configuration` | `GetPluginConfiguration`（L167） | — |
| `POST` | `/Plugins/{pluginId}/Configuration` | `UpdatePluginConfiguration`（L191） | — |
| `GET` | `/Plugins/{pluginId}/{version}/Image` | `GetPluginImage`（L222） | 声明允许匿名 |
| `POST` | `/Plugins/{pluginId}/Manifest` | `GetPluginManifest`（L271） | — |

## QuickConnectController

跨设备登录授权。源码：[QuickConnectController.cs](../Jellyfin.Api/Controllers/QuickConnectController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/QuickConnect/Enabled` | `GetQuickConnectEnabled`（L43） | — |
| `POST` | `/QuickConnect/Initiate` | `InitiateQuickConnect`（L57） | — |
| `GET` | `/QuickConnect/Connect` | `GetQuickConnectState`（L80） | — |
| `POST` | `/QuickConnect/Authorize` | `AuthorizeQuickConnect`（L108） | — |

## RemoteImageController

远程图片发现与下载。源码：[RemoteImageController.cs](../Jellyfin.Api/Controllers/RemoteImageController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Items/{itemId}/RemoteImages` | `GetRemoteImages`（L66） | — |
| `GET` | `/Items/{itemId}/RemoteImages/Providers` | `GetRemoteImageProviders`（L131） | — |
| `POST` | `/Items/{itemId}/RemoteImages/Download` | `DownloadRemoteImage`（L155） | — |

## ScheduledTasksController

计划任务、执行与触发器。源码：[ScheduledTasksController.cs](../Jellyfin.Api/Controllers/ScheduledTasksController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/ScheduledTasks` | `GetTasks`（L40） | — |
| `GET` | `/ScheduledTasks/{taskId}` | `GetTask`（L75） | — |
| `POST` | `/ScheduledTasks/Running/{taskId}` | `StartTask`（L98） | — |
| `DELETE` | `/ScheduledTasks/Running/{taskId}` | `StopTask`（L122） | — |
| `POST` | `/ScheduledTasks/{taskId}/Triggers` | `UpdateTask`（L147） | — |

## SearchController

媒体搜索提示。源码：[SearchController.cs](../Jellyfin.Api/Controllers/SearchController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Search/Hints` | `GetSearchHints`（L82） | — |

## SessionController

会话、客户端能力与远程控制。源码：[SessionController.cs](../Jellyfin.Api/Controllers/SessionController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Sessions` | `GetSessions`（L55） | — |
| `POST` | `/Sessions/{sessionId}/Viewing` | `DisplayContent`（L83） | — |
| `POST` | `/Sessions/{sessionId}/Playing` | `Play`（L122） | — |
| `POST` | `/Sessions/{sessionId}/Playing/{command}` | `SendPlaystateCommand`（L165） | — |
| `POST` | `/Sessions/{sessionId}/System/{command}` | `SendSystemCommand`（L196） | — |
| `POST` | `/Sessions/{sessionId}/Command/{command}` | `SendGeneralCommand`（L222） | — |
| `POST` | `/Sessions/{sessionId}/Command` | `SendFullGeneralCommand`（L250） | — |
| `POST` | `/Sessions/{sessionId}/Message` | `SendMessageCommand`（L280） | — |
| `POST` | `/Sessions/{sessionId}/User/{userId}` | `AddUserToSession`（L309） | — |
| `DELETE` | `/Sessions/{sessionId}/User/{userId}` | `RemoveUserFromSession`（L330） | — |
| `POST` | `/Sessions/Capabilities` | `PostCapabilities`（L354） | — |
| `POST` | `/Sessions/Capabilities/Full` | `PostFullCapabilities`（L387） | — |
| `POST` | `/Sessions/Viewing` | `ReportViewing`（L412） | — |
| `POST` | `/Sessions/Logout` | `ReportSessionEnded`（L430） | — |
| `GET` | `/Auth/Providers` | `GetAuthProviders`（L445） | — |
| `GET` | `/Auth/PasswordResetProviders` | `GetPasswordResetProviders`（L459） | — |

## StartupController

首次启动配置。源码：[StartupController.cs](../Jellyfin.Api/Controllers/StartupController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `POST` | `/Startup/Complete` | `CompleteWizard`（L40） | — |
| `POST` | `/Startup/User` | `UpdateStartupUser`（L58） | — |

## StudiosController

工作室分类。源码：[StudiosController.cs](../Jellyfin.Api/Controllers/StudiosController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Studios` | `GetStudios`（L72） | — |
| `GET` | `/Studios/{name}` | `GetStudio`（L147） | — |

## SubtitleController

字幕搜索、上传、提取与转换。源码：[SubtitleController.cs](../Jellyfin.Api/Controllers/SubtitleController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `DELETE` | `/Videos/{itemId}/Subtitles/{index}` | `DeleteSubtitle`（L95） | — |
| `GET` | `/Items/{itemId}/RemoteSearch/Subtitles/{language}` | `SearchRemoteSubtitles`（L122） | — |
| `POST` | `/Items/{itemId}/RemoteSearch/Subtitles/{subtitleId}` | `DownloadRemoteSubtitles`（L148） | — |
| `GET` | `/Providers/Subtitles/Subtitles/{subtitleId}` | `GetRemoteSubtitles`（L184） | — |
| `GET` | `/Videos/{routeItemId}/{routeMediaSourceId}/Subtitles/{routeIndex}/Stream.{routeFormat}` | `GetSubtitle`（L211） | — |
| `GET` | `/Videos/{routeItemId}/{routeMediaSourceId}/Subtitles/{routeIndex}/{routeStartPositionTicks}/Stream.{routeFormat}` | `GetSubtitleWithTicks`（L297） | — |
| `GET` | `/Videos/{itemId}/{mediaSourceId}/Subtitles/{index}/subtitles.m3u8` | `GetSubtitlePlaylist`（L343） | — |
| `POST` | `/Videos/{itemId}/Subtitles` | `UploadSubtitle`（L423） | — |
| `GET` | `/FallbackFont/Fonts` | `GetFallbackFontList`（L539） | — |
| `GET` | `/FallbackFont/Fonts/{name}` | `GetFallbackFont`（L591） | — |

## SuggestionsController

建议内容。源码：[SuggestionsController.cs](../Jellyfin.Api/Controllers/SuggestionsController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Items/Suggestions` | `GetSuggestions`（L61） | — |

## SyncPlayController

同步观看组及队列控制。源码：[SyncPlayController.cs](../Jellyfin.Api/Controllers/SyncPlayController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `POST` | `/SyncPlay/New` | `SyncPlayCreateGroup`（L57） | — |
| `POST` | `/SyncPlay/Join` | `SyncPlayJoinGroup`（L74） | — |
| `POST` | `/SyncPlay/Leave` | `SyncPlayLeaveGroup`（L91） | — |
| `GET` | `/SyncPlay/List` | `SyncPlayGetGroups`（L107） | — |
| `GET` | `/SyncPlay/{id:guid}` | `SyncPlayGetGroup`（L124） | — |
| `POST` | `/SyncPlay/SetNewQueue` | `SyncPlaySetNewQueue`（L140） | — |
| `POST` | `/SyncPlay/SetPlaylistItem` | `SyncPlaySetPlaylistItem`（L161） | — |
| `POST` | `/SyncPlay/RemoveFromPlaylist` | `SyncPlayRemoveFromPlaylist`（L179） | — |
| `POST` | `/SyncPlay/MovePlaylistItem` | `SyncPlayMovePlaylistItem`（L197） | — |
| `POST` | `/SyncPlay/Queue` | `SyncPlayQueue`（L215） | — |
| `POST` | `/SyncPlay/Unpause` | `SyncPlayUnpause`（L232） | — |
| `POST` | `/SyncPlay/Pause` | `SyncPlayPause`（L248） | — |
| `POST` | `/SyncPlay/Stop` | `SyncPlayStop`（L264） | — |
| `POST` | `/SyncPlay/Seek` | `SyncPlaySeek`（L281） | — |
| `POST` | `/SyncPlay/Buffering` | `SyncPlayBuffering`（L299） | — |
| `POST` | `/SyncPlay/Ready` | `SyncPlayReady`（L321） | — |
| `POST` | `/SyncPlay/SetIgnoreWait` | `SyncPlaySetIgnoreWait`（L343） | — |
| `POST` | `/SyncPlay/NextItem` | `SyncPlayNextItem`（L361） | — |
| `POST` | `/SyncPlay/PreviousItem` | `SyncPlayPreviousItem`（L379） | — |
| `POST` | `/SyncPlay/SetRepeatMode` | `SyncPlaySetRepeatMode`（L397） | — |
| `POST` | `/SyncPlay/SetShuffleMode` | `SyncPlaySetShuffleMode`（L415） | — |
| `POST` | `/SyncPlay/Ping` | `SyncPlayPing`（L432） | — |

## SystemController

服务器状态、日志及进程控制。源码：[SystemController.cs](../Jellyfin.Api/Controllers/SystemController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/System/Info` | `GetSystemInfo`（L71） | — |
| `GET` | `/System/Info/Storage` | `GetSystemStorage`（L84） | — |
| `GET` | `/System/Info/Public` | `GetPublicSystemInfo`（L94） | — |
| `GET` | `/System/Ping` | `PingSystem`（L105） | — |
| `POST` | `/System/Ping` | `PingSystem`（L105） | — |
| `POST` | `/System/Restart` | `RestartApplication`（L118） | — |
| `POST` | `/System/Shutdown` | `ShutdownApplication`（L134） | — |
| `GET` | `/System/Logs` | `GetServerLogs`（L150） | — |
| `GET` | `/System/Endpoint` | `GetEndpointInfo`（L189） | — |
| `GET` | `/System/Logs/Log` | `GetLogFile`（L212） | — |

## TimeSyncController

同步观看时钟校准。源码：[TimeSyncController.cs](../Jellyfin.Api/Controllers/TimeSyncController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/GetUtcTime` | `GetUtcTime`（L22） | — |

## TrickplayController

进度预览图与 HLS 图块清单。源码：[TrickplayController.cs](../Jellyfin.Api/Controllers/TrickplayController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Videos/{itemId}/Trickplay/{width}/tiles.m3u8` | `GetTrickplayHlsPlaylist`（L54） | — |
| `GET` | `/Videos/{itemId}/Trickplay/{width}/{index}.jpg` | `GetTrickplayTileImage`（L83） | — |

## TvShowsController

剧集、季、下一集与即将播出。源码：[TvShowsController.cs](../Jellyfin.Api/Controllers/TvShowsController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Shows/NextUp` | `GetNextUp`（L77） | — |
| `GET` | `/Shows/Upcoming` | `GetUpcomingEpisodes`（L140） | — |
| `GET` | `/Shows/{seriesId}/Episodes` | `GetEpisodes`（L205） | — |
| `GET` | `/Shows/{seriesId}/Seasons` | `GetSeasons`（L335） | — |

## UniversalAudioController

客户端音频格式协商。源码：[UniversalAudioController.cs](../Jellyfin.Api/Controllers/UniversalAudioController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Audio/{itemId}/universal` | `GetUniversalAudioStream`（L99） | — |
| `HEAD` | `/Audio/{itemId}/universal` | `GetUniversalAudioStream`（L99） | — |

## UserController

用户、认证、配置和权限。源码：[UserController.cs](../Jellyfin.Api/Controllers/UserController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Users` | `GetUsers`（L94） | — |
| `GET` | `/Users/Public` | `GetPublicUsers`（L109） | — |
| `GET` | `/Users/{userId}` | `GetUserById`（L131） | — |
| `DELETE` | `/Users/{userId}` | `DeleteUser`（L155） | — |
| `POST` | `/Users/AuthenticateByName` | `AuthenticateUserByName`（L178） | — |
| `POST` | `/Users/AuthenticateWithQuickConnect` | `AuthenticateWithQuickConnect`（L214） | — |
| `POST` | `/Users/Password` | `UpdateUserPassword`（L241） | — |
| `POST` | `/Users` | `UpdateUser`（L301） | — |
| `POST` | `/Users/{userId}/Policy` | `UpdateUserPolicy`（L341） | — |
| `POST` | `/Users/Configuration` | `UpdateUserConfiguration`（L395） | — |
| `POST` | `/Users/New` | `CreateUserByName`（L425） | — |
| `POST` | `/Users/ForgotPassword` | `ForgotPassword`（L449） | — |
| `POST` | `/Users/ForgotPassword/Pin` | `ForgotPasswordPin`（L474） | — |
| `GET` | `/Users/Me` | `GetCurrentUser`（L490） | — |

## UserLibraryController

用户视角条目、收藏与附件内容。源码：[UserLibraryController.cs](../Jellyfin.Api/Controllers/UserLibraryController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Items/{itemId}` | `GetItem`（L84） | — |
| `GET` | `/Items/Root` | `GetRootFolder`（L118） | — |
| `GET` | `/Items/{itemId}/Intros` | `GetIntros`（L141） | — |
| `POST` | `/UserFavoriteItems/{itemId}` | `MarkFavoriteItem`（L177） | — |
| `DELETE` | `/UserFavoriteItems/{itemId}` | `UnmarkFavoriteItem`（L209） | — |
| `DELETE` | `/UserItems/{itemId}/Rating` | `DeleteUserItemRating`（L241） | — |
| `POST` | `/UserItems/{itemId}/Rating` | `UpdateUserItemRating`（L274） | — |
| `GET` | `/Items/{itemId}/LocalTrailers` | `GetLocalTrailers`（L306） | — |
| `GET` | `/Items/{itemId}/SpecialFeatures` | `GetSpecialFeatures`（L340） | — |
| `GET` | `/Items/Latest` | `GetLatestMedia`（L385） | — |

## UserViewsController

用户媒体视图。源码：[UserViewsController.cs](../Jellyfin.Api/Controllers/UserViewsController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/UserViews` | `GetUserViews`（L65） | — |
| `GET` | `/UserViews/GroupingOptions` | `GetGroupingOptions`（L103） | — |

## VideoAttachmentsController

视频内嵌附件。源码：[VideoAttachmentsController.cs](../Jellyfin.Api/Controllers/VideoAttachmentsController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Videos/{videoId}/{mediaSourceId}/Attachments/{index}` | `GetAttachment`（L54） | — |

## VideosController

视频流、多版本与附加片段。源码：[VideosController.cs](../Jellyfin.Api/Controllers/VideosController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Videos/{itemId}/AdditionalParts` | `GetAdditionalPart`（L97） | — |
| `DELETE` | `/Videos/{itemId}/AlternateSources` | `DeleteAlternateSources`（L143） | — |
| `POST` | `/Videos/MergeVersions` | `MergeVersions`（L187） | — |
| `GET` | `/Videos/{itemId}/stream` | `GetVideoStream`（L318） | — |
| `HEAD` | `/Videos/{itemId}/stream` | `GetVideoStream`（L318） | — |
| `GET` | `/Videos/{itemId}/stream.{container}` | `GetVideoStreamByContainer`（L556） | — |
| `HEAD` | `/Videos/{itemId}/stream.{container}` | `GetVideoStreamByContainer`（L556） | — |

## YearsController

年份分类。源码：[YearsController.cs](../Jellyfin.Api/Controllers/YearsController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Years` | `GetYears`（L74） | — |
| `GET` | `/Years/{year}` | `GetYear`（L176） | — |

## TmdbController

内置 TMDb 客户端配置。源码：[TmdbController.cs](../MediaBrowser.Providers/Plugins/Tmdb/Api/TmdbController.cs)。

| HTTP | 路径 | 动作方法（源码行） | 备注 |
| --- | --- | --- | --- |
| `GET` | `/Tmdb/ClientConfiguration` | `TmdbClientConfiguration`（L37） | 隐藏于 OpenAPI |
