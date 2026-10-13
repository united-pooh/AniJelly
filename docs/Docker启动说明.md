# 从当前源码启动 Jellyfin

[Dockerfile](../Dockerfile) 编译本仓库服务端，并打包固定提交的官方 Jellyfin Web 及[兼容补丁](../docker/jellyfin-web-anijelly.patch)、[Cinema 视觉主题](../docker/Cinema主题说明.md)和[电视/频道移除补丁](../docker/jellyfin-web-no-livetv.patch)。[Compose 配置](../compose.yaml) 使用独立配置、缓存卷；目前不挂载媒体目录，仅绑定本机端口。

## 构建和启动

在仓库根目录运行，需已启动 Docker Desktop 或 Docker Engine：

```bash
docker compose build
docker compose up -d
docker compose ps
```

打开 <http://localhost:8096/web/>。首次设置先创建管理员，再设置界面语言、服务器名称及其他选项。管理员密码由你自行设置，不能留空；没有媒体目录时跳过添加媒体库。服务没有预设账号。

```bash
curl -fsS http://localhost:8096/health
docker compose logs --tail 100 jellyfin
```

健康检查应返回 `Healthy`。启动早期的 HTTP 200 或 `Degraded` 不代表启动完成。API 文档位于 <http://localhost:8096/api-docs/swagger/index.html>。

## 本机 Docker Desktop 启动方式

2026-10-12 本机 Docker Desktop 4.44.3 的普通 socket 启动请求卡在卷共享注册，重启 Desktop 后仍可复现。底层引擎健康，当前服务已通过同一引擎的本机 raw socket 启动；没有更改全局 context。此机器启动、重建或重启时使用：

```bash
docker --host "unix://$HOME/Library/Containers/com.docker.docker/Data/docker.raw.sock" compose up -d
docker --host "unix://$HOME/Library/Containers/com.docker.docker/Data/docker.raw.sock" compose restart
```

此路径仅用于当前 macOS Docker Desktop 环境，不是 Linux 部署要求。宿主端口转发、命名卷和非 root 写入均已实际验证。普通 `docker compose ps` 与 `logs` 可读取运行状态。

初次下载还受到本机代理 DNS/网络影响：MCR 镜像经官方来源下载、逐层校验摘要后导入，没有换第三方镜像源；Ubuntu 包下载使用已有代理进行本次构建。需要在此机器重新下载依赖时，可使用：

```bash
docker compose build \
  --build-arg HTTP_PROXY=http://host.docker.internal:7890 \
  --build-arg HTTPS_PROXY=http://host.docker.internal:7890 \
  --build-arg http_proxy=http://host.docker.internal:7890 \
  --build-arg https_proxy=http://host.docker.internal:7890 \
  --build-arg NO_PROXY=localhost,127.0.0.1 \
  --build-arg no_proxy=localhost,127.0.0.1
```

代理参数仅作用于构建，不写入 Compose 运行环境；代理端口来自本机已有服务。已有缓存的最终构建已用不带代理参数的普通 `docker compose build` 成功完成。

## 日常管理和数据

```bash
docker compose stop       # 停止服务
docker compose up -d      # 再次启动
docker compose restart   # 重启，保留数据
docker compose down      # 移除容器和网络，保留卷
```

| 数据 | Docker 卷 | 容器路径 |
| --- | --- | --- |
| 数据库、配置、账号、插件、元数据与 ASP.NET 密钥 | `anijelly_config` | `/config` |
| 缓存 | `anijelly_cache` | `/cache` |

`docker compose down -v` 会删除这些持久化卷，不用于日常停止。现有其他容器的数据与配置不参与此部署。运行用户为镜像内非 root 的 `app`。

以后添加媒体目录时，在 Compose 的 `volumes` 下增加所需的只读绑定，例如 `/本机媒体目录:/media:ro`，再在管理界面添加 `/media` 媒体库。正式实例不创建示例媒体目录，也不挂载用户其他文件夹；验收短片仅在临时实例缓存卷内生成。端口只允许本机连接；更改公网或局域网访问配置前需自行配置相应网络与认证。

## 版本和兼容边界

- 服务端来自当前工作区，版本 `13.0.0`，目标 .NET 10。构建支持 `linux/arm64` 与 `linux/amd64`；本机实际验收架构另见下节。
- Web 固定官方提交 `319dbfc7e83e5f883f5d2708b727329743049c03`，使用其 lockfile 安装依赖后应用补丁并构建。更换提交需要重新检查补丁和接口兼容。
- 运行镜像使用 ASP.NET 10 / Ubuntu Noble，FFmpeg 来自 Jellyfin 签名软件源的 `jellyfin-ffmpeg7`。这里未配置 GPU 设备映射。
- 首次设置与有等价替代的用户、条目、头像旧接口已在 Web 调用端迁移。服务端已删除的接口不恢复；完整变化见[弃用接口迁移说明](弃用接口迁移说明.md)。
- 艺人/专辑艺人聚合、艺人搜索/收藏，以及按名称打开的艺人/音乐流派详情仍未适配。通用 Items 查询不能无损替代聚合接口的计数与关联语义；详见[未适配功能清单](../docker/Web兼容说明.md#已知未适配功能)。
- 正式实例没有媒体挂载。电视、DVR 与通用频道扩展已删除；不能把启动成功视为真实用户媒体、硬件转码或外部插件已验证。

## 本机验证记录

2026-10-12：正式 `anijelly-jellyfin-1` 已部署删除电视/DVR/Channels 后的 `anijelly:local`，镜像 ID `sha256:c015fd04c694eac59932abef6d1566ee72224ce119ab541f9597891cd21ecdcd`，实际架构 `linux/arm64`。Liquid Glass 外观保留；新提出的 NAS 番剧四屏设计未部署。

| 检查 | 结果 |
| --- | --- |
| 正式运行 | 22 项独立检查通过；容器 healthy、`/health` 正文 Healthy，版本 13.0.0，日志含 Startup complete |
| HTTP/API | OpenAPI 251 路径 / 309 操作；两轮累计 96 条删除声明不出现；通用 LiveStreams/Open、Close 保留；30 个首屏脚本和 Swagger 可读取 |
| 持久化 | 部署前后服务器 ID、DataProtection 密钥 SHA-256 和原配置/缓存卷保持 |
| 权限与隔离 | UID/GID 1654，仅 127.0.0.1:8096，无媒体挂载；向导仍未完成，没有创建正式管理员 |
| 源码与 Web | Release 0 警告 / 0 错误；Web 215 项测试、类型及改动文件 lint 通过；三方向复核完成，三份补丁可复现全部 src；生产资源无测试/Vitest 混入 |
| 临时普通媒体 | 独立缓存卷内 30 秒 H.264/AAC 合成片扫描、PlaybackInfo、HTTP 206 Range、真实浏览器播放与暂停通过；不代表真实媒体/GPU 转码已测 |
| 临时配置和任务 | 浏览器保存 AniJelly Media Verified 后 API 回读一致；认证任务列表 18 项，无节目指南/频道刷新任务 |
| 正式任务边界 | 匿名 /ScheduledTasks 返回 401；未降低认证，未创建账号。任务缺席另由同镜像临时实例及集成 DI 验证 |

9 张真实桌面 1280×720 / 手机 390×844 截图、浏览器时间推进和具体边界见[本轮验收记录](../.agent/visual/media-only-validation.md)，正式检查见[运行证据](../.agent/visual/media-only-runtime.json)。本轮临时 18096 容器、两个测试卷和项目网络已移除，私密 fixture 已删除；测试账号未进入正式卷。清理后正式实例仍 Healthy，原卷与回滚标签保留，见[清理证据](../.agent/visual/media-only-cleanup.json)。

此前的[毛玻璃记录](../.agent/visual/glass-validation.md)、[Cinema v1 记录](../.agent/visual/cinema-validation.md)与[原版记录](../.agent/visual/jellyfin-validation.md)均为历史轮次，不代表当前镜像的检查数量或清理状态。

本机 Docker 虚拟机为 8 GiB，服务端发布和 Web 编译采用分阶段缓存以避免并行峰值。Web 压缩器也通过 [构建配置](../docker/webpack.production.cjs) 固定为 2 个工作进程，保留上游生产配置和两次压缩选项；否则默认按 10 个可用 CPU 启动压缩工作进程时，独立 Web 构建也曾内存不足。可按以下顺序构建：

```bash
docker build --target server-build .
docker compose build
```

新默认主题为 Cinema，原有主题及已保存偏好保留，模块和补丁维护方式见 [Cinema 主题说明](../docker/Cinema主题说明.md)。本次部署前镜像保留为 `anijelly:before-media-only`；更早的 `anijelly:before-glass` 与 `anijelly:before-cinema` 仍作历史回退标签。回退镜像不需要清除配置卷，但不得因此忽略数据库版本兼容。

ASP.NET 的挂载检查不解析符号链接，日志仍可能提示 `/home/app/.aspnet` 不持久化；实际路径已指向 `/config/aspnet`，并通过跨重建密钥哈希验证。两条 overlay 硬件滤镜能力警告保留；合成片直接播放通过不能证明硬件转码可用。

本轮正式启动独立快照记录 0 条 socket 错误、0 条其他 ERR/FTL 与上述 3 条警告；历史版本曾出现无令牌 socket 错误，认证规则未放宽。收藏页仍受已删除音乐聚合接口的兼容限制影响，不作为本次通过项。

全解决方案 Release / Debug 构建与全量测试的实际结果见[手册核验记录](手册核验记录.md#全解决方案构建roslyn-问题已修复)。
