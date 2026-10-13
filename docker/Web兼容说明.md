# 固定 Web 版本与兼容边界

服务端镜像编译本仓库工作区；Web 来自官方 [jellyfin-web](https://github.com/jellyfin/jellyfin-web/tree/319dbfc7e83e5f883f5d2708b727329743049c03)，固定提交 `319dbfc7e83e5f883f5d2708b727329743049c03`。上游 `package.json` 版本为 `13.0.0`，要求 Node.js >= 24、npm >= 11。使用提交自带的 `package-lock.json` 与 `npm ci`，不更新依赖。

锁定依赖包括 `jellyfin-apiclient 1.11.0`、`@jellyfin/sdk 0.0.0-unstable.202609160935`。接口兼容补丁 [jellyfin-web-anijelly.patch](jellyfin-web-anijelly.patch) 和随后应用的 [Cinema 视觉补丁](jellyfin-web-cinema.patch) 仅改 Web 的 `src/`，不修改 `node_modules`、后端 API 或添加服务器兼容代理。视觉范围与维护方式见 [Cinema 主题说明](Cinema主题说明.md)。

第三份 [功能移除补丁](jellyfin-web-no-livetv.patch) 删除电视直播、DVR 和通用频道插件的前端入口及相关实现。详细范围见[移除说明](../docs/电视直播与录像机移除说明.md)。远程媒体转码选项改用无电视示例的中性文案；中英文已翻译，其他语言由既有机制回退英文，权限行为保持。

第四份 [播放信息补丁](jellyfin-web-playerstats.patch) 在打开播放器统计窗口时同步显示已有媒体信息，独立补充客户端与服务器统计，合并在途请求并保留10秒会话缓存，避免关闭/换片后的旧响应回写。信息模块随视频页加载；内容未变化时不重建DOM。回归测试覆盖暂停、慢请求、失败、重开、销毁、换片与缓存。

2026-10-13播放信息修复已部署到8096：226项Web测试通过；真实播放器在服务器统计请求挂起时，暂停/播放打开分别约140/160毫秒（含菜单动画），无打开期间主线程长任务。原配置和服务器身份保留，见[本轮验收](../.agent/visual/player-info-validation.md)。

在 Web 仓库根目录执行：

```sh
npm ci --no-audit --fund=false
git apply /patches/jellyfin-web-anijelly.patch
git apply /patches/jellyfin-web-cinema.patch
git apply /patches/jellyfin-web-no-livetv.patch
git apply /patches/jellyfin-web-playerstats.patch
npm test
npm run build:check
npm run build:production
```

Docker 使用 Node.js 24 的 Debian Bookworm 构建阶段，并用 [生产构建配置](webpack.production.cjs) 将压缩并发限制为 2；其余配置继承固定上游，压缩 passes 保持 2。构建后复制 `dist/` 到服务端镜像的 Web 目录。升级上游提交或依赖锁文件后，必须重新审查补丁适用性与下列路由清单，不能只让 `git apply` 忽略冲突。

## 改动与验证范围

- 首次安装先显示管理员账户页。`POST Startup/User` 创建管理员后，通过既有 `authenticateUserByName` 登录，复用官方客户端的凭据持久化和 SDK token 更新。随后读取/更新完整的 `System/Configuration` 与 `System/Configuration/network`，完成后调用保留的 `POST Startup/Complete`。
- 浏览器中断但服务未重启时，重新打开向导可以输入既有管理员凭据。若服务端重启，既有安全检查会在发现已配置管理员后标记向导完成，此时直接正常登录后从控制台继续配置。创建接口返回 403 时只继续尝试正常登录，不绕过密码验证；其他创建错误停止流程。
- 等价用户路由迁移集中在唯一的 ApiClient 创建入口，保留查询参数与用户 ID；仅 POST 更新用户改到 `Users?userId=`，GET/DELETE `Users/{id}` 保留。
- 用户头像改用 `UserImage`，并修正独立 `UserAvatar.tsx` 中绕过 ApiClient 的旧路径。用户头像只有一张；旧接口的类型/索引和图片缩放参数并无对应的新功能。
- 开始/进度/停止播放及通用 InstantMix 在锁定的 ApiClient 中已经使用 `Sessions/Playing*` 与 `Items/{id}/InstantMix`，没有为了删除旧路由而修改它们。
- 本地回归先观察旧实现 19 个路由测试失败、5 个活动路由保护用例通过；首次设置 4 个测试失败。补丁后 29 个相关测试、TypeScript 检查、改动文件 ESLint 检查通过。本机 Node.js 26.7.0 / npm 11.19.0 的生产构建通过，Webpack 有两项资源/入口体积提示。镜像内 Node.js 24 生产构建及29项回归测试也通过；临时实例 API 创建首管理员后，浏览器以已有凭据验证向导恢复认证、完整配置流程、首页/控制台、退出与重新登录，API回读配置通过。正式实例保持未初始化，详见[部署验收](../docs/Docker启动说明.md#本机验证记录)。

## 已知未适配功能

本次按“提供网页界面、暂不挂载媒体”执行。空媒体首次设置和首页可运行不等于所有媒体浏览/播放功能均已适配。

`Artists` 和 `Artists/AlbumArtists` 原本按媒体项聚合艺人，不能等价替换为 `Items?includeItemTypes=MusicArtist` 或 `Persons`。本补丁保留这一限制，不悄悄改变查询含义。这些调用目前仍会遇到后端 404：

- 传统音乐库的艺人、专辑艺人标签：`src/apps/legacy/controllers/music/musicartists.js`。
- 现代媒体库的艺人、专辑艺人视图：`src/hooks/useFetchItems.ts`。
- 搜索结果中的艺人部分：`src/apps/legacy/features/search/api/useArtistsSearch.ts`。
- 收藏页和首页收藏艺人区：`src/apps/legacy/controllers/favorites.js`、`src/components/favoriteitems.js`。
- 分类条目页中的专辑艺人查询：`src/scripts/itemsByName.js`。
- 按名称而非条目 ID 打开的艺人、音乐流派详情：`src/apps/legacy/controllers/itemDetails/index.js` 的 `musicartist` / `musicgenre` 分支。按 ID 的条目详情已迁移；不能把 `MusicGenres/{name}` 直接换成普通 `Genres/{name}`。

正常音乐流派列表使用 `Genres` 并携带音乐库 `parentId`；锁定版本的 `getMusicGenres` 旧方法没有找到 Web 源码调用。未挂载媒体，媒体扫描、播放、字幕、转码、真实头像上传以及上述音乐页面尚不能据此宣称通过。

## 53 条删除声明的调用审计

审计同时检查 Web 源码、锁定 ApiClient 的 sourcemap 原始代码与 SDK 生成端点；区分依赖中“有方法声明”与 Web “有实际调用”。“未发现调用”只针对锁定提交，不代表所有外部客户端。此表以 HTTP 声明计数，GET/HEAD、带索引变体分别列出。

| 旧声明 | 锁定 Web 的调用情况 / 处理 |
|---|---|
| `GET /Artists` | 存在实际调用；无等价聚合替代，见已知未适配功能。 |
| `GET /Artists/AlbumArtists` | 存在实际调用；无等价聚合替代，见已知未适配功能。 |
| `GET /Artists/{name}` | itemDetails 的 musicartist 名称分支仍调用；未适配。 |
| `POST /Users/{userId}/Images/{imageType}` | ApiClient uploadUserImage 实际使用无索引变体；用户图片路由统一迁为 UserImage?userId=。 |
| `POST /Users/{userId}/Images/{imageType}/{index}` | ApiClient uploadUserImage 实际使用无索引变体；用户图片路由统一迁为 UserImage?userId=。 |
| `DELETE /Users/{userId}/Images/{imageType}` | ApiClient deleteUserImage 单独迁移，避免索引追加到查询串之后。 |
| `DELETE /Users/{userId}/Images/{imageType}/{index}` | ApiClient deleteUserImage 单独迁移，避免索引追加到查询串之后。 |
| `GET /Users/{userId}/Images/{imageType}` | ApiClient getUserImageUrl 和直接拼接头像 URL 的 UserAvatar 均已迁移。 |
| `HEAD /Users/{userId}/Images/{imageType}` | 无显式 HEAD 调用；用户头像 URL 构造统一迁为 UserImage?userId=。 |
| `GET /Users/{userId}/Images/{imageType}/{imageIndex}` | ApiClient getUserImageUrl 和直接拼接头像 URL 的 UserAvatar 均已迁移。 |
| `HEAD /Users/{userId}/Images/{imageType}/{imageIndex}` | 无显式 HEAD 调用；用户头像 URL 构造统一迁为 UserImage?userId=。 |
| `GET /MusicGenres/{name}/InstantMix` | 未发现 Web 调用该旧声明；通用入口使用 Items/{id}/InstantMix。 |
| `GET /Artists/InstantMix` | 未发现 Web 调用该旧声明；通用入口使用 Items/{id}/InstantMix。 |
| `GET /MusicGenres/InstantMix` | 未发现 Web 调用该旧声明；通用入口使用 Items/{id}/InstantMix。 |
| `GET /Users/{userId}/Items` | ApiClient/传统 Web 存在调用；迁为 Items 或 UserItems 对应路径并保留 userId 与其他参数。 |
| `GET /Users/{userId}/Items/Resume` | ApiClient/传统 Web 存在调用；迁为 Items 或 UserItems 对应路径并保留 userId 与其他参数。 |
| `GET /Users/{userId}/Items/{itemId}/UserData` | 未发现旧路径调用；SDK 使用现代 UserItems，客户端迁移也覆盖旧模式。 |
| `POST /Users/{userId}/Items/{itemId}/UserData` | 未发现旧路径调用；SDK 使用现代 UserItems，客户端迁移也覆盖旧模式。 |
| `GET /LiveTv/Recordings/Series` | 未发现 Web/锁定 ApiClient 调用。 |
| `GET /LiveTv/Recordings/Groups` | 未发现 Web/锁定 ApiClient 调用。 |
| `GET /MusicGenres` | 依赖仍有方法，但未发现 Web 调用；实际列表调用 Genres。 |
| `GET /MusicGenres/{genreName}` | itemDetails 的 musicgenre 名称分支仍调用；未适配。 |
| `POST /Users/{userId}/PlayedItems/{itemId}` | ApiClient markPlayed/markUnplayed 已迁为 UserPlayedItems/{id}?userId=。 |
| `DELETE /Users/{userId}/PlayedItems/{itemId}` | ApiClient markPlayed/markUnplayed 已迁为 UserPlayedItems/{id}?userId=。 |
| `POST /PlayingItems/{itemId}` | 未发现旧调用；ApiClient 的播放上报已经使用 Sessions/Playing*。 |
| `POST /Users/{userId}/PlayingItems/{itemId}` | 未发现旧调用；ApiClient 的播放上报已经使用 Sessions/Playing*。 |
| `POST /PlayingItems/{itemId}/Progress` | 未发现旧调用；ApiClient 的播放上报已经使用 Sessions/Playing*。 |
| `POST /Users/{userId}/PlayingItems/{itemId}/Progress` | 未发现旧调用；ApiClient 的播放上报已经使用 Sessions/Playing*。 |
| `DELETE /PlayingItems/{itemId}` | 未发现旧调用；ApiClient 的播放上报已经使用 Sessions/Playing*。 |
| `DELETE /Users/{userId}/PlayingItems/{itemId}` | 未发现旧调用；ApiClient 的播放上报已经使用 Sessions/Playing*。 |
| `GET /Startup/Configuration` | start/settings 向导改为登录后完整 GET/POST System/Configuration。 |
| `POST /Startup/Configuration` | start/settings 向导改为登录后完整 GET/POST System/Configuration。 |
| `POST /Startup/RemoteAccess` | remote 向导改为完整 GET/POST System/Configuration/network。 |
| `GET /Startup/User` | 删除向导 GET；使用 POST 创建管理员，然后 AuthenticateByName 登录。 |
| `GET /Startup/FirstUser` | SDK 有生成声明但未发现 Web 调用。 |
| `GET /Users/{userId}/Suggestions` | 未发现旧路径调用；客户端迁移覆盖到 Items/Suggestions?userId=。 |
| `GET /Trailers` | SDK 有声明，未发现 Web 调用；条目列表使用 Items 查询。 |
| `POST /Users/{userId}/Authenticate` | 未发现旧调用；ApiClient 已使用 Users/AuthenticateByName。 |
| `POST /Users/{userId}/Password` | ApiClient 更新/重置密码已迁为 Users/Password?userId=。 |
| `POST /Users/{userId}` | 单独迁移 ApiClient updateUser POST；保留同路径 GET/DELETE，SDK 本已现代。 |
| `POST /Users/{userId}/Configuration` | ApiClient 已迁为 Users/Configuration?userId=；SDK 本已现代。 |
| `GET /Users/{userId}/Items/{itemId}` | ApiClient/传统 Web 存在调用；迁为 Items 或 UserItems 对应路径并保留 userId 与其他参数。 |
| `GET /Users/{userId}/Items/Root` | ApiClient/传统 Web 存在调用；迁为 Items 或 UserItems 对应路径并保留 userId 与其他参数。 |
| `GET /Users/{userId}/Items/{itemId}/Intros` | ApiClient/传统 Web 存在调用；迁为 Items 或 UserItems 对应路径并保留 userId 与其他参数。 |
| `POST /Users/{userId}/FavoriteItems/{itemId}` | ApiClient 收藏状态调用已迁为 UserFavoriteItems/{id}?userId=。 |
| `DELETE /Users/{userId}/FavoriteItems/{itemId}` | ApiClient 收藏状态调用已迁为 UserFavoriteItems/{id}?userId=。 |
| `DELETE /Users/{userId}/Items/{itemId}/Rating` | ApiClient/传统 Web 存在调用；迁为 Items 或 UserItems 对应路径并保留 userId 与其他参数。 |
| `POST /Users/{userId}/Items/{itemId}/Rating` | ApiClient/传统 Web 存在调用；迁为 Items 或 UserItems 对应路径并保留 userId 与其他参数。 |
| `GET /Users/{userId}/Items/{itemId}/LocalTrailers` | ApiClient/传统 Web 存在调用；迁为 Items 或 UserItems 对应路径并保留 userId 与其他参数。 |
| `GET /Users/{userId}/Items/{itemId}/SpecialFeatures` | ApiClient/传统 Web 存在调用；迁为 Items 或 UserItems 对应路径并保留 userId 与其他参数。 |
| `GET /Users/{userId}/Items/Latest` | ApiClient/传统 Web 存在调用；迁为 Items 或 UserItems 对应路径并保留 userId 与其他参数。 |
| `GET /Users/{userId}/Views` | ApiClient getUserViews / homeScreenSettings 直接调用；已迁为 UserViews 及其 GroupingOptions。 |
| `GET /Users/{userId}/GroupingOptions` | ApiClient getUserViews / homeScreenSettings 直接调用；已迁为 UserViews 及其 GroupingOptions。 |
