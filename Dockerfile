ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0-noble
ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0-noble
ARG NODE_IMAGE=node:24-bookworm

FROM ${SDK_IMAGE} AS server-build
ARG TARGETARCH
WORKDIR /src
COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages \
    case "$TARGETARCH" in arm64) rid=linux-arm64 ;; amd64) rid=linux-x64 ;; *) exit 1 ;; esac \
    && dotnet publish Jellyfin.Server/Jellyfin.Server.csproj \
       --configuration Release --runtime "$rid" --self-contained false \
       -p:UseAppHost=false --output /out

FROM ${NODE_IMAGE} AS web-build
ARG JELLYFIN_WEB_COMMIT=319dbfc7e83e5f883f5d2708b727329743049c03
WORKDIR /web
RUN git init . \
    && git remote add origin https://github.com/jellyfin/jellyfin-web.git \
    && git fetch --depth 1 origin "$JELLYFIN_WEB_COMMIT" \
    && git checkout --detach FETCH_HEAD
RUN --mount=type=cache,target=/root/.npm npm ci --no-audit --fund=false
COPY docker/jellyfin-web-anijelly.patch docker/jellyfin-web-cinema.patch docker/jellyfin-web-no-livetv.patch docker/jellyfin-web-playerstats.patch /tmp/
COPY docker/webpack.production.cjs /tmp/
RUN git apply /tmp/jellyfin-web-anijelly.patch \
    && git apply /tmp/jellyfin-web-cinema.patch \
    && git apply /tmp/jellyfin-web-no-livetv.patch \
    && git apply /tmp/jellyfin-web-playerstats.patch \
    && NODE_ENV=production ./node_modules/.bin/webpack --config /tmp/webpack.production.cjs

FROM ${RUNTIME_IMAGE} AS runtime
RUN apt-get update \
    && apt-get install --no-install-recommends -y ca-certificates curl gnupg \
    && mkdir -p /etc/apt/keyrings \
    && curl -fsSL https://repo.jellyfin.org/jellyfin_team.gpg.key \
       | gpg --batch --dearmor -o /etc/apt/keyrings/jellyfin.gpg \
    && printf 'deb [signed-by=/etc/apt/keyrings/jellyfin.gpg] https://repo.jellyfin.org/ubuntu noble main\n' \
       > /etc/apt/sources.list.d/jellyfin.list \
    && apt-get update \
    && apt-get install --no-install-recommends -y jellyfin-ffmpeg7 libfontconfig1 libfreetype6 fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /config /cache \
    && chown app:app /config /cache
RUN mkdir -p /config/aspnet \
    && ln -s /config/aspnet /home/app/.aspnet \
    && chown app:app /config/aspnet
ENV ASPNETCORE_HTTP_PORTS=""
WORKDIR /jellyfin
COPY --from=server-build /out/ ./
COPY --from=web-build /web/dist/ /jellyfin-web/
USER app
EXPOSE 8096
HEALTHCHECK --interval=20s --timeout=5s --start-period=60s --retries=5 \
    CMD test "$(curl -fsS http://127.0.0.1:8096/health)" = Healthy || exit 1
ENTRYPOINT ["dotnet", "/jellyfin/jellyfin.dll"]
CMD ["--datadir", "/config", "--cachedir", "/cache", "--webdir", "/jellyfin-web", "--ffmpeg", "/usr/lib/jellyfin-ffmpeg/ffmpeg"]
